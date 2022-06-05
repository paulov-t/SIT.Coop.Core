using EFT.UI.Matchmaker;
using Newtonsoft.Json;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using ScreenController = EFT.UI.Matchmaker.MatchMakerAcceptScreen.GClass2426;
//using Grouping = GClass2434;
using EFT.UI;
using UnityEngine.UIElements;
using EFT;
using HarmonyLib;
using UnityEngine.Events;

namespace SIT.Coop.Core.Matchmaker
{
    public class MatchmakerAcceptScreenAwakePatch : ModulePatch
    {
		[Serializable]
		private class ServerStatus
		{
			[JsonProperty("ip")]
			public string ip { get; set; }

			[JsonProperty("status")]
			public string status { get; set; }
		}

		static BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

		public static Type GetThisType()
		{
            return Tarkov.Core.PatchConstants.EftTypes
                 //.Single(x => x == typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen));
                 .Single(x => x.FullName == "EFT.UI.Matchmaker.MatchMakerAcceptScreen");
		}

        protected override MethodBase GetTargetMethod()
        {

            var methodName = "Awake";

            return GetThisType().GetMethods(privateFlags).First(x=>x.Name == methodName);

        }


        private static object screenController;

		private static Button _updateListButton;

        private static CanvasGroup _canvasGroup;

        private static Profile profile;

		[PatchPrefix]
        private static bool PatchPrefix(
            ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
            ref DefaultUIButton ____backButton,
            ref DefaultUIButton ____acceptButton,
            ref DefaultUIButton ____updateListButton,
            ref Profile ___profile_0,
            ref CanvasGroup ____canvasGroup
            )
        {
			Logger.LogInfo("MatchmakerAcceptScreenAwakePatch.PatchPrefix");

			
			MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;
            var screenControllerFieldInfo = __instance.GetType().GetField("ScreenController", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if(screenControllerFieldInfo != null)
            {
                //Logger.LogInfo("MatchmakerAcceptScreenAwakePatch.PatchPrefix.Found ScreenController FieldInfo");
                screenController = screenControllerFieldInfo.GetValue(__instance);
                if(screenController != null)
                {

                }
            }
            //var GotoNextScreenMethod = __instance.GetType().GetMethod("method_15", privateFlags);
            //var BackOutScreenMethod = __instance.GetType().GetMethod("method_20", privateFlags);
            //var UpdateListScreenMethod = __instance.GetType().GetMethod("method_22", privateFlags);
            ____acceptButton.OnClick.AddListener(() => { GoToRaid();});
            ____backButton.OnClick.AddListener(() => { BackOut(); });

            _canvasGroup = ____canvasGroup;
            _canvasGroup.interactable = true;
            profile = ___profile_0;
            //return false; // dont do anything, think for ourselves?
            return true; // run the original

        }

        public static void GoToRaid()
        {
            // SendInvitePatch sets the Host up
            if (MatchmakerAcceptPatches.IsSinglePlayer)
            {
                Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Singleplayer Game...");
            }
            else if(MatchmakerAcceptPatches.IsServer)
            {
                Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Coop Game as Host");
                if (profile != null)
                    MatchmakerAcceptPatches.SetGroupId(profile.AccountId);
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Coop Game as Client");
            }

            //Logger.LogInfo("MatchmakerAcceptPatches.Grouping is " + MatchmakerAcceptPatches.Grouping);
            //Logger.LogInfo("MatchmakerAcceptPatches.GroupId is " + MatchmakerAcceptPatches.GetGroupId());
            //Logger.LogInfo("MatchmakerAcceptPatches.IsGroupOwner is " + MatchmakerAcceptPatches.IsGroupOwner());

            //MatchmakerAcceptPatches.MatchingType = EMatchmakerType.Single;
            //if (string.IsNullOrEmpty(MatchmakerAcceptPatches.GetGroupId()) && profile != null)
            //{
            //    MatchmakerAcceptPatches.SetGroupId(profile.AccountId);
            //}
            //if (MatchmakerAcceptPatches.Grouping == null || string.IsNullOrEmpty(MatchmakerAcceptPatches.GetGroupId()))
            //{
            //    Logger.LogInfo("No Network to Connect to. Running SP Only");
            //    Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Singleplayer Game...");
            //    return;
            //}
            ////if (!MatchMakerAcceptScreen.ForcedMatchingType)
            ////{
            //MatchmakerAcceptPatches.MatchingType = MatchmakerAcceptPatches.IsGroupOwner() ? EMatchmakerType.GroupLeader : EMatchmakerType.GroupPlayer;
            //Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Coop Game...");

            //    MatchMakerAcceptScreen.GroupId = this.gclass2422_0.GroupId;
            //    MatchMakerAcceptScreen.Group = this.gclass2422_0;
            //    if (MatchMakerAcceptScreen.IsServer && MatchMakerAcceptScreen.Group != null)
            //    {
            //        MatchMakerAcceptScreen.HostExpectedNumberOfPlayers = MatchMakerAcceptScreen.Group.SentInvites.Count + 1;
            //        MatchmakerCoopImplementation.NumberOfInvites = MatchMakerAcceptScreen.Group.SentInvites.Count;
            //    }
            //    Debug.LogError("Starting MP Game as " + MatchMakerAcceptScreen.MyMatchingType.ToString() + " in GroupId " + MatchMakerAcceptScreen.GroupId + " with " + MatchMakerAcceptScreen.HostExpectedNumberOfPlayers + " players");
            //    GClass1676.DisplayMessageNotification("Starting " + MatchMakerAcceptScreen.MyMatchingType.ToString() + " game with " + MatchmakerCoopImplementation.NumberOfInvites + " pals!");
            //}
            //else
            //{
            //    Debug.LogError("Starting MP Game as Forced " + MatchMakerAcceptScreen.MyMatchingType);
            //}
        }

        public static void BackOut()
        {
            //Logger.LogInfo("BackOut!");
            if (screenController != null) 
            {
                if (MatchmakerAcceptPatches.Grouping != null)
                {
                    if (MatchmakerAcceptPatches.IsGroupOwner())
                    {
                        // Invoke Disband Group
                        MatchmakerAcceptPatches.Grouping
                            .GetType()
                            .GetMethod("DisbandGroup", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { null });
                    }
                    else
                    {
                        MatchmakerAcceptPatches.Grouping
                            .GetType()
                            .GetMethod("LeaveGroup", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { null });
                    }

                    MatchmakerAcceptPatches.Grouping
                            .GetType()
                            .GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { null });

                    MatchmakerAcceptPatches.Grouping
                            .GetType()
                            .GetMethod("ExitFromMatchMaker", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { });

                    MatchmakerAcceptPatches.Grouping = null;
                }
                _canvasGroup.interactable = false;
                screenController.GetType().GetMethod("CloseScreen", BindingFlags.Public | BindingFlags.Instance).Invoke(screenController, new object[] { });
                screenController = null;
                _canvasGroup = null;
            }
            
        }

    }

	
}
