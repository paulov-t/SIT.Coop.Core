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
    internal class PlayerOnInteractWithDoorPatch : ModulePatch
    {
        /// <summary>
        /// Targetting vmethod_1
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"OnInteractWithDoorPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length == 2
                && x.GetParameters()[0].Name == "door"
                && x.GetParameters()[1].Name == "interactionResult"
                );

            Logger.LogInfo($"OnInteractWithDoorPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPostfix]
        public static void Patch(
            EFT.Player __instance,
            WorldInteractiveObject door
            , object interactionResult)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            Logger.LogInfo("OnInteractWithDoorPatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("doorId", door.Id);
            //    PatchConstants.GetFieldOrPropertyFromInstance<object>(door, "Id", false));
            //dictionary.Add("interactionResult", Enum.Parse(typeof(EInteractionType),
            //    PatchConstants.GetFieldOrPropertyFromInstance<string>(interactionResult, "InteractionType", false)).ToString());
            dictionary.Add("m", "Door");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //Logger.LogInfo("OnInteractWithDoorPatch.PatchPostfix:Sent");
        }


        [PatchPostfix]
        public static void Replicated(
            object __instance,
            object door
            , object interactionResult)
        {
        }
    }
}
