using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using EFT;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnInteractPatch : ModulePatch
    {

        /// <summary>
        /// Targetting vmethod_1
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnInteractPatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "Interact");

            Logger.LogInfo($"PlayerOnInteractPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            return true;
            //return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void Patch(
            EFT.Player __instance)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            Logger.LogInfo("PlayerOnInteractPatch.PatchPostfix");
            //Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //dictionary.Add("doorId", door.Id);
            //dictionary.Add("type", interactionResult.InteractionType.ToString());
            //dictionary.Add("m", "Door");
            //ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //Logger.LogInfo("OnInteractWithDoorPatch.PatchPostfix:Sent");
        }


        //public static void Replicated(
        //    EFT.Player player,
        //    Dictionary<string, object> packet)
        //{
        //    var comp = player.GetComponent<PlayerReplicatedComponent>();
        //    if(comp == null)
        //    {
        //        return;
        //    }

        //    Enum.TryParse<EInteractionType>(packet["type"].ToString(), out EInteractionType interactionType);

        //    var foundDoor = comp.ListOfInteractiveObjects
        //        .FirstOrDefault(
        //        x => x.Id == packet["doorId"].ToString());
        //    if(foundDoor == null)
        //    {
        //        return;
        //    }

        //    foundDoor.Interact(new InteractionResult(interactionType));

        //}
    }
}
