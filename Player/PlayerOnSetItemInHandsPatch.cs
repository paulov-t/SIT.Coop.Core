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

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, Item item)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("item.id", item.Id);
            dictionary.Add("item.tpl", item.TemplateId);
            dictionary.Add("m", "SetItemInHands");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
        }

        internal static void SetItemInHandsReplicated(LocalPlayer player, Dictionary<string, object> packet)
        {

            var item = player.Profile.Inventory.GetAllItemByTemplate(packet["item.tpl"].ToString()).FirstOrDefault();
            if(item != null)
            {
                PatchConstants.Logger.LogInfo($"SetItemInHandsReplicated: Attempting to set item of tpl {packet["item.tpl"].ToString()}");
                //player.TryProceed(item, delegate(Result<IHandsController> result) { 
                
                //    if(result.Succeed)
                //        PatchConstants.Logger.LogInfo($"SetItemInHandsReplicated: Result:Succeed");
                //    else if (result.Failed)
                //        PatchConstants.Logger.LogInfo($"SetItemInHandsReplicated: Result:Failed");

                //});
            }
            else
            {
                PatchConstants.Logger.LogError($"SetItemInHandsReplicated: Could not find item of tpl {packet["item.tpl"].ToString()}");
            }

        }
    }
}
