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
    internal class PlayerOnSayPatch : ModulePatch
    {
        /// <summary>
		/// public override void Say(EPhraseTrigger @event, bool demand = false, float delay = 0f, ETagStatus mask = (ETagStatus)0, int probability = 100, bool aggressive = false)
        /// 
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"OnGesturePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => 
                x.GetParameters().Length >= 3
                && x.GetParameters()[0].Name.Contains("event")
                && x.GetParameters()[1].Name.Contains("demand")
                && x.GetParameters()[2].Name.Contains("delay")
                );

            Logger.LogInfo($"PlayerOnSayPatch:{t.Name}:{method.Name}");
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
            object @event)
        {
            //Logger.LogInfo("PlayerOnSayPatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("event", @event);
            dictionary.Add("m", "Say");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //Logger.LogInfo("PlayerOnSayPatch.PatchPostfix:Sent");

        }

        public static void SayReplicated(EFT.Player player, Dictionary<string, object> packet)
        {

        }
    }
}
