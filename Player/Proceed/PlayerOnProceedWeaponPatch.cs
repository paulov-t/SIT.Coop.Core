using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnProceedWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnProceedWeaponPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "Proceed"
                && x.GetParameters()[0].Name == "weapon"
                );

            Logger.LogInfo($"PlayerOnProceedWeaponPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, EFT.InventoryLogic.Weapon weapon)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            //Logger.LogInfo($"PlayerOnProceedWeaponPatch:Patch");

            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("m", "Proceed");
            args.Add("item.id", weapon.Id);
            args.Add("item.tpl", weapon.TemplateId);
            args.Add("pType", "Weapon");
            if (__instance.Profile.Inventory.Equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.FirstPrimaryWeapon).ContainedItem.Id == weapon.Id)
                args.Add("slot", "FirstPrimaryWeapon");
            else if (__instance.Profile.Inventory.Equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.SecondPrimaryWeapon).ContainedItem.Id == weapon.Id)
                args.Add("slot", "SecondPrimaryWeapon");
            else if (__instance.Profile.Inventory.Equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.Holster).ContainedItem.Id == weapon.Id)
                args.Add("slot", "Holster");
            else if (__instance.Profile.Inventory.Equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.Scabbard).ContainedItem.Id == weapon.Id)
                args.Add("slot", "Scabbard");

            ServerCommunication.PostLocalPlayerData(__instance, args);

        }

        public static void ProceedWeaponReplicated(EFT.Player player, Dictionary<string, object> packet)
        {
            if (player == null)
                return;

            var item = player.Profile.Inventory.GetAllItemByTemplate(packet["item.tpl"].ToString()).FirstOrDefault();
            if (item != null)
            {
                PatchConstants.Logger.LogInfo($"ProceedWeaponReplicated: Attempting to set item of tpl {packet["item.tpl"].ToString()}");
                if (item is EFT.InventoryLogic.Weapon weapon)
                    player.Proceed(weapon, null, false);
            }

        }
    }
}
