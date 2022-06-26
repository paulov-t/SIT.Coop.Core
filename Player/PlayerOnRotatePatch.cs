using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SIT.Coop.Core.LocalGame;

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

        public static Dictionary<string, DateTime> LastStoredMoveTime { get; } = new Dictionary<string, DateTime>();

        [PatchPrefix]
        public static bool PatchPrefix(
            EFT.Player __instance,
            Vector2 deltaRotation
            , bool ignoreClamp)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return true;

            if (__instance.IsAI)
                return true;

            if (LocalGamePatches.MyPlayer == null)
                return true;

            if (LocalGamePatches.MyPlayerProfile == null)
                return true;

            return __instance.Profile.AccountId == LocalGamePatches.MyPlayerProfile.AccountId;
            //return false;

            //// Get the replicated component for movement
            //var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            //if (prc == null) // If the replicated component doesn't exist (i.e. its SP), run as normal
            //    return true;

            //return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 deltaRotation
            , bool ignoreClamp)
        {
            var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            if (prc == null)
                return;

            if (deltaRotation.IsAnyComponentInfinity() || deltaRotation.IsAnyComponentNaN())
                return;

            if (deltaRotation.SqrMagnitude() < 0.02f)
                return;

            if (prc.LastRotationPacketPostTime.HasValue && prc.LastRotationPacketPostTime > DateTime.Now.AddSeconds(-0.5)) 
            {
                prc.ClientListRotationsToSend.Add(deltaRotation);
                return;
            }

            //if (prc.LastRotationPacket != null
            //  && prc.LastRotationPacket["arX"].ToString() == deltaRotation.x.ToString()
            //  && prc.LastRotationPacket["arY"].ToString() == deltaRotation.y.ToString()
            //  )
            //    return;


            //Logger.LogInfo("PlayerOnSayPatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //dictionary.Add("arX", __instance.MovementContext.Rotation.x);
            //dictionary.Add("arY", __instance.MovementContext.Rotation.y);
            //dictionary.Add("arX", deltaRotation.x);
            //dictionary.Add("arY", deltaRotation.y);
            //dictionary.Add("ignoreClamp", ignoreClamp.ToString());
            //dictionary.Add("m", "Rotate");
            //dictionary.Add("batch", prc.ClientListRotationsToSend.Distinct());
            //dictionary.Add("m", "RotateBatch");
            //Task.Run(delegate
            //{
            //    ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //    prc.LastRotationPacketPostTime = DateTime.Now;
            //    prc.ClientListRotationsToSend.Clear();
            //});
        }

        //public static void RotateReplicated(EFT.Player player, Dictionary<string, object> dict)
        //{
        //    Vector2 rot = Vector2.zero;
        //    rot.x = float.Parse(dict["arX"].ToString());
        //    rot.y = float.Parse(dict["arY"].ToString());
        //    bool ignoreClamp = bool.Parse(dict["ignoreClamp"].ToString());

        //    if (!rot.IsAnyComponentInfinity() && !rot.IsAnyComponentNaN())
        //    {
        //        player.CurrentState.Rotate(rot, ignoreClamp);
        //        //player.MovementContext.Rotation = rot;
        //    }
        //}

        //public static void RotateReplicatedV(EFT.Player player, Vector2 delta)
        //{
        //    if (player.Profile.AccountId == LocalGamePatches.MyPlayerProfile.AccountId)
        //        return;

        //    if (!delta.IsAnyComponentInfinity() && !delta.IsAnyComponentNaN())
        //    {
        //        player.CurrentState.Rotate(delta, false);
        //    }
        //}
    }
    
}
