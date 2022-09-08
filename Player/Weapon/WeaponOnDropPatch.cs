using EFT.InventoryLogic;
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
    internal class WeaponOnDropPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player+FirearmController");
            if (t == null)
                Logger.LogInfo($"WeaponOnDropPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "Drop");

            //Logger.LogInfo($"WeaponOnDropPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            object __instance,
            float animationSpeed, Action callback, bool fastDrop, Item nextControllerItem
            )
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("animationSpeed", animationSpeed);
            dictionary.Add("fastDrop", fastDrop);
            if (nextControllerItem != null)
            {
                dictionary.Add("nextControllerItem.Id", nextControllerItem.Id);
                dictionary.Add("nextControllerItem.Tpl", nextControllerItem.TemplateId);
            }
            dictionary.Add("m", "Drop");

            var player = PatchConstants.GetAllFieldsForObject(__instance).Single(x => x.Name == "_player").GetValue(__instance);
            ServerCommunication.PostLocalPlayerData(player, dictionary);
        }
    }
}
