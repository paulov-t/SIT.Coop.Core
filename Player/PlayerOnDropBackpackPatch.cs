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
    internal class PlayerOnDropBackpackPatch : ModulePatch
    {
        /// <summary>
        /// Targetting vmethod_1
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnDropBackpackPatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "DropBackpack");

            Logger.LogInfo($"PlayerOnDropBackpackPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance)
        {
            Logger.LogInfo("PlayerOnDropBackpackPatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("p.equip", __instance.Profile.Inventory.Equipment.SITToJson());
            dictionary.Add("m", "DropBackpack");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            Logger.LogInfo("PlayerOnDropBackpackPatch.PatchPostfix:Sent");
        }

        public static void Replicated(
            EFT.Player player,
            Dictionary<string, object> packet)
        {
        }
    }
}
