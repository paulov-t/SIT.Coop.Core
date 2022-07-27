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

        public static Dictionary<string, ulong> Sequence { get; } = new Dictionary<string, ulong>();
        public static Dictionary<string, DateTime> LastPacketSent { get; } = new Dictionary<string, DateTime>();
        public static Dictionary<string, Vector2?> LastDirection { get; } = new Dictionary<string, Vector2?>();

        public static Dictionary<string, bool> ClientIsMoving { get; } = new Dictionary<string, bool>();

        public static bool IsMyPlayer(EFT.Player player) { return player == (LocalGamePatches.MyPlayer as EFT.Player); }

        [PatchPrefix]
        public static bool PatchPrefix(
            EFT.Player __instance,
            Vector2 direction)
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
            //bool runOriginal = false;
            //// Get the replicated component for movement
            //var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            //if (prc == null || Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer) // If the replicated component doesn't exist (i.e. its SP), run as normal
            //    return true;

            //var accountId = __instance.Profile.AccountId;


            //if (!LastDirection.ContainsKey(accountId))
            //    LastDirection.Add(accountId, direction);

            //var lastDirectionDist = Vector3.Distance(LastDirection[accountId], direction);
            //var lastDirectionDot = Vector3.Dot(LastDirection[accountId], direction);
            //// if its fuck all, just ignore this shit
            //if (lastDirectionDist < 0.009 || lastDirectionDot < 0.009)
            //{
            //    //Logger.LogInfo("removing direction shit");
            //    prc.DequeueAllMovementPackets();
            //    prc.LastMovementPacket = null;
            //    LastDirection[accountId] = direction;
            //    runOriginal = true;
            //}

            //if (!Sequence.ContainsKey(accountId))
            //    Sequence.Add(accountId, 0);

            //Sequence[accountId]++;

            ////prc.LastMovementPacket = null;

            //var timeToWait = __instance.IsAI ? -1000 : -66;

            //if (!LastPacketSent.ContainsKey(accountId) || LastPacketSent[accountId] < DateTime.Now.AddMilliseconds(timeToWait))
            //{
            //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //    dictionary.Add("seq", Sequence[accountId]);

            //    var newPost = __instance.Transform.position;
            //    newPost.x += __instance.MovementContext.Velocity.normalized.x * 0.05f;
            //    newPost.y += __instance.MovementContext.Velocity.normalized.y * 0.05f;
            //    newPost.z += __instance.MovementContext.Velocity.normalized.z * 0.05f;
            //    //newPost.x += __instance.MovementContext.Velocity.normalized.x * 0.01f;
            //    //newPost.x += __instance.MovementContext.MovementDirection.x;
            //    //newPost.y += __instance.MovementContext.MovementDirection.y;

            //    dictionary.Add("nPx", newPost.x);
            //    dictionary.Add("nPy", newPost.y);
            //    dictionary.Add("nPz", newPost.z);
            //    dictionary.Add("dX", direction.x.ToString());
            //    dictionary.Add("dY", direction.y.ToString());
            //    dictionary.Add("vel", __instance.MovementContext.Velocity);
            //    dictionary.Add("spd", __instance.MovementContext.CharacterMovementSpeed);
            //    //dictionary.Add("mdX", __instance.MovementContext.MovementDirection.x);
            //    //dictionary.Add("mdY", __instance.MovementContext.MovementDirection.y);
            //    dictionary.Add("arX", __instance.Rotation.x);
            //    dictionary.Add("arY", __instance.Rotation.y);
            //    //dictionary.Add("sprint", __instance.MovementContext.IsSprintEnabled);
            //    dictionary.Add("m", "Move");
            //    //Task.Run(delegate
            //    //{
            //    ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //    //});

            //    LastDirection[accountId] = direction;

            //    if (!LastPacketSent.ContainsKey(accountId))
            //        LastPacketSent.Add(accountId, DateTime.Now);

            //    LastPacketSent[accountId] = DateTime.Now;
            //}
            //return runOriginal;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 direction)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            var accountId = __instance.Profile.AccountId;

            if (!LastDirection.ContainsKey(__instance.Profile.AccountId))
                LastDirection.Add(__instance.Profile.AccountId, null);

            if (LastDirection[__instance.Profile.AccountId] != direction)
            {
                LastDirection[__instance.Profile.AccountId] = direction;

                var timeToWait = __instance.IsAI ? -1000 : -5;
               
                if (!LastPacketSent.ContainsKey(accountId) || LastPacketSent[accountId] < DateTime.Now.AddMilliseconds(timeToWait))
                {
                    if (!Sequence.ContainsKey(accountId))
                        Sequence.Add(accountId, 0);

                    Sequence[accountId]++;

                    Logger.LogInfo(__instance.Profile.AccountId + " " + direction);
                    __instance.CurrentState.Move(direction);
                    __instance.InputDirection = direction;

                    if (!LastPacketSent.ContainsKey(accountId))
                        LastPacketSent.Add(accountId, DateTime.Now);

                    LastPacketSent[accountId] = DateTime.Now;

                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary.Add("seq", Sequence[accountId]);
                    dictionary.Add("dX", direction.x.ToString());
                    dictionary.Add("dY", direction.y.ToString());
                    dictionary.Add("m", "Move");
                    ServerCommunication.PostLocalPlayerData(__instance, dictionary);
                    // setup prediction of outcome
                    prc.DequeueAllMovementPackets();
                    prc.LastMovementPacket = dictionary;
                }
            }

            // This forces the game to use whatever local input is pushed.
            // Problem with running this at the same time as receiving data is that one overrides the other's inertia shit and breaks it
            // Lets only do this when stopping.
            //if (direction == Vector2.zero)
            //{
            //    __instance.CurrentState.Move(direction);
            //    __instance.InputDirection = direction;
            ////}

            //var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            //var accountId = __instance.Profile.AccountId;

            //var timeToWait = __instance.IsAI ? -1000 : -66;
            //if(direction == Vector2.zero)
            //    timeToWait = 2000;

            //if (!LastPacketSent.ContainsKey(accountId) || LastPacketSent[accountId] < DateTime.Now.AddMilliseconds(timeToWait))
            //{
            //    if (!Sequence.ContainsKey(accountId))
            //        Sequence.Add(accountId, 0);

            //    Sequence[accountId]++;

            //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //    var newPost = __instance.Transform.position;
            //    newPost.x += __instance.MovementContext.Velocity.normalized.x * 0.05f;
            //    newPost.y += __instance.MovementContext.Velocity.normalized.y * 0.05f;
            //    newPost.z += __instance.MovementContext.Velocity.normalized.z * 0.05f;
            //    dictionary.Add("seq", Sequence[accountId]);
            //    dictionary.Add("nPx", newPost.x);
            //    dictionary.Add("nPy", newPost.y);
            //    dictionary.Add("nPz", newPost.z);
            //    dictionary.Add("dX", direction.x.ToString());
            //    dictionary.Add("dY", direction.y.ToString());
            //    dictionary.Add("vel", __instance.MovementContext.Velocity);
            //    dictionary.Add("spd", __instance.MovementContext.CharacterMovementSpeed);
            //    dictionary.Add("arX", __instance.Rotation.x);
            //    dictionary.Add("arY", __instance.Rotation.y);
            //    dictionary.Add("m", "Move");
            //    ServerCommunication.PostLocalPlayerData(__instance, dictionary);

            //    if (!LastPacketSent.ContainsKey(accountId))
            //        LastPacketSent.Add(accountId, DateTime.Now);

            //    LastPacketSent[accountId] = DateTime.Now;
            //}
        }

        public static void MoveReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            var accountId = player.Profile.AccountId;

            //Logger.LogInfo($"{accountId} MoveReplicated!");
            //var newPos = Vector3.zero;
            //newPos.x = float.Parse(dict["nPx"].ToString());
            //newPos.y = float.Parse(dict["nPy"].ToString());
            //newPos.z = float.Parse(dict["nPz"].ToString());

            Vector2 direction = new Vector2(float.Parse(dict["dX"].ToString()), float.Parse(dict["dY"].ToString()));
            player.CurrentState.Move(direction);
            player.InputDirection = direction;


            //if (long.Parse(dict["t"].ToString()) > DateTime.Now.AddSeconds(-1).Ticks) 
            //{
            //    //if (newPos != Vector3.zero && Vector3.Distance(player.Position, newPos) > 4.0f)
            //    //{
            //    //    player.Transform.position = newPos;
            //    //}

            //    //Vector2 rotation = new Vector2(float.Parse(dict["arX"].ToString()), float.Parse(dict["arY"].ToString()));
            //    //PatchConstants.SetFieldOrPropertyFromInstance<Vector2>(player, "Rotation", rotation);
            //}


            //// handle rotation
            //if (player.IsAI || IsMyPlayer(player))
            //    return;


        }


    }
}
