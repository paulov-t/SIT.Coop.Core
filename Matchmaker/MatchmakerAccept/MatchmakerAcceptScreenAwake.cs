//using SIT.Tarkov.Core;
//using SIT.Tarkov.Coop.Core;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using EFT.UI;
//using EFT.UI.Screens;
//using static EFT.UI.Matchmaker.MatchMakerAcceptScreen;
//using EFT;
//using ScreenController = EFT.UI.Matchmaker.MatchMakerAcceptScreen.GClass2426;
//using Grouping = GClass2434;
//using EFT.UI.Matchmaker;

//namespace SIT.Coop.Core.Matchmaker
//{

//    /// <summary>
//    /// Override MatchMakerAcceptScreen Awake call to make the Button Listeners do what we want
//    /// </summary>
//    public class MatchmakerAcceptScreenAwake : ModulePatch
//    {

//        static BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

//        public static Type GetThisType()
//        {
//            return Tarkov.Core.PatchConstants.EftTypes
//                 .Single(x => x == typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen));
//        }

//        protected override MethodBase GetTargetMethod()
//        {

//            var methodName = "Awake";

//            return GetThisType()
//                .GetMethod(methodName, privateFlags);

//        }

//        [PatchPrefix]
//        private static bool PatchPrefix(
//            ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
//            ref ScreenController ___ScreenController,
//            ref DefaultUIButton ____acceptButton
//            )
//        {
//            MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance = __instance;

//            if (MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance != null)
//            {
//                Logger.LogInfo($"Found MatchMakerAcceptScreen Intance");
//            }

//            MatchmakerAcceptPatches.ScreenController = ___ScreenController;
//            MatchmakerAcceptPatches.Profile = SIT.Tarkov.Core.PatchConstants.BackEndSession.Profile;

//            if (MatchmakerAcceptPatches.ScreenController != null)
//            {
//                Logger.LogInfo($"Found Screen Controller");
//            }
//            else
//            {
//                Logger.LogInfo($"Not Found Screen Controller");
//                return false;
//            }

//            // Grouping will ALWAYS BE NULL HERE!
//            //if (MatchmakerAcceptScreen.Grouping != null)
//            //{
//            //    Logger.LogInfo($"Found Grouping");
//            //}

//            if (MatchmakerAcceptPatches.Profile != null)
//            {
//                //Logger.LogInfo($"Found Profile");
//            }

//            if (____acceptButton != null)
//            {
//                ____acceptButton.OnClick.RemoveAllListeners();
//                ____acceptButton.OnClick.AddListener(delegate
//                {
//                    Logger.LogInfo($"Clicked AcceptButton");

//                    if (MatchmakerAcceptPatches.ScreenController != null)
//                    {
//                        Logger.LogInfo(MatchmakerAcceptPatches.GroupingPropertyName);

//                        MatchmakerAcceptPatches.Grouping = PrivateValueAccessor.GetPrivateFieldValue(
//                                       GetThisType(),
//                                       MatchmakerAcceptPatches.GroupingPropertyName,
//                                       MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance) as Grouping;


//                        MatchmakerAcceptPatches.MatchingType = EFT.UI.Matchmaker.EMatchingType.Single;
//                        if (string.IsNullOrEmpty(MatchmakerAcceptPatches.GroupId))
//                        {
//                            MatchmakerAcceptPatches.GroupId = MatchmakerAcceptPatches.Profile.AccountId;
//                        }
//                        if (MatchmakerAcceptPatches.Grouping == null || string.IsNullOrEmpty(MatchmakerAcceptPatches.Grouping.GroupId))
//                        {
//                            Logger.LogInfo("No Network to Connect to. Running SP Only");
//                            MatchmakerAcceptPatches.ScreenController.ShowNextScreen(null, EFT.UI.Matchmaker.EMatchingType.Single);
//                            //GClass1682.DisplayMessageNotification("Starting Singleplayer Game...");

//                            Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Singleplayer Game...");
//                            return;
//                        }
//                        if (!MatchmakerAcceptPatches.ForcedMatchingType)
//                        {
//                            MatchmakerAcceptPatches.MatchingType = ((!MatchmakerAcceptPatches.Grouping.IsOwner) ? EMatchingType.GroupPlayer : EMatchingType.GroupLeader);
//                            MatchmakerAcceptPatches.GroupId = MatchmakerAcceptPatches.Grouping.GroupId;
//                            if (MatchmakerAcceptPatches.IsServer && MatchmakerAcceptPatches.Grouping != null)
//                            {
//                                MatchmakerAcceptPatches.HostExpectedNumberOfPlayers = MatchmakerAcceptPatches.Grouping.SentInvites.Count + 1;
//                                //MatchmakerCoopImplementation.NumberOfInvites = MatchmakerAcceptPatches.Grouping.SentInvites.Count;
//                            }
//                            Logger.LogInfo("Starting Coop Game as " + MatchmakerAcceptPatches.MatchingType.ToString() + " in GroupId " + MatchmakerAcceptPatches.GroupId + " with " + MatchmakerAcceptPatches.HostExpectedNumberOfPlayers + " players");
//                            //GClass1682.DisplayMessageNotification("Starting Coop game with " + MatchmakerCoopImplementation.NumberOfInvites + " friend(s)!");
//                        }
//                        else
//                        {
//                            //Logger.LogInfo("Starting MP Game as Forced " + MatchMakerAcceptScreen.MyMatchingType);
//                        }
//                        MatchmakerAcceptPatches.Grouping.Dispose();
//                        MatchmakerAcceptPatches.ScreenController.ShowNextScreen(MatchmakerAcceptPatches.GroupId, MatchmakerAcceptPatches.MatchingType);
//                    }
//                });
//            }

//            return true;
//        }
//    }

//}
