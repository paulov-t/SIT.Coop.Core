using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player.Weapon
{
    internal class WeaponOnTriggerPressedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //foreach (var tt in PatchConstants.EftTypes.Where(x => x.Name.Contains("FirearmController")))
            //{
            //    Logger.LogInfo(tt.FullName);
            //}
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player+FirearmController");
            if (t == null)
                Logger.LogInfo($"WeaponOnTriggerPressedPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "SetTriggerPressed");

            //Logger.LogInfo($"WeaponOnTriggerPressedPatch:{t.Name}:{method.Name}");
            return method;
        }

        //[PatchPrefix]
        //public static bool PatchPrefix(
        //    EFT.Player.ItemHandsController __instance,
        //    bool pressed
        //    )
        //{
        //    return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        //}

        private static Dictionary<string, DateTime> lastTriggerPressedPacketSent = new Dictionary<string, DateTime>();


        [PatchPostfix]
        public static void PatchPostfix(
            //EFT.Player.ItemHandsController __instance,
            EFT.Player.FirearmController __instance,
            bool pressed
            )
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer || BlockPressed)
                return;

            var player = PatchConstants.GetAllFieldsForObject(__instance).First(x => x.Name == "_player").GetValue(__instance) as EFT.Player;
            if (!lastTriggerPressedPacketSent.ContainsKey(player.Profile.AccountId) || lastTriggerPressedPacketSent[player.Profile.AccountId] < DateTime.Now.AddSeconds(-0.2))
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("pressed", pressed);
                dictionary.Add("m", "SetTriggerPressed");

                ServerCommunication.PostLocalPlayerData(player, dictionary);

                if (!lastTriggerPressedPacketSent.ContainsKey(player.Profile.AccountId))
                    lastTriggerPressedPacketSent.Add(player.Profile.AccountId, DateTime.Now);
                else
                    lastTriggerPressedPacketSent[player.Profile.AccountId] = DateTime.Now;

            }
        }

        public static EFT.Player.FirearmController GetFirearmController(EFT.Player player)
        {
            if (player.HandsController is EFT.Player.FirearmController)
            {
                return player.HandsController as EFT.Player.FirearmController;
            }
            return null;
        }

        private static List<Dictionary<string, object>> ReceivedPackets = new List<Dictionary<string, object>>();
        public static bool BlockPressed = false;

        public static void WeaponOnTriggerPressedReplicated(EFT.Player player, Dictionary<string, object> packet)
        {
            // I send packets, don't receive it
            if (lastTriggerPressedPacketSent.ContainsKey(player.Profile.AccountId))
                return;

            if (packet.ContainsKey("pressed"))
            {
                var firearmController = GetFirearmController(player);
                if (firearmController != null && bool.TryParse(packet["pressed"].ToString(), out var pressed))
                {
                    BlockPressed = true;
                    firearmController.SetTriggerPressed(pressed);
                    BlockPressed = false;
                }
            }
            //ReceivedPackets.Add(packet);
            //ReceivedPackets = ReceivedPackets.OrderBy(x => (float)x["t"]).ToList();
            //for (var i = 0; i < ReceivedPackets.Count; i++)  
            //{
            //    Logger.LogInfo("Processing SetTriggerPressed");
            //    var cPacket = ReceivedPackets[i];
            //    if (cPacket["m"].ToString() != "SetTriggerPressed")
            //        continue;
            //    var firearmController = GetFirearmController(player);
            //    if (firearmController != null)
            //    {
            //        firearmController.SetTriggerPressed(bool.Parse(cPacket["pressed"].ToString()));
            //    }
            //}
            //ReceivedPackets.Clear();

        }
    }
}
