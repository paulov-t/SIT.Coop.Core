using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnInventoryOpenedPatch : ModulePatch
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
                Logger.LogInfo($"PlayerOnInventoryOpened:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "SetInventoryOpened"
                );

            Logger.LogInfo($"PlayerOnInventoryOpenedPatch:{t.Name}:{method.Name}");
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
            bool opened)
        {
            Logger.LogInfo("PlayerOnInventoryOpenedPatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("opened", opened);
            dictionary.Add("m", "InventoryOpened");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            Logger.LogInfo("PlayerOnInventoryOpenedPatch.PatchPostfix:Sent");

        }
    }
}
