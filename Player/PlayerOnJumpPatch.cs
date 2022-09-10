using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnJumpPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnJumpPatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "Jump");

            Logger.LogInfo($"PlayerOnJumpPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("m", "Jump");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);

        }

        public static void Replicated(EFT.Player player, Dictionary<string, object> packet)
        {
            if (player == null)
                return;

            player.CurrentState.Jump();
        }
    }
}
