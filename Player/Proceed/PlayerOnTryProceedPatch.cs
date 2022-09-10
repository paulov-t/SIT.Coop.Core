using Comfort.Common;
using EFT.InventoryLogic;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnTryProceedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnTryProceedPatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "TryProceed");

            Logger.LogInfo($"PlayerOnTryProceedPatch:{t.Name}:{method.Name}");
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
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            if (item == null)
                return;

            Logger.LogInfo($"PlayerOnTryProceedPatch:Patch");

            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("m", "TryProceed");
            args.Add("item.id", item.Id);
            args.Add("item.tpl", item.TemplateId);

            var dictProcessed = ServerCommunication.PostLocalPlayerData(__instance, args);
            Replicated(__instance, args);

        }

        private static ConcurrentBag<string> Processed = new ConcurrentBag<string>();

        public static void Replicated(EFT.Player player, Dictionary<string, object> packet)
        {
            var t = packet["t"].ToString();
            if (Processed.Contains(t))
                return;

            Processed.Add(t);
            Logger.LogInfo($"PlayerOnTryProceedPatch:Replicated");

            var item = player.Profile.Inventory.GetAllItemByTemplate(packet["item.tpl"].ToString()).FirstOrDefault();
            if (item != null)
            {
                //player.TryProceed(item, null, false);
                    if (item is EFT.InventoryLogic.Weapon weapon)
                    {
                        player.Proceed(weapon, delegate (Result<IShootController> result)
                        {
                            EFT.Player.smethod_0(result, null);
                        }, false);
                        return;
                    }
                    if (item is ThrowWeap potentialGrenade)
                    {
                    ThrowWeap throwWeap = potentialGrenade;
                        player.Proceed(throwWeap, delegate (Result<GInterface104> result)
                        {
                            EFT.Player.smethod_0(result, null);
                        }, false);
                        return;
                    }
                    if (item is Meds meds)
                    {
                        player.Proceed(meds, EBodyPart.Common, null, item.GetRandomAnimationVariant(), false);
                        return;
                    }
                if (item is FoodDrink foodDrink)
                {
                    FoodDrink foodDrink2 = foodDrink;
                    player.Proceed(foodDrink2, 1f, null, item.GetRandomAnimationVariant(), false);
                    return;
                }

                PlayerOnProceedKnifePatch.ProceedWeaponReplicated(player, packet);
                //KnifeComponent itemComponent = item.GetItemComponent<KnifeComponent>();
                //if (itemComponent != null)
                //{
                //    this.Proceed(itemComponent, delegate (Result<IKnifeHandsController> result)
                //    {
                //        Player.smethod_0(result, CS$<> 8__locals0.completeCallback);
                //    }, scheduled);
                //}
            }

        }
    }
}
