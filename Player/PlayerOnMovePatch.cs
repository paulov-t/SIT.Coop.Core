using SIT.Coop.Core.Matchmaker;
using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using EFT;
using SIT.Coop.Core.LocalGame;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnMovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnMovePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x =>
                x.GetParameters().Length == 1
                && x.GetParameters()[0].Name.Contains("direction")
                && x.Name == "Move"
                );

            //Logger.LogInfo($"PlayerOnMovePatch:{t.Name}:{method.Name}");
            return method;
        }

        public static Dictionary<string, ulong> Sequence { get; } = new Dictionary<string, ulong>();
        public static Dictionary<string, DateTime> LastPacketSent { get; } = new Dictionary<string, DateTime>();
        public static Dictionary<string, long> LastPacketReceived { get; } = new Dictionary<string, long>();
        public static Dictionary<string, Vector2?> LastDirection { get; } = new Dictionary<string, Vector2?>();

        public static Dictionary<string, bool> ClientIsMoving { get; } = new Dictionary<string, bool>();

        public static bool IsMyPlayer(EFT.Player player) { return player == (LocalGamePatches.MyPlayer as EFT.Player); }

        [PatchPrefix]
        public static bool PatchPrefix(
            EFT.Player __instance,
            Vector2 direction)
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 direction)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            var prc = __instance.GetComponent<PlayerReplicatedComponent>();
            var accountId = __instance.Profile.AccountId;

            if (!LastDirection.ContainsKey(__instance.Profile.AccountId))
                LastDirection.Add(__instance.Profile.AccountId, null);

            if (LastDirection[__instance.Profile.AccountId] != direction)
            {
                LastDirection[__instance.Profile.AccountId] = direction;

                var timeToWait = __instance.IsAI ? -1000 : -5;
               
                if (!LastPacketSent.ContainsKey(accountId) || LastPacketSent[accountId] < DateTime.Now.AddMilliseconds(timeToWait))
                {
                    if (!Sequence.ContainsKey(accountId))
                        Sequence.Add(accountId, 0);

                    Sequence[accountId]++;

                    //Logger.LogInfo(__instance.Profile.AccountId + " " + direction);
                    __instance.CurrentState.Move(direction);
                    __instance.InputDirection = direction;

                    if (!LastPacketSent.ContainsKey(accountId))
                        LastPacketSent.Add(accountId, DateTime.Now);

                    LastPacketSent[accountId] = DateTime.Now;

                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary.Add("seq", Sequence[accountId]);
                    dictionary.Add("dX", direction.x.ToString());
                    dictionary.Add("dY", direction.y.ToString());
                    //dictionary.Add("pX", __instance.Position.x);
                    //dictionary.Add("pY", __instance.Position.y);
                    //dictionary.Add("pZ", __instance.Position.z);
                    //dictionary.Add("rX", __instance.Rotation.x);
                    //dictionary.Add("rY", __instance.Rotation.y);
                    dictionary.Add("m", "Move");
                    ServerCommunication.PostLocalPlayerData(__instance, dictionary);
                    // setup prediction of outcome
                    prc.DequeueAllMovementPackets();
                    prc.LastMovementPacket = dictionary;
                }
            }
        }

        public static void MoveReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            var accountId = player.Profile.AccountId;

            var thisPacket = dict;

            //Logger.LogInfo($"{accountId} MoveReplicated!");
            //var newPos = Vector3.zero;
            //newPos.x = float.Parse(dict["pX"].ToString());
            //newPos.y = float.Parse(dict["pY"].ToString());
            //newPos.z = float.Parse(dict["pZ"].ToString());
            //var newRot = Vector2.zero;
            //newRot.x = float.Parse(dict["rX"].ToString());
            //newRot.y = float.Parse(dict["rY"].ToString());


            Vector2 direction = new Vector2(float.Parse(dict["dX"].ToString()), float.Parse(dict["dY"].ToString()));
           

            var packetTime = long.Parse(dict["t"].ToString());

            // Is first packet OR is after the last packet received. This copes with unordered received packets
            if ((!LastPacketReceived.ContainsKey(accountId) || LastPacketReceived[accountId] <= packetTime))
            {
                player.CurrentState.Move(direction);
                player.InputDirection = direction;
                //Logger.LogInfo(accountId + ": move replicated");

                // Standard check to ensure position is correct
                //if (
                //    // Only do this for OTHER players that we have no recolection of Last Packets sent
                //    !LastPacketSent.ContainsKey(accountId)
                //    && packetTime > DateTime.Now.AddSeconds(-1).Ticks
                //    )
                //{
                //    if (newPos != Vector3.zero && Vector3.Distance(player.Position, newPos) > 10.0f)
                //    {
                //        player.Transform.position = newPos;
                //    }
                //}
                //// If the guy is nowhere near where he should be, move them!
                //else 
                //if (Vector3.Distance(player.Position, newPos) > 7.0f)
                //{
                //    player.Transform.position = newPos;
                //}
            }


            if (!LastPacketReceived.ContainsKey(accountId))
                LastPacketReceived.Add(accountId, packetTime);

            LastPacketReceived[accountId] = packetTime; 

        }


    }
}
