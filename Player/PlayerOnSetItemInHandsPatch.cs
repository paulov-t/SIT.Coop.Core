using EFT.InventoryLogic;
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
    }
}
