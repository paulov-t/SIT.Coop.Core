using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.Player
{
    public class PlayerOnRotatePatch : ModulePatch
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
                Logger.LogInfo($"PlayerOnRotatePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "Rotate"
                );

            Logger.LogInfo($"PlayerOnRotatePatch:{t.Name}:{method.Name}");
            return method;
        }

        //[PatchPrefix]
        //public static void PatchPrefix(object gesture)
        //{
        //    Logger.LogInfo("OnGesturePatch.PatchPrefix");
        //}

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 deltaRotation
            , bool ignoreClamp)
        {
            if (deltaRotation.IsAnyComponentInfinity() || deltaRotation.IsAnyComponentNaN())
                return;

                //Logger.LogInfo("PlayerOnSayPatch.PatchPostfix");
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("arX", __instance.MovementContext.Rotation.x);
            dictionary.Add("arY", __instance.MovementContext.Rotation.y);
            dictionary.Add("m", "Rotate");
            Task.Run(delegate
            {
                ServerCommunication.PostLocalPlayerData(__instance, dictionary);
                //MatchMakerAcceptScreen.ServerCommunicationCoopImplementation.PostLocalPlayerData(this, dictionary);
            });
            //Logger.LogInfo("PlayerOnSayPatch.PatchPostfix:Sent");

        }

        public static void RotateReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            try
            {
                Vector2 rot = Vector2.zero;
                rot.x = float.Parse(dict["arX"].ToString());
                rot.y = float.Parse(dict["arY"].ToString());

                if (!rot.IsAnyComponentInfinity() && !rot.IsAnyComponentNaN())
                {
                    player.CurrentState.Rotate(rot, false);
                }
            }
            catch(Exception ex)
            {

            }
        }
    }
    
}
