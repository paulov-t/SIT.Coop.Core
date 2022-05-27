using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Z.Coop.Core.Player
{
    internal class PlayerOnGesturePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"OnGesturePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length >= 1 && x.GetParameters()[0].Name.Contains("gesture"));

            Logger.LogInfo($"OnGesturePatch:{t.Name}:{method.Name}");
            return method;
        }

        //[PatchPrefix]
        //public static void PatchPrefix(object gesture)
        //{
        //    Logger.LogInfo("OnGesturePatch.PatchPrefix");
        //}

        [PatchPostfix]
        public static void PatchPostfix(
            object __instance,
            object gesture)
        {
            Logger.LogInfo("OnGesturePatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("gesture", gesture);
            dictionary.Add("m", "Gesture");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            Logger.LogInfo("OnGesturePatch.PatchPostfix:Sent");

        }
    }
}
