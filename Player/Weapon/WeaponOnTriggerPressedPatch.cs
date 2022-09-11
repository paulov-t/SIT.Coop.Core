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

        [PatchPrefix]
        public static bool PatchPrefix(
            EFT.Player.FirearmController __instance,
            bool pressed
            )
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        private static Dictionary<string, float> lastTriggerPressedPacketSent = new Dictionary<string, float>();


        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player.FirearmController __instance,
            bool pressed
            )
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            var player = PatchConstants.GetAllFieldsForObject(__instance).First(x => x.Name == "_player").GetValue(__instance) as EFT.Player;
            Dictionary<string, object> packet = new Dictionary<string, object>();
            packet.Add("pressed", pressed);
            packet.Add("m", "SetTriggerPressed");

            var createdPacket = ServerCommunication.PostLocalPlayerData(player, packet);
            Replicated(player, createdPacket);
        }

        public static EFT.Player.FirearmController GetFirearmController(EFT.Player player)
        {
            if (player.HandsController is EFT.Player.FirearmController)
            {
                return player.HandsController as EFT.Player.FirearmController;
            }
            return null;
        }

        public static object GetCurrentOperation(EFT.Player player)
        {
            var firearmController = GetFirearmController(player);
            if (firearmController == null)
                return null;

            var currentOperation = PatchConstants.GetFieldOrPropertyFromInstance<object>(firearmController, "CurrentOperation", false);
            return currentOperation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="packet"></param>
        public static void Replicated(EFT.Player player, Dictionary<string, object> packet)
        {
            // get account id of the packet
            var aid = packet["accountId"].ToString();

            // get the packet timestamp
            var t = float.Parse(packet["t"].ToString());

            // add the aid to keys of sent packets
            if (!lastTriggerPressedPacketSent.ContainsKey(aid))
                lastTriggerPressedPacketSent.Add(aid, 0);

            // if the timestamp of the new packet less than the last received then ignore
            if (t < lastTriggerPressedPacketSent[aid])
                return;

            // set the last received packet for the aid
            lastTriggerPressedPacketSent[aid] = t;

            // must contain pressed key
            if (packet.ContainsKey("pressed"))
            {
                var firearmController = GetFirearmController(player);
                if (firearmController != null && bool.TryParse(packet["pressed"].ToString(), out var pressed))
                {
                    // -------------------------------------
                    // target what the following method does
                    // firearmController.SetTriggerPressed(pressed);
                    // -------------------------------------

                    var currentOperation = PatchConstants.GetFieldOrPropertyFromInstance<object>(firearmController, "CurrentOperation", false);
                    if (currentOperation != null) 
                    {
                        var setTriggerPressedMethod = PatchConstants.GetMethodForType(currentOperation.GetType(), "SetTriggerPressed");
                        if (setTriggerPressedMethod != null)
                        {
                            setTriggerPressedMethod.Invoke(currentOperation, new object[] { pressed });
                        }
                    }
                }
            }
        }
    }
}
