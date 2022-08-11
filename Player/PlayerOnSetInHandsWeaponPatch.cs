using EFT.InventoryLogic;
using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnSetInHandsWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnSetItemInHandsPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "SetInHands"
                && x.GetParameters()[0].Name == "weapon"
                );

            Logger.LogInfo($"PlayerOnSetInHandsWeaponPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, EFT.InventoryLogic.Weapon weapon)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //dictionary.Add("item.id", weapon.Id);
            //dictionary.Add("item.tpl", weapon.TemplateId);
            dictionary.Add("weapon", weapon.SITToJson());
            dictionary.Add("m", "SetInHands.Weapon");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
        }

        internal static void SetInHandsReplicated(LocalPlayer player, Dictionary<string, object> packet)
        {
            PatchConstants.Logger.LogInfo("PlayerOnSetInHandsWeaponPatch");
            var w = PatchConstants.SITParseJson<EFT.InventoryLogic.Weapon>(packet["weapon"].ToString());
            player.SetInHands(w, null);

        }
    }
}
