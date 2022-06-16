using EFT;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.LocalGame
{
    internal class LocalGameBotWaveSystemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = LocalGamePatches.LocalGameInstance.GetType().BaseType;

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length == 2
                && x.GetParameters()[0].Name.Contains("wavesSettings")
                && x.GetParameters()[1].Name.Contains("waves")
                );
            return method;
        }

        [PatchPrefix]
        public static bool PatchPrefix(object wavesSettings, WildSpawnWave[] waves, WildSpawnWave[] __result, WavesSpawnScenario ___wavesSpawnScenario_0)
        {
            if (!Matchmaker.MatchmakerAcceptPatches.IsClient)
                return true;

            foreach (WildSpawnWave wildSpawnWave in waves)
            {
                wildSpawnWave.slots_min = 0;
                wildSpawnWave.slots_max = 0;
            }

            __result = waves;

            //___wavesSpawnScenario_0.Stop();

            return false;
        }

        [PatchPostfix]
        public static void PatchPostfix(object wavesSettings, WildSpawnWave[] waves, WildSpawnWave[] __result, WavesSpawnScenario ___wavesSpawnScenario_0)
        {
            foreach (WildSpawnWave wildSpawnWave in waves)
            {
                wildSpawnWave.slots_min = 0;
                wildSpawnWave.slots_max = 0;
            }

            __result = waves;

            //___wavesSpawnScenario_0.Stop();
        }
    }
}

/*
private static WildSpawnWave[] smethod_2(GStruct106 wavesSettings, WildSpawnWave[] waves)
{
*/