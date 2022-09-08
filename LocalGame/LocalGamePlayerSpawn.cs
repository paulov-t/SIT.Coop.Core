using Comfort.Common;
using EFT;
using Newtonsoft.Json;
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
using static SIT.Coop.Core.LocalGame.LocalGamePatches;
using SIT.Coop.Core.Player;
using System.Threading;

namespace SIT.Coop.Core.LocalGame
{
    internal class LocalGamePlayerSpawn : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName.StartsWith("EFT.LocalGame"));
            if (t == null)
                Logger.LogInfo($"LocalGamePlayerSpawn:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length >= 4
                && x.GetParameters()[0].Name.Contains("playerId")
                && x.GetParameters()[1].Name.Contains("position")
                && x.GetParameters()[2].Name.Contains("rotation")
                && x.GetParameters()[3].Name.Contains("layerName")
                );

            Logger.LogInfo($"LocalGamePlayerSpawn:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static async void PatchPrefix(
            object __instance
            , Task __result
            )
        {
            //Logger.LogInfo($"LocalGamePlayerSpawn:PatchPrefix");
        }

        [PatchPostfix]
        public static async void PatchPostfix(
            object __instance
            , Vector3 position
            , Task<EFT.LocalPlayer> __result
            )
        {
            //Logger.LogInfo($"LocalGamePlayerSpawn:PatchPostfix");

            await __result.ContinueWith((x) =>
            {
                var p = x.Result;
                
                Logger.LogInfo($"LocalGamePlayerSpawn:PatchPostfix:{p.GetType()}");

                var profile = PatchConstants.GetPlayerProfile(p);
                if (PatchConstants.GetPlayerProfileAccountId(profile) == PatchConstants.GetPHPSESSID())
                {
                    LocalGamePatches.MyPlayer = p;
                }

                if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                    return;

                //gameWorld = Singleton<GameWorld>.Instance;
                //coopGameComponent = gameWorld.GetOrAddComponent<CoopGameComponent>();
                if(Singleton<GameWorld>.Instance.TryGetComponent<CoopGameComponent>(out var coopGameComponent)) {

                    Logger.LogInfo("LocalGamePlayerSpawn CoopGameComponent Found!");

                }

                // Player spawns before Bots. This must occur here to clear out previous session.
                CoopGameComponent.Players.Clear();
                // TODO: Shouldnt this be a member variable, not static?
                CoopGameComponent.Players.TryAdd(PatchConstants.GetPlayerProfileAccountId(profile), p);
                var prc = p.GetOrAddComponent<PlayerReplicatedComponent>();
                prc.player = p;
                prc.IsMyPlayer = true;


                //Dictionary<string, object> dictionary2 = new Dictionary<string, object>
                //    {
                //        {
                //            "SERVER",
                //            "SERVER"
                //        },
                //        {
                //            "accountId",
                //            p.Profile.AccountId
                //        },
                //        {
                //            "profileId",
                //            p.ProfileId
                //        },
                //        {
                //            "groupId",
                //            Matchmaker.MatchmakerAcceptPatches.GetGroupId()
                //        },
                //        {
                //            "sPx",
                //            position.x
                //        },
                //        {
                //            "sPy",
                //            position.y
                //        },
                //        {
                //            "sPz",
                //            position.z
                //        },
                //        { "m", "PlayerSpawn" },
                //        {
                //            "p.info",
                //            JsonConvert.SerializeObject(p.Profile.Info)//.SITToJson()
                //        },
                //        {
                //            "p.cust",
                //             p.Profile.Customization.SITToJson()
                //        },
                //        {
                //            "p.equip",
                //            p.Profile.Inventory.Equipment.ToJson()
                //        }
                //    };
                ////new Request().PostJson("/client/match/group/server/players/spawn", dictionary2.ToJson());
                //ServerCommunication.PostLocalPlayerData(p, dictionary2);

                if (Matchmaker.MatchmakerAcceptPatches.IsServer)
                {
                    Dictionary<string, object> value2 = new Dictionary<string, object>
                    {
                        {
                            "m",
                            "SpawnPointForCoop"
                        },
                        {
                            "playersSpawnPointx",
                            position.x
                        },
                        {
                            "playersSpawnPointy",
                            position.y
                        },
                        {
                            "playersSpawnPointz",
                            position.z
                        },
                        {
                            "groupId",
                            p.Profile.AccountId
                        }
                    };
                    Logger.LogInfo("Setting Spawn Point to " + position);
                    new SIT.Tarkov.Core.Request().PostJson("/client/match/group/server/setPlayersSpawnPoint", JsonConvert.SerializeObject(value2));
                    ServerCommunication.SendDataDownWebSocket(value2);

                }
                else if (Matchmaker.MatchmakerAcceptPatches.IsClient)
                {
                    int attemptsToReceiveSpawn = 60;
                    var spawnPointPosition = Vector3.zero;
                    while (spawnPointPosition == Vector3.zero && attemptsToReceiveSpawn > 0)
                    {
                        attemptsToReceiveSpawn--;
                        //LocalGame.SetMatchmakerStatus($"Retreiving Spawn Location from Server {attemptsToReceiveSpawn}s");

                        try
                        {
                            Dictionary<string, object> value3 = new Dictionary<string, object> {
                                {
                                    "groupId",
                                    MatchmakerAcceptPatches.GetGroupId()
                                } };
                            string value4 = new SIT.Tarkov.Core.Request().PostJson("/client/match/group/server/getPlayersSpawnPoint", JsonConvert.SerializeObject(value3));
                            if (!string.IsNullOrEmpty(value4))
                            {
                                System.Random r = new System.Random();
                                var randX = r.NextFloat(-1, 1);
                                var randZ = r.NextFloat(-1, 1);

                                var spawnPointDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(value4);
                                var spawnPointX = float.Parse(spawnPointDict["x"].ToString());
                                var spawnPointY = float.Parse(spawnPointDict["y"].ToString());
                                var spawnPointZ = float.Parse(spawnPointDict["z"].ToString());
                                Vector3 vector = new Vector3(spawnPointX, spawnPointY, spawnPointZ);
                                spawnPointPosition = vector;
                                PatchConstants.Logger.LogInfo($"Setup Client to use same Spawn at {spawnPointPosition.x}:{spawnPointPosition.y}:{spawnPointPosition.z} as Host");
                                spawnPointPosition = spawnPointPosition + new Vector3(randX, 0, randZ);
                            }
                            else
                            {
                                PatchConstants.Logger.LogInfo("Getting Client Spawn Point Failed::ERROR::No Value Given");
                            }
                        }
                        catch (Exception ex)
                        {
                            PatchConstants.Logger.LogInfo("Getting Client Spawn Point Failed::ERROR::" + ex.ToString());
                        }
                        //await Task.Delay(1000);
                        Thread.Sleep(500);
                    }
                    if(spawnPointPosition != Vector3.zero)
                    {
                        //p.Teleport(spawnPointPosition, true);
                    }
                }
                    //gameWorld.GetType().DontDestroyOnLoad(coopGameComponent);
                //}
            });


            
        }

     
    }
}
