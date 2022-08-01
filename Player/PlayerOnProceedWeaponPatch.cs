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

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, EFT.InventoryLogic.Weapon weapon)
        {
            Logger.LogInfo($"PlayerOnProceedWeaponPatch:Patch");

        }
    }
}
