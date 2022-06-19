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

namespace SIT.Coop.Core.LocalGame
{
    internal class LocalGamePlayerSpawn : ModulePatch
    {
        private static GameWorld gameWorld = null;
        private static CoopGameComponent coopGameComponent = null;

        protected override MethodBase GetTargetMethod()
        {
            //foreach(var ty in SIT.Tarkov.Core.PatchConstants.EftTypes.Where(x => x.Name.StartsWith("BaseLocalGame")))
            //{
            //    Logger.LogInfo($"LocalGameStartingPatch:{ty}");
            //}
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

                if (coopGameComponent != null)
                    UnityEngine.Object.Destroy(coopGameComponent);

                if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                    return;

                coopGameComponent = Plugin.Instance.GetOrAddComponent<CoopGameComponent>();
                // TODO: Shouldnt this be a member variable, not static?
                CoopGameComponent.Players.TryAdd(PatchConstants.GetPlayerProfileAccountId(profile), p);


                Dictionary<string, object> dictionary2 = new Dictionary<string, object>
                    {
                        {
                            "accountId",
                            p.Profile.AccountId
                        },
                        {
                            "profileId",
                            p.ProfileId
                        },
                        {
                            "groupId",
                            Matchmaker.MatchmakerAcceptPatches.GetGroupId()
                        },
                        {
                            "nP",
                            position
                        },
                        {
                            "sP",
                            position
                        },
                        { "m", "PlayerSpawn" },
                        {
                            "p.info",
                            p.Profile.Info.SITToJson()
                        },
                        {
                            "p.cust",
                             p.Profile.Customization.SITToJson()
                        },
                        {
                            "p.equip",
                            p.Profile.Inventory.Equipment.CloneItem().ToJson()
                        }
                    };
                new Request().PostJson("/client/match/group/server/players/spawn", dictionary2.ToJson());
                //ServerCommunication.PostLocalPlayerData(p, dictionary2);

                if(Matchmaker.MatchmakerAcceptPatches.IsServer)
                {
                    Dictionary<string, object> value2 = new Dictionary<string, object>
                        {
                            {
                                "playersSpawnPoint",
                                position
                            },
                            {
                                "groupId",
                                p.Profile.AccountId
                            }
                        };
                    new Request().PostJson("/client/match/group/server/setPlayersSpawnPoint", JsonConvert.SerializeObject(value2));
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
                            string value4 = new Request().PostJson("/client/match/group/server/getPlayersSpawnPoint", JsonConvert.SerializeObject(value3));
                            if (!string.IsNullOrEmpty(value4))
                            {
                                System.Random r = new System.Random();
                                var randX = r.NextFloat(-1, 1);
                                var randZ = r.NextFloat(-1, 1);
                                Vector3 vector = JsonConvert.DeserializeObject<Vector3>(value4);
                                spawnPointPosition = vector;
                                PatchConstants.Logger.LogInfo($"Setup Client to use same Spawn at {spawnPointPosition.x}:{spawnPointPosition.y}:{spawnPointPosition.z} as Host");
                                spawnPointPosition = spawnPointPosition + new Vector3(randX, 0, randZ);
                                //LocalGame.SetMatchmakerStatus("Spawn Location from Server received");
                                //await Task.Delay(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            PatchConstants.Logger.LogInfo("Getting Client Spawn Point Failed::ERROR::" + ex.ToString());
                        }
                        //await Task.Delay(1000);
                    }
                    if(spawnPointPosition != Vector3.zero)
                    {
                        p.Teleport(spawnPointPosition, true);
                    }
                }
                    //gameWorld.GetType().DontDestroyOnLoad(coopGameComponent);
                //}
            });


            
        }

     
    }
}
