using EFT;
using SIT.Coop.Core.LocalGame;
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
    /// <summary>
    /// This on dead patch removes people from the CoopGameComponent Players list
    /// </summary>
    public class PlayerOnDeadPatch : ModulePatch
    {
        public PlayerOnDeadPatch(BepInEx.Configuration.ConfigFile config)
        {
        }

        protected override MethodBase GetTargetMethod() => PatchConstants.GetMethodForType(typeof(EFT.Player), "OnDead");

        [PatchPostfix]
        public static void PatchPostfix(EFT.Player __instance, EDamageType damageType)
        {
            if(CoopGameComponent.Players != null)
                CoopGameComponent.Players.TryRemove(__instance.Profile.AccountId, out _);

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("m", "Dead");
            var generatedDict = ServerCommunication.PostLocalPlayerData(__instance, dictionary);
        }
    }
}
