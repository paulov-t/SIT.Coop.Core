using Comfort.Common;
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

        protected override MethodBase GetTargetMethod()
        {
            return PatchConstants.GetAllMethodsForType(LocalGamePatches.LocalGameInstance.GetType().BaseType)
                .Single(
                m =>
                m.IsVirtual
                && m.GetParameters().Length >= 4
                && m.GetParameters()[0].ParameterType == typeof(float)
                && m.GetParameters()[0].Name == "startDelay"
                && m.GetParameters()[1].Name == "controllerSettings"
                );
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

        [PatchPostfix]
        //public static IEnumerator PatchPostfix(
        //float startDelay, object controllerSettings, object spawnSystem, object runCallback,
        //GClass1222 ___gclass1222_0
        //)
        public static IEnumerator PatchPostfix(
            IEnumerator __result,
            object __instance,
            //GClass1222 ___gclass1222_0,
            //GClass543 ___gclass543_0,
            Callback runCallback
        )
        {
            var bossSpawner = PatchConstants.GetFieldOrPropertyFromInstance<object>(__instance, BotSystemHelpers.BossSpawnRunnerType.Name.ToLower() + "_0", false);

            // Run normally.
            if (!Matchmaker.MatchmakerAcceptPatches.IsClient)
            {

            }
            else 
            {
                //___gclass1222_0.SetSettings(0, new GClass1051[0], new GClass500[0]);
                BotSystemHelpers.SetSettingsNoBots();
                try
                {
                    BotSystemHelpers.Stop();
                    //___gclass1222_0.Stop();
                }
                catch
                {

                }
                //yield return new WaitForSeconds(startDelay);
                yield return new WaitForSeconds(1);
                //___gclass543_0.Run();
                //using (GClass19.StartWithToken("SessionRun"))
                using (PatchConstants.StartWithToken("SessionRun"))
                {
                    //this.vmethod_4();
                    // TODO: This needs removing!
                    PatchConstants.GetMethodForType(__instance.GetType(), "vmethod_4").Invoke(__instance, new object[0]);
                }
                runCallback.Succeed();
            }
        }
    }
}
