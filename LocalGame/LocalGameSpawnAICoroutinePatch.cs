using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SIT.Coop.Core.Player;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using SIT.Tarkov.Core.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.LocalGame
{
    internal class LocalGameSpawnAICoroutinePatch : ModulePatch
    {
        /*
        protected virtual IEnumerator vmethod_3(float startDelay, GStruct252 controllerSettings, GInterface250 spawnSystem, Callback runCallback)
        */
        public static object BossSpawner;
        public static object BossSpawnerWaves;
        private static ConfigFile _config;
        //private static MethodInfo MethodInfoBotCreation;
        private static int maxCountOfBots = 20;
        private static LocationSettings.SelectedLocation LocationSettings;


        public LocalGameSpawnAICoroutinePatch(ConfigFile config, BaseLocalGame<GamePlayerOwner> game)
        {
            _config = config;

            var gameType = game.GetType().BaseType;
            Logger.LogInfo($"LocalGameSpawnAICoroutinePatch:gameType:{gameType.Name}");
            var gameInstance = game;
            Logger.LogInfo($"LocalGameSpawnAICoroutinePatch:game:{gameInstance}");

            Logger.LogInfo("LocalGameSpawnAICoroutinePatch:Get Boss Spawner");
            BossSpawner = PatchConstants.GetFieldFromTypeByFieldType(
                    gameType,
                    typeof(BossSpawnerClass)).GetValue(game);

            Logger.LogInfo("LocalGameSpawnAICoroutinePatch:Get Location Settings");

            LocationSettings = (LocationSettings.SelectedLocation)PatchConstants.GetFieldFromTypeByFieldType(
                    gameType,
                    typeof(LocationSettings.SelectedLocation)).GetValue(LocalGamePatches.LocalGameInstance);

        }

        protected override MethodBase GetTargetMethod()
        {
            //BossSpawner = PatchConstants.GetFieldFromType(LocalGamePatches.LocalGameInstance.GetType().BaseType
            //    , BotSystemHelpers.BossSpawnRunnerType.Name.ToLower() + "_0").GetValue(LocalGamePatches.LocalGameInstance);
            //BossSpawner = PatchConstants.GetFieldFromTypeByFieldType(
            //        LocalGamePatches.LocalGameInstance.GetType(),
            //        typeof(BossSpawnerClass)).GetValue(LocalGamePatches.LocalGameInstance);

            //Logger.LogInfo("LocalGameSpawnAICoroutinePatch:Get Location Settings");

            //LocationSettings = (LocationSettings.SelectedLocation)PatchConstants.GetFieldFromTypeByFieldType(
            //        LocalGamePatches.LocalGameInstance.GetType(),
            //        typeof(LocationSettings.SelectedLocation)).GetValue(LocalGamePatches.LocalGameInstance); 


            //PatchConstants.GetFieldOrPropertyFromInstance<object>(LocalGamePatches.LocalGameInstance, BotSystemHelpers.BossSpawnRunnerType.Name.ToLower() + "_0", false);
            //Logger.LogInfo($"BossSpawner:{BossSpawner.GetType().Name}");

            BossSpawnerWaves = PatchConstants.GetFieldOrPropertyFromInstance<object>(BossSpawner, "BossSpawnWaves", false);
            //Logger.LogInfo($"BossSpawnerWaves:{BossSpawnerWaves.GetType().Name}");

            //MethodInfoBotCreation = PatchConstants.GetMethodForType(LocalGamePatches.LocalGameInstance.GetType().BaseType, "method_8");

            var targetMethod = PatchConstants.GetAllMethodsForType(LocalGamePatches.LocalGameInstance.GetType().BaseType)
                .Single(
                m =>
                m.IsVirtual
                && m.GetParameters().Length >= 4
                && m.GetParameters()[0].ParameterType == typeof(float)
                && m.GetParameters()[0].Name == "startDelay"
                && m.GetParameters()[1].Name == "controllerSettings"
                );

            //Logger.LogInfo($"LocalGameSpawnAICoroutinePatch.TargetMethod:{targetMethod.Name}");
            return targetMethod;
        }

        //[PatchPrefix]
        //public static bool PatchPrefix(
        //float startDelay, object controllerSettings, object spawnSystem, object runCallback,
        //object ___wavesSpawnScenario_0, ref IEnumerator __result
        //)
        //{
        //    Logger.LogInfo("LocalGameSpawnAICoroutinePatch.PatchPrefix");

        //    return true;
        //}

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            return false;
        }

        [PatchPostfix]
        public static IEnumerator PatchPostfix(
            IEnumerator __result,
            BaseLocalGame<GamePlayerOwner> __instance,
            ISpawnSystem spawnSystem,
            Callback runCallback,
            WavesSpawnScenario ___wavesSpawnScenario_0,
            NonWavesSpawnScenario ___nonWavesSpawnScenario_0
        )
        {
            Logger.LogInfo($"LocalGameSpawnAICoroutinePatch:PatchPostfix");

            // Run normally.
            if (!Matchmaker.MatchmakerAcceptPatches.IsClient)
            {

                // Wait for client to join 
                // comment out to test general stuff
                var waitForClients = _config.Bind("Server", "Wait for Clients", true).Value;
                if (waitForClients && Matchmaker.MatchmakerAcceptPatches.IsServer)
                {
                    if (CoopGameComponent.Players.Any())
                    {
                        var filteredPlayersOnlyCount = CoopGameComponent.Players.Count(x => !x.Value.IsAI);
                        while (filteredPlayersOnlyCount < Matchmaker.MatchmakerAcceptPatches.HostExpectedNumberOfPlayers)
                        {
                            filteredPlayersOnlyCount = CoopGameComponent.Players.Count(x => !x.Value.IsAI);
                            yield return new WaitForSeconds(1);
                        }
                    }
                }


                //Logger.LogInfo($"BotSystemHelpers.RoleLimitDifficultyType.Name_0:{BotSystemHelpers.RoleLimitDifficultyType.Name + "_0"}");
                //CoopGameComponent.Players.Clear();
                var nonSpawnWaveShit = PatchConstants.GetFieldOrPropertyFromInstance<object>(
                    LocalGamePatches.LocalGameInstance
                    //, BotSystemHelpers.RoleLimitDifficultyType.Name + "_0"
                    , "nonWavesSpawnScenario_0"
                    , false);
                //if (nonSpawnWaveShit != null)
                {
                    Logger.LogInfo($"nonSpawnWaveShit:{nonSpawnWaveShit.GetType().Name}");

                    // this doesn't work because the gclass name isn't the same as the member name, need a work around
                    //var openZones = PatchConstants.GetFieldOrPropertyFromInstance<object>(LocalGamePatches.LocalGameInstance, BotSystemHelpers.LocationBaseType.Name + "_0", false);
                    var openZones = LocationSettings;// PatchConstants.GetFieldOrPropertyFromInstance<object>(__instance, "GClass1113_0", false);
                    var openZones2 = PatchConstants.GetFieldOrPropertyFromInstance<string>(openZones, "OpenZones", false);

                    //Logger.LogInfo($"BotSystemHelpers.ProfileCreatorType:{BotSystemHelpers.ProfileCreatorType.Name}");
                    // Construct Profile Creator
                    var profileCreator = new BotPresetFactory1(PatchConstants.BackEndSession, ___wavesSpawnScenario_0.SpawnWaves, (BossLocationSpawn[])BossSpawnerWaves, null, false);

                    BotCreator1 botCreator = new BotCreator1(__instance, profileCreator, BotCreationMethod);

                    BotZone[] botZones = LocationScene.GetAllObjects<BotZone>().ToArray();
                    bool enableWaveControl = true;

                    __instance.BotsController.Init(
                        __instance
                        , botCreator
                        , botZones
                        , spawnSystem
                        , ___wavesSpawnScenario_0.BotLocationModifier
                        , true
                        , false
                        , enableWaveControl
                        , false
                        , false
                        , Singleton<GameWorld>.Instance
                        , openZones2);
                    
                    var AICountOverride = _config.Bind("Server", "Override Number of AI", false).Value;
                    var NumberOfAI = _config.Bind("Server", "Number of AI", 15).Value;
                    if (AICountOverride)
                    {
                        maxCountOfBots = NumberOfAI;
                    }
                    var backendConfig = PatchConstants.BackEndSession.BackEndConfig;
                    var botPresets = backendConfig.BotPresets;
                    var botWeaponScatterings = backendConfig.BotWeaponScatterings;

                    Logger.LogInfo($"Max Number of Bots:{maxCountOfBots}");
                    __instance.BotsController.SetSettings(maxCountOfBots, botPresets, botWeaponScatterings);
                    //BotSystemHelpers.SetSettings(maxCountOfBots, botPresets, botWeaponScatterings);
                    var AIIgnorePlayers = _config.Bind("Server", "AI Ignore Players", false).Value;
                    if (!AIIgnorePlayers)
                    {
                        //var gparam = PatchConstants.GetFieldOrPropertyFromInstance<object>(LocalGamePatches.LocalGameInstance, "gparam_0", false);
                        //var player = PatchConstants.GetFieldOrPropertyFromInstance<EFT.Player>(gparam, "Player", false);
                        //BotSystemHelpers.AddActivePlayer(player);
                        __instance.BotsController.AddActivePLayer(__instance.AllPlayers[0]);
                    }
                    yield return new WaitForSeconds(1);

                    var EnableAISpawnWaveSystem = _config.Bind("Server", "Enable AI Spawn Wave System", true
                        , new ConfigDescription("Whether to run the Wave Spawner System. Useful for testing.")).Value;
                    if (EnableAISpawnWaveSystem)
                    {
                        if (___wavesSpawnScenario_0.SpawnWaves != null && ___wavesSpawnScenario_0.SpawnWaves.Length != 0)
                        {
                            ___wavesSpawnScenario_0.Run();
                        }
                        else
                        {
                            ___nonWavesSpawnScenario_0.Run();
                        }

                        PatchConstants.GetMethodForType(BossSpawner.GetType(), "Run").Invoke(BossSpawner, new object[] { EBotsSpawnMode.Anyway });
                    }
                    yield return new WaitForSeconds(3);

                    using (PatchConstants.StartWithToken("SessionRun"))
                    {
                        // TODO: This needs removing!
                        PatchConstants.GetMethodForType(LocalGamePatches.LocalGameInstance.GetType(), "vmethod_4").Invoke(LocalGamePatches.LocalGameInstance, new object[0]);
                    }
                    yield return new WaitForSeconds(1);

                   
                }

                runCallback.Succeed();
            }
            else 
            {
                BotSystemHelpers.SetSettingsNoBots();
                try
                {
                    BotSystemHelpers.Stop();
                }
                catch
                {

                }
                yield return new WaitForSeconds(1);
                using (PatchConstants.StartWithToken("SessionRun"))
                {
                    // TODO: This needs removing!
                    PatchConstants.GetMethodForType(LocalGamePatches.LocalGameInstance.GetType(), "vmethod_4").Invoke(LocalGamePatches.LocalGameInstance, new object[0]);
                }
                runCallback.Succeed();
            }
        }


        private static MethodInfo method_8 = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static async Task<LocalPlayer> BotCreationMethod(Profile profile, Vector3 position)
        {
            // If its Client. Out!
            if (Matchmaker.MatchmakerAcceptPatches.IsClient)
                return null;

            // If there is no profile. Out!
            if (profile == null)
                return null;

            if (CoopGameComponent.Players.Count >= maxCountOfBots) 
            {
                //Logger.LogInfo($"BotCreationMethod. [ERROR] CoopGameComponent.Players is full");
                return null;
            }

            if (CoopGameComponent.Players.ContainsKey(profile.AccountId))
            {
                //Logger.LogInfo($"BotCreationMethod. [ERROR] Bot already exists");
                return null;
            }

            // TODO: Rewrite the following method into BotCreationMethod
            if (method_8 == null)
                method_8 = PatchConstants.GetMethodForType(LocalGamePatches.LocalGameInstance.GetType().BaseType, "method_8");

            var player = await (Task<LocalPlayer>)method_8
            .Invoke(LocalGamePatches.LocalGameInstance, new object[] { profile, position });

            if (Matchmaker.MatchmakerAcceptPatches.IsServer)
            {
                var prc = player.GetOrAddComponent<PlayerReplicatedComponent>();
                prc.player = player;
                CoopGameComponent.Players.TryAdd(profile.AccountId, player);
                Dictionary<string, object> dictionary2 = new Dictionary<string, object>
                    {
                        {
                            "SERVER",
                            "SERVER"
                        },
                        {
                            "isAI",
                            true
                        },
                        {
                            "accountId",
                            player.Profile.AccountId
                        },
                        {
                            "profileId",
                            player.ProfileId
                        },
                        {
                            "groupId",
                            Matchmaker.MatchmakerAcceptPatches.GetGroupId()
                        },
                        {
                            "sPx",
                            position.x
                        },
                        {
                            "sPy",
                            position.y
                        },
                        {
                            "sPz",
                            position.z
                        },
                        { "m", "PlayerSpawn" },
                        {
                            "p.info",
                            JsonConvert.SerializeObject(player.Profile.Info
                                , Formatting.None 
                                , new JsonSerializerSettings() { })//.SITToJson()
                        },
                        {
                            "p.cust",
                             player.Profile.Customization.SITToJson()
                        },
                        {
                            "p.equip",
                            //player.Profile.Inventory.Equipment.CloneItem().ToJson()
                            player.Profile.Inventory.Equipment.SITToJson()
                        }
                    };
                //new Request().PostJson("/client/match/group/server/players/spawn", dictionary2.ToJson());
                await ServerCommunication.PostLocalPlayerDataAsync(player, dictionary2);
                //Logger.LogInfo($"BotCreationMethod. [SUCCESS] Adding AI {profile.AccountId} to CoopGameComponent.Players list");
            }
            return player;
        }
    }
}
