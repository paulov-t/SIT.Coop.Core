using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player.Weapon
{
    internal class WeaponOnTriggerPressedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //foreach (var tt in PatchConstants.EftTypes.Where(x => x.Name.Contains("FirearmController")))
            //{
            //    Logger.LogInfo(tt.FullName);
            //}
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player+FirearmController");
            if (t == null)
                Logger.LogInfo($"WeaponOnTriggerPressedPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "SetTriggerPressed");

            Logger.LogInfo($"WeaponOnTriggerPressedPatch:{t.Name}:{method.Name}");
            return method;
        }

        //[PatchPrefix]
        //public static void PatchPrefix(object gesture)
        //{
        //    Logger.LogInfo("OnGesturePatch.PatchPrefix");
        //}

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player.ItemHandsController __instance,
            bool pressed
            )
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("pressed", pressed);
            dictionary.Add("m", "SetTriggerPressed");

            var player = PatchConstants.GetAllFieldsForObject(__instance).Single(x => x.Name == "_player").GetValue(__instance);
            ServerCommunication.PostLocalPlayerData(player, dictionary);
        }
    }
}
