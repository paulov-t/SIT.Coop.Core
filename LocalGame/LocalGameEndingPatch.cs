using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.EnvironmentEffect;
using EFT.UI;
using EFT.UI.Screens;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.LocalGame
{
    internal class LocalGameEndingPatch : ModulePatch
    {
        private static ConfigFile _config;


        public LocalGameEndingPatch(ConfigFile config)
        {
            _config = config;
        }

        protected override MethodBase GetTargetMethod()
        {
            //foreach(var ty in SIT.Tarkov.Core.PatchConstants.EftTypes.Where(x => x.Name.StartsWith("BaseLocalGame")))
            //{
            //    Logger.LogInfo($"LocalGameStartingPatch:{ty}");
            //}
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName.StartsWith("EFT.LocalGame"));
            if (t == null)
                Logger.LogInfo($"LocalGameEndingPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length >= 4
                && x.GetParameters().Any(x => x.Name.Contains("profileId"))
                && x.GetParameters().Any(x => x.Name.Contains("exitStatus"))
                && x.GetParameters().Any(x => x.Name.Contains("exitName"))
                && x.GetParameters().Any(x => x.Name.Contains("delay"))
                );

            Logger.LogInfo($"LocalGameEndingPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PatchPrefix(
            BaseLocalGame<GamePlayerOwner> __instance
            )
        {
            return !Matchmaker.MatchmakerAcceptPatches.IsClient;
        }

        [PatchPostfix]
        public static void Postfix(
            BaseLocalGame<GamePlayerOwner> __instance
            , string profileId, ExitStatus exitStatus, string exitName, float delay
            )
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsClient)
            {
                EndSession(__instance, profileId, exitStatus, exitName, delay);
            }
        }

        public static void EndSession(
            BaseLocalGame<GamePlayerOwner> game
            , string profileId
            , ExitStatus exitStatus
            , string exitName
            , float delay)
        {
            if (profileId != game.Profile_0.Id || game.Status == GameStatus.Stopped || game.Status == GameStatus.Stopping)
            {
                Logger.LogInfo("EndSession: Unable to End the Session!");
                return;
            }
            var gameStatus = PatchConstants.GetFieldOrPropertyFromInstance
                                   <GameStatus>(game, "Status", false);
            if (game.Status == GameStatus.Starting || game.Status == GameStatus.Started)
            {
                //__instance.endByTimerScenario_0.GameStatus_0 = GameStatus.SoftStopping;
                var endByTimerScenario_0 = PatchConstants.GetFieldOrPropertyFromInstance
                                <EndByTimerScenario>(game, "endByTimerScenario_0", false);
                if (endByTimerScenario_0 != null)
                {
                    var endByTimerScenarioGameStatus = PatchConstants.GetFieldOrPropertyFromInstance
                                    <GameStatus>(endByTimerScenario_0, "GameStatus_0", false);
                    endByTimerScenarioGameStatus = GameStatus.SoftStopping;
                }

            }
            gameStatus = GameStatus.Stopping;
            //__instance.Status = GameStatus.Stopping;
            game.GameTimer.TryStop();
            //__instance.endByExitTrigerScenario_0.Stop();
            //__instance.gameUI_0.TimerPanel.Close();
            if (EnvironmentManager.Instance != null)
            {
                EnvironmentManager.Instance.Stop();
            }
            MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, delegate
            {
                if (GlobalScreenController.CheckCurrentScreen(EScreenType.Reconnect))
                {
                    GlobalScreenController.CloseAllScreensForced();
                }
                //__instance.gparam_0.Player.OnGameSessionEnd(exitStatus, __instance.PastTime, __instance.GClass1113_0.Id, exitName);
                LocalGamePatches.MyPlayer.OnGameSessionEnd(exitStatus, game.PastTime, game.LocationObjectId, exitName);
                game.CleanUp();
                //__instance.Status = GameStatus.Stopped;
                gameStatus = GameStatus.Stopped;
                PatchConstants.SetFieldOrPropertyFromInstance(game, "GameStatus", gameStatus);

                //TimeSpan timeSpan = GClass1150.Now - this.dateTime_0;
                //__instance.ginterface120_0.OfflineRaidEnded(exitStatus, exitName, timeSpan.TotalSeconds).HandleExceptions();
                PatchConstants.BackEndSession.OfflineRaidEnded(exitStatus, exitName, 0);
                StaticManager.Instance.WaitSeconds(delay, delegate
                {
                    //__instance.callback_0(new Result<ExitStatus, TimeSpan, Metrics>(exitStatus, GClass1150.Now - this.dateTime_0, new Metrics()));
                    var callback_0 = PatchConstants.GetFieldOrPropertyFromInstance
                                <Callback<ExitStatus, TimeSpan, Metrics>>(game, "callback_0", false);
                    if (callback_0 != null)
                    {
                        callback_0(new Result<ExitStatus, TimeSpan, Metrics>(exitStatus, new TimeSpan(), new Metrics()));
                    }
                    UIEventSystem.Instance.Enable();
                });
            });
        }
    }
}
