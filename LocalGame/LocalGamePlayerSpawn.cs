using Comfort.Common;
using EFT;
using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

                //gameWorld = Singleton<GameWorld>.Instance;
                //if (gameWorld != null)
                //{
                coopGameComponent = Plugin.Instance.GetOrAddComponent<CoopGameComponent>();
                if(Matchmaker.MatchmakerAcceptPatches.IsClient)
                {

                }
                    //gameWorld.GetType().DontDestroyOnLoad(coopGameComponent);
                //}
            });


            
        }

     
    }
}
