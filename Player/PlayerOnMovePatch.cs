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

            Logger.LogInfo($"PlayerOnMovePatch:{t.Name}:{method.Name}");
            return method;
        }

        public static DateTime? LastMoveTime { get; set; }
        public static DateTime? LastAIMovesTime { get; set; }

        public static List<Vector2> StoredMoves = new List<Vector2>();
        public static DateTime? LastStoredMoveTime { get; set; }

        [PatchPrefix]
        public static bool PatchPrefix(
            EFT.Player __instance,
            Vector2 direction)
        {
            return true;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 direction)
        {
            if (direction == Vector2.zero)
                return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("nP", __instance.Transform.position);
            dictionary.Add("dX", direction.x.ToString());
            dictionary.Add("dY", direction.y.ToString());
            dictionary.Add("vel", __instance.MovementContext.Velocity);
            dictionary.Add("spd", __instance.MovementContext.CharacterMovementSpeed);
            dictionary.Add("mdX", __instance.MovementContext.MovementDirection.x);
            dictionary.Add("mdY", __instance.MovementContext.MovementDirection.y);
            dictionary.Add("sprint", __instance.MovementContext.IsSprintEnabled);
            dictionary.Add("m", "Move");
            Task.Run(delegate
            {
               ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            });
        }

        public static void MoveReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            try
            {
                Vector2 direction = new Vector2(float.Parse(dict["dX"].ToString()), float.Parse(dict["dY"].ToString()));
                direction.x = Math.Sign(direction.x);
                direction.y = Math.Sign(direction.y);
                player.MovementContext.MovementDirection = direction;

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
                if (dict.ContainsKey("spd"))
                    player.MovementContext.CharacterMovementSpeed = float.Parse(dict["spd"].ToString());
                player.MovementContext.PlayerAnimatorEnableInert(false);
            }
            catch(Exception ex)
            {
                Logger.LogInfo("PlayerOnMovePatch.MoveReplicated:ERROR:" + ex.Message);
            }
        }
    }
}
