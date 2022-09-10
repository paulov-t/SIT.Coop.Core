using EFT.InventoryLogic;
using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using Comfort.Common;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnSetItemInHandsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnSetItemInHandsPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "SetItemInHands"
                );

            Logger.LogInfo($"PlayerOnSetItemInHandsPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, Item item)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer )
                return;

            if (item == null)
                return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("item.id", item.Id);
            dictionary.Add("item.tpl", item.TemplateId);
            dictionary.Add("m", "SetItemInHands");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);

        }

        internal static void SetItemInHandsReplicated(LocalPlayer player, Dictionary<string, object> packet)
        {
            PlayerOnTryProceedPatch.Replicated(player, packet);

            //var item = player.Profile.Inventory.GetAllItemByTemplate(packet["item.tpl"].ToString()).FirstOrDefault();
            //if(item != null)
            //{
            //    PatchConstants.Logger.LogInfo($"SetItemInHandsReplicated: Attempting to set item of tpl {packet["item.tpl"].ToString()}");
               
            //    if (item is EFT.InventoryLogic.Weapon weapon)
            //        player.Proceed(weapon, null, false);

            //    // Knife wont work?! Needs some weird casting
            //    //if (item is EFT.InventoryLogic.KnifeComponent knife)
            //    //    player.Proceed(weapon, null, false);

            //    if (item is CurUsingMeds meds)
            //        player.Proceed(meds, EBodyPart.Common, null, 0, false);
            //}
            //else
            //{
            //    PatchConstants.Logger.LogError($"SetItemInHandsReplicated: Could not find item of tpl {packet["item.tpl"].ToString()}");
            //}

        }
    }
}
