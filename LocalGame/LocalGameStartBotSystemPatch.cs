using Comfort.Common;
using EFT;
using EFT.UI;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.LocalGame
{
    /// <summary>
    /// This must be run AFTER LocalGamePatches.LocalGameInstance has been created
    /// </summary>
    internal class LocalGameStartBotSystemPatch : ModulePatch
    {

        private static MethodInfo TimeBeforeDeployMethod { get; set; } 
        private static MethodInfo SetupExfilsBeforeStartMethod { get; set; } 

        private static GameStatus GameStatus { get {

                if (LocalGamePatches.LocalGameInstance == null)
                    return GameStatus.Stopped;

                return (GameStatus)PatchConstants.GetAllPropertiesForObject(LocalGamePatches.LocalGameInstance).Single(x => x.Name == "Status").GetValue(LocalGamePatches.LocalGameInstance);


            } }

        /// <summary>
        /// This class requires you to run some methods late, so get the instance (after it has been created) and then the base type to get the right method
        /// 
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            var t = LocalGamePatches.LocalGameInstance.GetType().BaseType;

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length == 3
                && x.GetParameters()[0].Name.Contains("botsSettings")
                && x.GetParameters()[1].Name.Contains("spawnSystem")
                && x.GetParameters()[2].Name.Contains("runCallback")
                );

            //foreach (var ty in SIT.Tarkov.Core.PatchConstants.GetAllMethodsForType(t).Where(x=>x.GetParameters().Length >= 1 && x.GetParameters().Any(y=>y.Name.ToLower().Contains("callback"))))
            //{
            //    Logger.LogInfo($"LocalGameStartingPatch:{ty.Name}({string.Join(",", ty.GetParameters().Select(x=>x.Name))})");
            //}
            Logger.LogInfo($"LocalGameStartBotSystemPatch:{t.Name}:{method.Name}");

            TimeBeforeDeployMethod = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length == 1
                && x.GetParameters()[0].Name.Contains("timeBeforeDeploy")
                );
            Logger.LogInfo($"LocalGameStartBotSystemPatch:TimeBeforeDeployMethod:{TimeBeforeDeployMethod.Name}");

            SetupExfilsBeforeStartMethod =  PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "vmethod_4"
                );
            Logger.LogInfo($"LocalGameStartBotSystemPatch:SetupExfilsBeforeStartMethod:{SetupExfilsBeforeStartMethod.Name}");

            return method;
        }

        [PatchPrefix]
        public static bool PatchPrefix(Callback runCallback, object ___gameUI_0)
        {
            Logger.LogInfo($"LocalGameStartBotSystemPatch:PatchPrefix");
            if(!Matchmaker.MatchmakerAcceptPatches.IsClient)
                return true;



            //return true;

            int timeBeforeDeploy = 5;
            //if (TimeBeforeDeployMethod == null)
            //{
            //    Logger.LogInfo($"LocalGameStartBotSystemPatch:PatchPrefix:TimeBeforeDeployMethod is NULL");
            //    return true;
            //}

            //TimeBeforeDeployMethod.Invoke(LocalGamePatches.LocalGameInstance, new object[] { timeBeforeDeploy });

            //using (PatchConstants.StartWithToken("SessionRun"))
            //{
            //    SetupExfilsBeforeStartMethod.Invoke(__instance, null);
            //}

            //Singleton<GUISounds>.Instance.method_4();
            Singleton<BetterAudio>.Instance.TweenAmbientVolume(0f, timeBeforeDeploy);
            //this.gameUI_0.gameObject.SetActive(true);
            //this.gameUI_0.TimerPanel.ProfileId = this.ProfileId;

            //runCallback.Succeed();
            //return false;
            return true;
        }

    }
}
