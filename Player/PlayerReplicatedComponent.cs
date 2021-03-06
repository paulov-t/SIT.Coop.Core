using EFT.Interactive;
using SIT.Coop.Core.Player.Weapon;
using SIT.Coop.Core.Web;
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
        public float LastTiltLevel { get; private set; }
        public Vector2 LastRotation { get; private set; } = Vector2.zero;
        public Vector2 LastMovementDirection { get; private set; } = Vector2.zero;

        void Awake()
        {
            //PatchConstants.Logger.LogInfo("PlayerReplicatedComponent:Awake");
        }

        void Start()
        {
            //PatchConstants.Logger.LogInfo("PlayerReplicatedComponent:Start");

            if (this.listOfInteractiveObjects == null)
            {
                this.listOfInteractiveObjects = FindObjectsOfType<WorldInteractiveObject>();
            }
        }


        private WorldInteractiveObject[] listOfInteractiveObjects;


        void FixedUpdate()
        {

        }

        bool handlingPackets = false;

        void Update()
        {
            if (player == null)
                return;

            if (this.listOfInteractiveObjects == null)
            {
                this.listOfInteractiveObjects = FindObjectsOfType<WorldInteractiveObject>();
            }

            UpdateMovement();

            if (!handlingPackets && player != null && QueuedPackets.Any())
            {
                handlingPackets = true;

                //PatchConstants.Logger.LogInfo($"QueuedPackets.Length:{QueuedPackets.Count}");

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
                            //PatchConstants.Logger.LogInfo("Damage");
                            PlayerOnDamagePatch.DamageReplicated(player, packet);
                            break;
                        case "Dead":
                            PatchConstants.Logger.LogInfo("Dead");
                            break;
                        case "Door":
                            PatchConstants.Logger.LogInfo("Door");
                            break;
                        case "Move":
                            if(LastMovementPacket == null 
                                || int.Parse(packet["seq"].ToString()) > int.Parse(LastMovementPacket["seq"].ToString())
                                )
                                LastMovementPacket = packet;
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
                            PlayerOnSayPatch.SayReplicated(player, packet);
                            break;
                        case "SetTriggerPressed":
                            //PatchConstants.Logger.LogInfo("SetTriggerPressed");
                            WeaponOnTriggerPressedPatch.WeaponOnTriggerPressedReplicated(player, packet);
                            break;
                        case "SetItemsInHands":
                            PatchConstants.Logger.LogInfo("SetItemsInHands");
                            break;
                        case "InventoryOpened":
                            PlayerOnInventoryOpenedPatch.InventoryOpenedReplicated(player, packet);
                            break;
                        case "Tilt":
                            PlayerOnTiltPatch.TiltReplicated(player, packet);
                            break;

                    }
                }
                handlingPackets = false;
            }
        }

        public void DequeueAllMovementPackets()
        {
            if (QueuedPackets.Any())
            {
                handlingPackets = true;

                //PatchConstants.Logger.LogInfo($"QueuedPackets.Length:{QueuedPackets.Count}");

                if (QueuedPackets.TryDequeue(out Dictionary<string, object> packet))
                {
                    var method = packet["m"].ToString();
                    if(method != "Move")
                    {
                        QueuedPackets.Enqueue(packet);
                    }
                }
            }
        }

        private void UpdateMovement()
        {
            if (player == null)
                return;


            if (player.MovementContext != null)
            {
                if (this.LastTiltLevel != player.MovementContext.Tilt)
                {
                    this.LastTiltLevel = player.MovementContext.Tilt;
                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary.Add("tilt", LastTiltLevel);
                    dictionary.Add("m", "Tilt");
                    ServerCommunication.PostLocalPlayerData(player, dictionary);
                }


                //var rotationDist = Vector2.Distance(player.Rotation, LastRotation);
                //var rotationDot = Vector2.Dot(player.Rotation, LastRotation);
                //var rotationAngle = Vector2.Angle(player.Rotation, LastRotation);

                //if (player.IsAI && player.Rotation != this.LastRotation && rotationAngle > 0.5)
                //{
                //    this.LastRotation = player.Rotation;
                //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                //    dictionary.Add("rX", LastRotation.x);
                //    dictionary.Add("rY", LastRotation.y);
                //    dictionary.Add("m", "Rotation");
                //    ServerCommunication.PostLocalPlayerData(player, dictionary);
                //}

                //var movementAngle = Vector2.Angle(player.MovementContext.MovementDirection, this.LastMovementDirection);

                //if (this.LastMovementDirection == Vector2.zero 
                //    || (player.MovementContext.MovementDirection != this.LastMovementDirection
                //        && movementAngle >= 45)
                //    )
                //{
                //    this.LastMovementDirection = player.MovementContext.MovementDirection;
                //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                //    dictionary.Add("dX", LastMovementDirection.x);
                //    dictionary.Add("dY", LastMovementDirection.y);
                //    dictionary.Add("m", "MoveDirection");
                //    ServerCommunication.PostLocalPlayerData(player, dictionary);
                //}
            }


            if (LastMovementPacket == null)
                return;

            //if (LastRotationPacket == null)
            //    return;

            //if (LastMovementPacket.ContainsKey("t") && long.Parse(LastMovementPacket["t"].ToString()) < DateTime.Now.AddSeconds(-10).Ticks)
            //{
            //    LastMovementPacket = null;
            //    return;
            //}

            PlayerOnMovePatch.MoveReplicated(player, LastMovementPacket);


            //if(ReceivedRotationPackets.Any() && ReceivedRotationPackets.TryDequeue(out var r)) 
            //    PlayerOnRotatePatch.RotateReplicatedV(player, r);

        }
    }
}
