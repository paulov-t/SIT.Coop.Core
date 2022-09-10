using Comfort.Common;
using EFT.InventoryLogic;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnEnableSprintPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnEnableSprintPatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "EnableSprint");

            Logger.LogInfo($"PlayerOnEnableSprintPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, bool enable)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("enable", enable.ToString());
            args.Add("m", "EnableSprint");

            ServerCommunication.PostLocalPlayerData(__instance, args);

        }

        public static void Replicated(EFT.Player player, Dictionary<string, object> packet)
        {
            if (player == null)
                return;

            if (bool.TryParse(packet["enable"].ToString(), out var enable))
            {
                player.EnableSprint(enable);
            }
        }

    }
}
