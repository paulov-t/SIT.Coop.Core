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

        private static object grouping => MatchmakerAcceptPatches.GetGrouping();

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
            ref CanvasGroup ____canvasGroup,
            ref object ___UI,
            ref ERaidMode ___eraidMode_0
            )
        {
            //Logger.LogInfo("MatchmakerAcceptScreenAwakePatch.PatchPrefix");

            // ----------------------------------------------------
            // Reset number of players for next Raid
            MatchmakerAcceptPatches.HostExpectedNumberOfPlayers = 1;

            MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;
            screenController = MatchmakerAcceptPatches.MatchmakerScreenController;
            ___eraidMode_0 = ERaidMode.Online;
            //var screenControllerFieldInfo = __instance.GetType().GetField("ScreenController", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            //if(screenControllerFieldInfo != null)
            //{
            //    //Logger.LogInfo("MatchmakerAcceptScreenAwakePatch.PatchPrefix.Found ScreenController FieldInfo");
            //    screenController = screenControllerFieldInfo.GetValue(__instance);
            //    if(screenController != null)
            //    {

            //    }
            //}
            //var GotoNextScreenMethod = __instance.GetType().GetMethod("method_15", privateFlags);
            //var BackOutScreenMethod = __instance.GetType().GetMethod("method_20", privateFlags);
            //var UpdateListScreenMethod = __instance.GetType().GetMethod("method_22", privateFlags);
            ____acceptButton.OnClick.AddListener(() => { GoToRaid();});
            ____backButton.OnClick.AddListener(() => { BackOut(); });
            ____updateListButton.OnClick.AddListener(() => 
            { 

                MatchmakerAcceptPatches.CheckForMatch(); 
            
            });

            //_canvasGroup = ____canvasGroup;
            //_canvasGroup.interactable = true;

            //DefaultUIButton startServerButton = GameObject.Instantiate<DefaultUIButton>(____acceptButton);
            //RectTransform acceptBtnRectTransform = ____acceptButton.GetComponent<RectTransform>();
            //RectTransform startServerRectTransform = startServerButton.GetComponent<RectTransform>();
            //startServerRectTransform.position = acceptBtnRectTransform.position;
            //startServerButton.SetRawText("CUNT!", 18);

            profile = ___profile_0;
            //return false; // dont do anything, think for ourselves?
            return true; // run the original

        }

        [PatchPostfix]
        private static void PatchPostfix(
            ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
            ref ERaidMode ___eraidMode_0
            )
        {
            //Logger.LogInfo("MatchmakerAcceptScreenAwakePatch.PatchPostfix");
        }

        public static void GoToRaid()
        {
            MatchmakerAcceptPatches.CheckForMatch();

            if (MatchmakerAcceptPatches.IsSinglePlayer)
            {
                Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Singleplayer Game...");
            }
            // SendInvitePatch sets up the Host
            else if (MatchmakerAcceptPatches.IsServer)
            {
                Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Coop Game as Host");
                MatchmakerAcceptPatches.SetGroupId(PatchConstants.GetPHPSESSID());
            }
            // MatchmakerAcceptPatches.CheckForMatch sets up the Client
            else if (MatchmakerAcceptPatches.IsClient)
            {
                Tarkov.Core.PatchConstants.DisplayMessageNotification("Starting Coop Game as Client");
            }

        }

        public static void BackOut()
        {
            //Logger.LogInfo("BackOut!");
            if (screenController != null) 
            {
                Logger.LogInfo("screenController.GetType():" + screenController.GetType().FullName);
                //if (MatchmakerAcceptPatches.Grouping != null)
                //{
                //    if (MatchmakerAcceptPatches.IsGroupOwner())
                //    {
                //        // Invoke Disband Group
                //        MatchmakerAcceptPatches.Grouping
                //            .GetType()
                //            .GetMethod("DisbandGroup", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { null });
                //    }
                //    else
                //    {
                //        MatchmakerAcceptPatches.Grouping
                //            .GetType()
                //            .GetMethod("LeaveGroup", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { null });
                //    }

                //    MatchmakerAcceptPatches.Grouping
                //            .GetType()
                //            .GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { null });

                //    MatchmakerAcceptPatches.Grouping
                //            .GetType()
                //            .GetMethod("ExitFromMatchMaker", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.Grouping, new object[] { });

                //    MatchmakerAcceptPatches.Grouping = null;
                //}
                //_canvasGroup.interactable = false;
                //screenController.GetType().GetMethod("CloseScreen", BindingFlags.Public | BindingFlags.Instance).Invoke(screenController, new object[] { });
                //screenController = null;
                //_canvasGroup = null;
            }
            
        }


    }

	
}
