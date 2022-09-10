#pragma warning disable CS0618 // Type or member is obsolete
using EFT.Interactive;
using SIT.Coop.Core.LocalGame;
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
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace SIT.Coop.Core.Player
{
    internal class PlayerReplicatedComponent : NetworkBehaviour
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
        public Quaternion? LastRotation { get; private set; }
        public Vector2 LastMovementDirection { get; private set; } = Vector2.zero;
        public bool IsMyPlayer { get; internal set; }

        private NetworkConnection _connection { get; } = new NetworkConnection();
        private System.Random RandomConnectionIds { get; } = new System.Random();

        private NetworkClient _client { get; set; }

        void Awake()
        {
            //PatchConstants.Logger.LogInfo("PlayerReplicatedComponent:Awake");
        }

        void Start()
        {
            //PatchConstants.Logger.LogInfo("PlayerReplicatedComponent:Start");

            //_connection.Initialize("0.0.0.0", 1, 1, new HostTopology());

            //_connection.Initialize("127.0.0.1", 1, RandomConnectionIds.Next(63333), new HostTopology(CoopGameComponent.GetConnectionConfig(), 99));

            //_client = NetworkManager.singleton.StartClient(CoopGameComponent.GetMatchInfo());
            //_client.RegisterHandler(999, (NetworkMessage message) => {

            //    PatchConstants.Logger.LogInfo("Client Received 999 Message");

            //});
            ////_client.Connect("127.0.0.1", 5000);
            //_client.Connect(CoopGameComponent.GetMatchInfo());
            //if (_client.isConnected)
            //{
            //    _client.Send(999, null);
            //}
            //else
            //{
            //    //PatchConstants.Logger.LogInfo("NetworkClient is not connected!!");
            //}

            if (this.listOfInteractiveObjects == null)
            {
                this.listOfInteractiveObjects = FindObjectsOfType<WorldInteractiveObject>();
            }
        }


        private WorldInteractiveObject[] listOfInteractiveObjects;

        private Vector3 ReceivedPacketPostion = Vector3.zero;
        private Vector2 ReceivedPacketRotation = Vector2.zero;


        void FixedUpdate()
        {

        }

        bool handlingPackets = false;

        void Update()
        {
            if (player == null)
                return;

            //if (this.listOfInteractiveObjects == null)
            //{
            //    this.listOfInteractiveObjects = FindObjectsOfType<WorldInteractiveObject>();
            //}

            UpdateMovement();

            if (player != null && QueuedPackets.Any())
            //if (!handlingPackets && player != null && QueuedPackets.Any())
            {
                handlingPackets = true;

                //PatchConstants.Logger.LogInfo($"QueuedPackets.Length:{QueuedPackets.Count}");

                if (QueuedPackets.TryDequeue(out Dictionary<string, object> packet))
                {
                    var method = packet["m"].ToString();
                    if (packet["accountId"].ToString() != player.Profile.AccountId)
                        return;

                    // Any packets are ancient and lossless, then remove
                    if (packet.ContainsKey("t") && long.Parse(packet["t"].ToString()) < DateTime.Now.AddSeconds(-PacketTimeoutInSeconds).Ticks)
                    {
                        QueuedPackets.TryDequeue(out _);
                        return;
                    }

                    switch (method)
                    {

                        case "ApplyCorpseImpulse":
                            PlayerOnApplyCorpseImpulsePatch.Replicated(player, packet);
                            break;
                        case "Damage":
                            PlayerOnDamagePatch.DamageReplicated(player, packet);
                            break;
                        case "Dead":
                            PatchConstants.Logger.LogInfo("Dead");
                            break;
                        case "Door":
                            PatchConstants.Logger.LogInfo("Door");
                            break;
                        case "DropBackpack":
                            PatchConstants.Logger.LogInfo("Door");
                            break;
                        case "EnableSprint":
                            PlayerOnEnableSprintPatch.Replicated(player, packet);
                            break;
                        case "Gesture":
                            PlayerOnGesturePatch.Replicated(player, packet);
                            break;
                        case "HostDied":
                            PatchConstants.Logger.LogInfo("Host Died");
                            LocalGameEndingPatch.EndSession(LocalGamePatches.LocalGameInstance, LocalGamePatches.MyPlayerProfile.Id, EFT.ExitStatus.Survived, "", 0);
                            break;
                        case "Jump":
                            PlayerOnJumpPatch.Replicated(player, packet);
                            break;
                        case "Move":
                            if(LastMovementPacket == null 
                                || int.Parse(packet["seq"].ToString()) > int.Parse(LastMovementPacket["seq"].ToString())
                                )
                                LastMovementPacket = packet;
                            break;
                        case "Position":
                            if (!IsMyPlayer)
                            {
                                Vector3 newPos = Vector3.zero;
                                newPos.x = float.Parse(packet["x"].ToString());
                                newPos.y = float.Parse(packet["y"].ToString());
                                newPos.z = float.Parse(packet["z"].ToString());
                                ReceivedPacketPostion = newPos;
                            }
                            break;
                        case "Rotation":
                            if(!IsMyPlayer)
                            {
                                var rotationX = float.Parse(packet["rX"].ToString());
                                var rotationY = float.Parse(packet["rY"].ToString());
                                ReceivedPacketRotation = new Vector2(rotationX, rotationY);
                            }
                            break;
                        case "Say":
                            PlayerOnSayPatch.SayReplicated(player, packet);
                            break;
                        case "SetTriggerPressed":
                            //PatchConstants.Logger.LogInfo("SetTriggerPressed");
                            WeaponOnTriggerPressedPatch.Replicated(player, packet);
                            break;
                        case "SetItemInHands":
                            PlayerOnSetItemInHandsPatch.SetItemInHandsReplicated(player, packet);
                            break;
                        case "InventoryOpened":
                            PlayerOnInventoryOpenedPatch.Replicated(player, packet);
                            break;
                        case "Tilt":
                            //PlayerOnTiltPatch.TiltReplicated(player, packet);
                            break;
                        case "Proceed":
                            switch(packet["pType"].ToString())
                            {
                                case "Weapon":
                                    //PlayerOnProceedWeaponPatch.ProceedWeaponReplicated(player, packet);
                                    break;
                                case "Knife":
                                    PlayerOnProceedKnifePatch.ProceedWeaponReplicated(player, packet);
                                    break;
                                case "Meds":
                                    break;
                                case "Food":
                                    break;
                            }
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

        public static bool ShouldReplicate(EFT.Player player, bool isMyPlayer)
        {
            return (Matchmaker.MatchmakerAcceptPatches.IsClient && isMyPlayer)
               || (Matchmaker.MatchmakerAcceptPatches.IsServer && player.IsAI)
               || (Matchmaker.MatchmakerAcceptPatches.IsServer && isMyPlayer);
        }

        private Vector3? LastSentPosition;

        private void UpdateMovement()
        {
            if (player == null)
                return;


            if (ShouldReplicate(player, IsMyPlayer))
            {
                if (player.MovementContext != null)
                {
                    if (this.LastTiltLevel != player.MovementContext.Tilt 
                        && (this.LastTiltLevel - player.MovementContext.Tilt > 0.1f || this.LastTiltLevel - player.MovementContext.Tilt < -0.1f)
                        )
                    {
                        this.LastTiltLevel = player.MovementContext.Tilt;
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        dictionary.Add("tilt", LastTiltLevel);
                        dictionary.Add("m", "Tilt");
                        ServerCommunication.PostLocalPlayerData(player, dictionary);
                    }

                    if (!LastRotation.HasValue)
                        LastRotation = player.MovementContext.TransformRotation;

                    var rotationAngle = Quaternion.Angle(player.MovementContext.TransformRotation, LastRotation.Value);

                    if (player.MovementContext.TransformRotation != this.LastRotation && rotationAngle > 15)
                    {
                        this.LastRotation = player.MovementContext.TransformRotation;
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        dictionary.Add("rX", player.MovementContext.Rotation.x);
                        dictionary.Add("rY", player.MovementContext.Rotation.y);
                        dictionary.Add("m", "Rotation");
                        ServerCommunication.PostLocalPlayerData(player, dictionary);
                    }

                    if(!LastSentPosition.HasValue || Vector3.Distance(LastSentPosition.Value, player.Position) > 0.9f)
                    {
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        dictionary.Add("x", player.Position.x);
                        dictionary.Add("y", player.Position.y);
                        dictionary.Add("z", player.Position.z);
                        dictionary.Add("m", "Position");
                        ServerCommunication.PostLocalPlayerData(player, dictionary);
                        LastSentPosition = player.Position;
                    }

                }
            }

            if (!IsMyPlayer)
            {
                if (Vector3.Distance(player.Transform.position, ReceivedPacketPostion) < 2f)
                    player.Transform.position = Vector3.Lerp(player.Transform.position, ReceivedPacketPostion, 2f * Time.deltaTime);
                else
                    player.Transform.position = ReceivedPacketPostion;

                //player.MovementContext.MovementDirection

                player.MovementContext.Rotation = Vector2.Lerp(player.MovementContext.Rotation, ReceivedPacketRotation, 2f * Time.deltaTime);
            }

            if (LastMovementPacket == null)
                return;


            PlayerOnMovePatch.MoveReplicated(player, LastMovementPacket);


            //if(ReceivedRotationPackets.Any() && ReceivedRotationPackets.TryDequeue(out var r)) 
            //    PlayerOnRotatePatch.RotateReplicatedV(player, r);

        }
    }
}
