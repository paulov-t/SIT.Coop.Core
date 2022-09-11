using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SIT.Coop.Core.Player.Weapon;
using System.IO;

namespace SIT.Coop.Core.Player
{
    internal class WeaponOnReloadMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player.FirearmController);
            if (t == null)
                Logger.LogInfo($"WeaponOnReloadMagPatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "ReloadMag");

            Logger.LogInfo($"WeaponOnReloadMagPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            //return true;
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            IShootController __instance
            , Magazine magazine
            , GridItemAddress gridItemAddress)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            Logger.LogInfo("WeaponOnReloadMagPatch.PatchPostfix");
            //try
            //{
            //    var serializedMag = magazine.SITToJson();
            //    Logger.LogInfo("Mag has been serialized!");
            //    var serializedGridItemAddress = gridItemAddress.SITToJson();
            //    Logger.LogInfo("GridItemAddress has been serialized!");
            //}
            //catch (Exception)
            //{

            //}

            var player = PatchConstants.GetAllFieldsForObject(__instance).First(x => x.Name == "_player").GetValue(__instance) as EFT.Player;
            if (player == null)
                return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("m.id", magazine.Id);
            dictionary.Add("m.tpl", magazine.TemplateId);
            dictionary.Add("c.pid", gridItemAddress.Container.ParentItem.Id);
            dictionary.Add("c.id", gridItemAddress.Container.ID);
            dictionary.Add("g.x", gridItemAddress.LocationInGrid.x);
            dictionary.Add("g.y", gridItemAddress.LocationInGrid.y);
            dictionary.Add("g.r", gridItemAddress.LocationInGrid.r.ToString());
            dictionary.Add("m", "ReloadMag");
            ServerCommunication.PostLocalPlayerData(player, dictionary);

        }

        public static void Replicated(EFT.Player player, Dictionary<string, object> packet)
        {
            if (player == null)
                return;

            var firearmController = WeaponOnTriggerPressedPatch.GetFirearmController(player);
            if (firearmController == null)
                return;

            var currentOp = WeaponOnTriggerPressedPatch.GetCurrentOperation(player);
            if (currentOp == null)
                return;

            var reloadMagMethod = PatchConstants.GetMethodForType(currentOp.GetType(), "ReloadMag");
            if (reloadMagMethod != null)
            {
                Logger.LogInfo("Replicated - Found ReloadMag method!");

                // Find Mag Container for Grid

                // Create Location In Grid
                Enum.TryParse<ItemRotation>(packet["g.r"].ToString(), out ItemRotation rotation);
                var locInGrid = new LocationInGrid(
                    int.Parse(packet["g.x"].ToString())
                    , int.Parse(packet["g.y"].ToString())
                    , rotation
                    , true
                    );

                // Find Grid
                GridItemAddressDescriptor gridItemAddressDescriptor = new GridItemAddressDescriptor();
                gridItemAddressDescriptor.LocationInGrid = locInGrid;
                gridItemAddressDescriptor.Container = new ContainerDescriptor() { ContainerId = packet["c.id"].ToString(), ParentId = packet["c.pid"].ToString() };
                var gridItemAddress = player.InventoryController.ToGridItemAddress(gridItemAddressDescriptor);
                if (gridItemAddress == null)
                {
                    Logger.LogInfo("Replicated. Unable to create GridItemAddress. Ignoring!");
                    return;
                }
                // Create GridItemAddress
                //var gridItemAddress = new GridItemAddress(null, )

                // Get Magazine
                //var field = PatchConstants.GetFieldFromTypeByFieldType(player.GetType(), typeof(InventoryController));
                var magId = packet["m.id"].ToString();
                var magTemplateId = packet["m.tpl"].ToString();
                var listOfMags = new List<Magazine>(10);
                player.InventoryController.GetAcceptableItemsNonAlloc(Equipment.AllSlotNames, listOfMags
                    , (Magazine mag)
                        => mag.TemplateId == magTemplateId
                    );
                if (listOfMags.Count == 0)
                {
                    Logger.LogInfo("Replicated. Unable to find Magazine on person. Ignoring!");
                    return;
                }
                listOfMags = listOfMags.OrderByDescending(x => x.StackObjectsCount).ToList();
                Magazine magazine = listOfMags.FirstOrDefault();
                if (magazine == null)
                {
                    Logger.LogInfo("Replicated. Unable to find Magazine on person. Ignoring!");
                    return;
                }

                Logger.LogInfo("Replicated. Found Magazine. Sweet!");

                // CurrentOperation->Reload
                reloadMagMethod.Invoke(currentOp, new object[] { magazine, gridItemAddress, null, null });
            }

        }
    }
}
