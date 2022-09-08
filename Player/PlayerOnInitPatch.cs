using BepInEx.Configuration;
using Newtonsoft.Json;
using SIT.Coop.Core.LocalGame;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnInitPatch : ModulePatch
    {
        private static ConfigFile _config;
        public PlayerOnInitPatch(ConfigFile config)
        {
            _config = config;
        }

        protected override MethodBase GetTargetMethod()
        {
            return PatchConstants.GetMethodForType(typeof(EFT.LocalPlayer), "Init");
        }

        //[PatchPrefix]
        //public static
        //  bool
        //  PatchPrefix(EFT.LocalPlayer __instance)
        //{
        //    var EnableAISpawnWaveSystem = _config.Bind("Server", "Enable AI Spawn Wave System", true
        //                           , new ConfigDescription("Whether to run the Wave Spawner System. Useful for testing.")).Value;

        //    var result = !__instance.IsAI || (!Matchmaker.MatchmakerAcceptPatches.IsClient && EnableAISpawnWaveSystem);
        //    return result;
        //}

        [PatchPostfix]
        public static
            async
            void
            PatchPostfix(EFT.LocalPlayer __instance)
        {
            var player = __instance;

                CoopGameComponent.Players.TryAdd(player.Profile.AccountId, player);
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
                            player.Transform.position.x
                        },
                        {
                            "sPy",
                            player.Transform.position.y
                        },
                        {
                            "sPz",
                            player.Transform.position.z
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

                var prc = player.GetOrAddComponent<PlayerReplicatedComponent>();
                prc.player = player;
                await ServerCommunication.PostLocalPlayerDataAsync(player, dictionary2);
                Logger.LogInfo($"PlayerOnInitPatch. Sent to Server!");

            if(Matchmaker.MatchmakerAcceptPatches.IsServer)
                PatchConstants.DisplayMessageNotification($"{__instance.Profile.Nickname}:{__instance.Side}:{__instance.Profile.Info.Settings.Role} has spawned");

                //Logger.LogInfo($"BotCreationMethod. [SUCCESS] Adding AI {profile.AccountId} to CoopGameComponent.Players list");
        }
    }
}
