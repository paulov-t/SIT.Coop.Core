using SIT.Coop.Core.Matchmaker;
using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using EFT;
using SIT.Coop.Core.LocalGame;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnMovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnMovePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x =>
                x.GetParameters().Length == 1
                && x.GetParameters()[0].Name.Contains("direction")
                && x.Name == "Move"
                );

            //Logger.LogInfo($"PlayerOnMovePatch:{t.Name}:{method.Name}");
            return method;
        }

        public static Dictionary<string, DateTime> LastStoredMoveTime { get; } = new Dictionary<string, DateTime>();

        [PatchPrefix]
        public static bool PatchPrefix(
            EFT.Player __instance,
            Vector2 direction)
        {
            // Get the replicated component for movement
            var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            if (prc == null) // If the replicated component doesn't exist (i.e. its SP), run as normal
                return true;

            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 direction)
        {
            //if (direction == Vector2.zero)
            //    return;
            var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            if (prc == null)
                return;

            if (prc.LastMovementPacket != null
                && prc.LastMovementPacket["dX"].ToString() == direction.x.ToString()
                && prc.LastMovementPacket["dY"].ToString() == direction.y.ToString()
                )
                return;


            var accountId = __instance.Profile.AccountId;
            //if (!LastStoredMoveTime.ContainsKey(accountId) || LastStoredMoveTime[accountId] > DateTime.Now.AddMilliseconds(-100))
            //    return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            var newPost = __instance.Transform.position;
            newPost.x += __instance.MovementContext.Velocity.normalized.x * 0.05f;
            newPost.y += __instance.MovementContext.Velocity.normalized.y * 0.05f;
            newPost.z += __instance.MovementContext.Velocity.normalized.z * 0.05f;
            //newPost.x += __instance.MovementContext.Velocity.normalized.x * 0.01f;
            //newPost.x += __instance.MovementContext.MovementDirection.x;
            //newPost.y += __instance.MovementContext.MovementDirection.y;

            dictionary.Add("nPx", newPost.x);
            dictionary.Add("nPy", newPost.y);
            dictionary.Add("nPz", newPost.z);
            dictionary.Add("dX", direction.x.ToString());
            dictionary.Add("dY", direction.y.ToString());
            dictionary.Add("vel", __instance.MovementContext.Velocity);
            dictionary.Add("spd", __instance.MovementContext.CharacterMovementSpeed);
            //dictionary.Add("mdX", __instance.MovementContext.MovementDirection.x);
            //dictionary.Add("mdY", __instance.MovementContext.MovementDirection.y);
            dictionary.Add("arX", __instance.MovementContext.Rotation.x);
            dictionary.Add("arY", __instance.MovementContext.Rotation.y);
            dictionary.Add("sprint", __instance.MovementContext.IsSprintEnabled);
            dictionary.Add("m", "Move");
            //Task.Run(delegate
            //{
                ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //});

            //if(!LastStoredMoveTime.ContainsKey(accountId))
            //    LastStoredMoveTime.Add(accountId, DateTime.Now);

            //LastStoredMoveTime[accountId] = DateTime.Now;
        }

        public static void MoveReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            //Logger.LogInfo("MoveReplicated!");
            var newPos = Vector3.zero;
            newPos.x = float.Parse(dict["nPx"].ToString());
            newPos.y = float.Parse(dict["nPy"].ToString());
            newPos.z = float.Parse(dict["nPz"].ToString());
            //player.Transform.position = newPos;
            //try
            //{
            Vector2 direction = new Vector2(float.Parse(dict["dX"].ToString()), float.Parse(dict["dY"].ToString()));
            //Vector2 rotation = new Vector2(float.Parse(dict["arX"].ToString()), float.Parse(dict["arY"].ToString()));
            //direction.x = Math.Sign(direction.x);
            //direction.y = Math.Sign(direction.y);
            //player.MovementContext.MovementDirection = direction;

            bool enableSprint = false;
            if (dict.ContainsKey("sprint"))
                enableSprint = bool.Parse(dict["sprint"].ToString());

            player.MovementContext.EnableSprint(enableSprint && direction.y > 0.1f);
            if (player.MovementContext.IsSprintEnabled)
            {
                player.MovementContext.SetPoseLevel(1f);
                if (player.MovementContext.PoseLevel > 0.9f && player.MovementContext.SmoothedCharacterMovementSpeed >= 1f)
                {
                    player.MovementContext.PlayerAnimatorEnableSprint(true);
                }
            }
            //    if (dict.ContainsKey("spd"))
            //        player.MovementContext.CharacterMovementSpeed = float.Parse(dict["spd"].ToString());
            //    player.MovementContext.PlayerAnimatorEnableInert(false);
            //}
            //catch(Exception ex)
            //{
            //    Logger.LogInfo("PlayerOnMovePatch.MoveReplicated:ERROR:" + ex.Message);
            //}

            player.CurrentState.Move(direction);
            if (direction.sqrMagnitude >= float.Epsilon)
            {
                //player.float_4 = Time.time;
            }
            player.InputDirection = direction;

            if(Vector3.Distance(player.Position, newPos) > 1.0f || Vector3.Distance(player.Position, newPos) < -1.0f)
            {
                player.Transform.position = newPos;
            }

            // handle rotation
            if (player.IsAI || player.Profile.AccountId == LocalGamePatches.MyPlayerProfile.AccountId)
                return;

            Vector2 rotation = new Vector2(float.Parse(dict["arX"].ToString()), float.Parse(dict["arY"].ToString()));
            PatchConstants.SetFieldOrPropertyFromInstance<Vector2>(player, "Rotation", rotation);
        }


    }
}
