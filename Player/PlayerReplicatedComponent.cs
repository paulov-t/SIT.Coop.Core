using SIT.Tarkov.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.Player
{
    internal class PlayerReplicatedComponent : MonoBehaviour
    {
        internal const int PacketTimeoutInSeconds = 1;
        internal ConcurrentQueue<Dictionary<string, object>> QueuedPackets { get; }
            = new ConcurrentQueue<Dictionary<string, object>>();

        internal Dictionary<string, object> LastMovementPacket { get; set; }
        internal Dictionary<string, object> LastRotationPacket { get; set; }
        internal DateTime? LastRotationPacketPostTime { get; set; }
        internal EFT.LocalPlayer player { private get; set; }

        internal List<Vector2> ClientListRotationsToSend { get; } = new List<Vector2>();
        internal ConcurrentQueue<Vector2> ReceivedRotationPackets { get; } = new ConcurrentQueue<Vector2>();


        void Awake()
        {
            PatchConstants.Logger.LogInfo("PlayerReplicatedComponent:Awake");
        }

        void Start()
        {
            PatchConstants.Logger.LogInfo("PlayerReplicatedComponent:Start");
        }



        

        void FixedUpdate()
        {

        }

        bool handlingPackets = false;

        void Update()
        {
            if (player == null)
                return;

            UpdateMovement();

            if (!handlingPackets && player != null)
            {
                handlingPackets = true;

                if (QueuedPackets.TryDequeue(out Dictionary<string, object> packet))
                {
                    var method = packet["m"].ToString();

                    // Any packets are ancient and lossless, then remove
                    if (packet.ContainsKey("t") && long.Parse(packet["t"].ToString()) < DateTime.Now.AddSeconds(-PacketTimeoutInSeconds).Ticks)
                    {
                        QueuedPackets.TryDequeue(out _);
                        return;
                    }

                    switch (method)
                    {
                        case "Damage":
                            PatchConstants.Logger.LogInfo("Damage");
                            PlayerOnDamagePatch.DamageReplicated(player, packet);
                            break;
                        case "Dead":
                            PatchConstants.Logger.LogInfo("Dead");
                            break;
                        case "Door":
                            PatchConstants.Logger.LogInfo("Door");
                            break;
                        case "Move":
                            LastMovementPacket = packet;

                            //PlayerOnMovePatch.MoveReplicated(player, packet);
                            break;
                        case "RotateBatch":
                            var rotationBatch = Json.Deserialize<List<Vector2>>(Json.Serialize(packet["batch"]));
                            foreach (var r in rotationBatch) 
                            {
                                if (r.IsZero() || r.SqrMagnitude() < 0.1f)
                                    continue;

                                ReceivedRotationPackets.Enqueue(r);
                            }
                            break;
                        //case "Rotate":
                        //    //PatchConstants.Logger.LogInfo("Rotate");
                        //    PlayerOnRotatePatch.RotateReplicated(player, packet);
                        //    //LastRotationPacket = packet;
                        //    break;
                        case "Say":
                            break;
                        case "SetTriggerPressed":
                            PatchConstants.Logger.LogInfo("SetTriggerPressed");
                            break;
                        case "SetItemsInHands":
                            PatchConstants.Logger.LogInfo("SetItemsInHands");
                            break;
                        case "InventoryOpened":
                            PlayerOnInventoryOpenedPatch.InventoryOpenedReplicated(player, packet);
                            break;

                    }
                }
                handlingPackets = false;
            }
        }

        private void UpdateMovement()
        {
            if (player == null)
                return;

            if (LastMovementPacket == null)
                return;

            //if (LastRotationPacket == null)
            //    return;

            if (LastMovementPacket.ContainsKey("t") && long.Parse(LastMovementPacket["t"].ToString()) < DateTime.Now.AddSeconds(-10).Ticks)
            {
                LastMovementPacket = null;
                return;
            }

            PlayerOnMovePatch.MoveReplicated(player, LastMovementPacket);


            //if(ReceivedRotationPackets.Any() && ReceivedRotationPackets.TryDequeue(out var r)) 
            //    PlayerOnRotatePatch.RotateReplicatedV(player, r);

        }
    }
}
