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
    internal class PlayerOnHealPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnHealPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "Heal");

            Logger.LogInfo($"PlayerOnHealPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            EBodyPart bodyPart, float value)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("bodyPart", bodyPart);
            dictionary.Add("value", value);
            dictionary.Add("m", "Heal");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);

        }
    }
}
