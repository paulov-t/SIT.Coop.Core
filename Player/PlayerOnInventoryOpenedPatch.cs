using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using System.Collections.Concurrent;

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

        public static ConcurrentDictionary<EFT.Player, bool> LastOpenedValue = new ConcurrentDictionary<EFT.Player, bool>();

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            bool opened)
        {
            if(
                !LastOpenedValue.ContainsKey(__instance)
                || (LastOpenedValue.ContainsKey(__instance) && LastOpenedValue[__instance] != opened)) 
            {
                //Logger.LogInfo("PlayerOnInventoryOpenedPatch.PatchPostfix");
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("opened", opened);
                dictionary.Add("m", "InventoryOpened");
                ServerCommunication.PostLocalPlayerData(__instance, dictionary);
                //Logger.LogInfo("PlayerOnInventoryOpenedPatch.PatchPostfix:Sent");
                LastOpenedValue.TryAdd(__instance, opened);
                LastOpenedValue[__instance] = opened;
            }

        }

        internal static void InventoryOpenedReplicated(LocalPlayer player, Dictionary<string, object> dictionary)
        {
            //Logger.LogInfo("InventoryOpened");
            player.SetInventoryOpened(bool.Parse(dictionary["opened"].ToString()));
        }
    }
}
