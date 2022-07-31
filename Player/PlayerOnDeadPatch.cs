using EFT;
using SIT.Coop.Core.LocalGame;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    /// <summary>
    /// This on dead patch removes people from the CoopGameComponent Players list
    /// </summary>
    public class PlayerOnDeadPatch : ModulePatch
    {
        public PlayerOnDeadPatch(BepInEx.Configuration.ConfigFile config)
        {
        }

        protected override MethodBase GetTargetMethod() => typeof(EFT.Player)
            .GetMethod("OnDead", BindingFlags.NonPublic | BindingFlags.Instance);

        [PatchPostfix]
        public static void PatchPostfix(EFT.Player __instance, EDamageType damageType)
        {
            CoopGameComponent.Players.TryRemove(__instance.Profile.AccountId, out _);
        }
    }
}
