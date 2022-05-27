using BepInEx.Logging;
using Newtonsoft.Json;
using SIT.Coop.Core.Matchmaker;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.LocalGame
{
    /// <summary>
    /// Target that smethod_3 like
    /// </summary>
    internal class LocalGameStartingPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            //foreach(var ty in SIT.Tarkov.Core.PatchConstants.EftTypes.Where(x => x.Name.StartsWith("BaseLocalGame")))
            //{
            //    Logger.LogInfo($"LocalGameStartingPatch:{ty}");
            //}
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName.StartsWith("EFT.LocalGame"));
            if (t == null)
                Logger.LogInfo($"LocalGameStartingPatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.GetParameters().Length >= 3
                && x.GetParameters()[0].Name.Contains("botsSettings")
                && x.GetParameters()[1].Name.Contains("entryPoint")
                && x.GetParameters()[2].Name.Contains("backendUrl")
                );

            //foreach(var m in PatchConstants.GetAllMethodsForType(t))
            //{
            //    Logger.LogInfo($"LocalGameStartingPatch:{m.Name}");
            //}
            // int playerId, Vector3 position, Quaternion rotation, string layerName, 
            //var method = PatchConstants.GetAllMethodsForType(t)
            //    .FirstOrDefault(x => x.GetParameters().Length >= 4
            //    && x.GetParameters()[0].Name.Contains("playerId")
            //    && x.GetParameters()[1].Name.Contains("position")
            //    && x.GetParameters()[2].Name.Contains("rotation")
            //    && x.GetParameters()[3].Name.Contains("layerName")
            //    );

            Logger.LogInfo($"LocalGameStartingPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static async void PatchPrefix(
            object __instance
            , Task __result
            )
        {

            Logger.LogInfo($"LocalGameStartingPatch:PatchPrefix");
            LocalGamePatches.LocalGameInstance = __instance;

            await StartAndConnectToServer(__instance);
        }

        [PatchPostfix]
        public static async void PatchPostfix(
            object __instance
            , Task __result
            )
        {
            Logger.LogInfo($"LocalGameStartingPatch:PatchPostfix");
            LocalGamePatches.LocalGameInstance = __instance;

            await StartAndConnectToServer(__instance);
            ServerCommunication.OnDataReceived += ServerCommunication_OnDataReceived;
        }

        public static async Task StartAndConnectToServer(object __instance)
        {
            Logger.LogInfo("Matchmaker Matching type is " + Matchmaker.MatchmakerAcceptPatches.MatchingType);
            if (!(__instance.GetType().Name.Contains("HideoutGame")) && MatchmakerAcceptPatches.MatchingType != EMatchmakerType.Single)
            {
                if (MatchmakerAcceptPatches.MatchingType == EMatchmakerType.GroupLeader)
                {
                    string myExternalAddress = ServerCommunication.GetMyExternalAddress();
                    WebCallHelper.PostJson("/client/match/group/server/start", true, JsonConvert.SerializeObject(myExternalAddress));
                    await Task.Delay(500);
                }
                else
                {
                    //MatchMakerAcceptScreen.ServerCommunicationCoopImplementation.PostJson(backendUrl + "/client/match/group/server/join", true, JsonConvert.SerializeObject(MatchMakerAcceptScreen.GroupId));
                    await Task.Delay(500);
                }
                SetMatchmakerStatus("Attempting to connect to Game Host. . . (BepInEx)");
                await Task.Delay(3000);
                //LocalGame.SetMatchmakerStatus("Attempting to connect to Game Host. . .");
                //if (this.ConnectToBackendWebSocket(backendUrl))
                //{
                //    LocalGame.SetMatchmakerStatus("Connected to Game Host");
                //    await Task.Delay(1000);
                //}
                //else
                //{
                //    LocalGame.SetMatchmakerStatus("Unable to Connect to MP servers. Reverting to Singleplayer.");
                //    await Task.Delay(3000);
                //    BaseLocalGame<TPlayerOwner>.ForceSinglePlayer = true;
                //}

                //MatchMakerAcceptScreen.ServerCommunicationCoopImplementation.OnPostLocalPlayerData += ServerCommunicationCoopImplementation_OnPostLocalPlayerData;
            }
        }

        private static void ServerCommunication_OnDataReceived(byte[] buffer)
        {
            Logger.LogInfo($"LocalGameStartingPatch:PatchPostfix:OnDataReceived");
        }

        private static void SetMatchmakerStatus(string status, float? progress = null)
        {
            if (LocalGamePatches.LocalGameInstance == null)
                return;


            var method = PatchConstants.GetAllMethodsForType(LocalGamePatches.LocalGameInstance.GetType()).First(x => x.Name == "SetMatchmakerStatus");
            if(method != null)
            {
                method.Invoke(LocalGamePatches.LocalGameInstance, new object[] { status, progress });
            }
            
        }
    }
}
