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

namespace SIT.Coop.Core.Matchmaker
{
    public class MatchmakerAcceptScreenShowPatch : ModulePatch
    {
        

        static BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static Type GetThisType()
        {
            return Tarkov.Core.PatchConstants.EftTypes
                 .Single(x => x == typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen));
        }

        protected override MethodBase GetTargetMethod()
        {

            var methodName = "Show";

            return GetThisType().GetMethods(privateFlags).First(x => x.Name == methodName);

        }


        private static Button _updateListButton;

        

        [PatchPrefix]
        private static bool PatchPrefix(
            //ref object session, ref ESideType side, ref object selectedDateTime, ref object location, ref bool local, ref string keyId,
            ref object session, ref RaidSettings raidSettings,
            ref EFT.ERaidMode ___eraidMode_0,
            ref EFT.RaidSettings ___raidSettings_0,
            ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
            //ref ScreenController ___ScreenController, 
            ref DefaultUIButton ____updateListButton,
            ref Profile ___profile_0
			)
        {
			Logger.LogInfo("MatchmakerAcceptScreenShow.PatchPrefix");
			//_updateListButton = ____updateListButton;
			MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;

            raidSettings.RaidMode = ERaidMode.Online;
            //eraidMode_0 = ERaidMode.Online;
            //MatchmakerAcceptPatches.ScreenController = ___ScreenController;
            //local = false;
            //___raidSettings_0.RaidMode = ERaidMode.Online;
            //___raidSettings_0.RaidMode = ERaidMode.Coop;

            //return false; // dont do anything, think for ourselves?
            return true; // run the original

        }

        [PatchPostfix]
        private static void PatchPostfix(
            ref object session, ref RaidSettings raidSettings,
            ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
			ref Profile ___profile_0,
            ref DefaultUIButton ____acceptButton,
            ref DefaultUIButton ____playersRaidReadyPanel,
			ref DefaultUIButton ____groupPreview
            )
        {

            Logger.LogInfo("MatchmakerAcceptScreenShow.PatchPostfix");
            Logger.LogInfo(___profile_0.AccountId);

            MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;

            raidSettings.RaidMode = ERaidMode.Local;

            ____acceptButton.gameObject.SetActive(true);
            ____playersRaidReadyPanel.ShowGameObject();
            ____playersRaidReadyPanel.gameObject.SetActive(true);
        }


        //	if (MatchmakerAcceptPatches.Grouping == null || MatchmakerAcceptPatches.Grouping.Disposed)
        //	{
        //		Logger.LogInfo("PeriodicUpdate::Grouping is NULL!");
        //	}
        //}

        //private static async Task DoPeriodicUpdateAndSearches()
        //{
        //	Logger.LogInfo("DoPeriodicUpdateAndSearches");

        //	await Task.Delay(1000);
        //	//UpdateListButtonClickedStatus(false);
        //	//Task task2 = UpdateGroupStatus();
        //	GetInvites(delegate
        //	{
        //	});
        //	GetMatchStatus(delegate
        //	{
        //		MatchmakerAcceptPatches.MatchingType = EMatchingType.GroupPlayer;
        //		MatchmakerAcceptPatches.GroupId = JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(MatchmakerAcceptPatches.Grouping.GroupId));
        //		Debug.LogError("Starting Game as " + MatchmakerAcceptPatches.MatchingType.ToString() + " in GroupId " + MatchmakerAcceptPatches.GroupId + " via Match Status");
        //		MatchmakerAcceptPatches.Grouping.Dispose().HandleExceptions();
        //		MatchmakerAcceptPatches.ScreenController.ShowNextScreen(MatchmakerAcceptPatches.GroupId, MatchmakerAcceptPatches.MatchingType);
        //	}, null);
        //	//await Task.WhenAll(task2);
        //	UpdateListButtonClickedStatus(true);
        //}

        //private static void UpdateListButtonClickedStatus(bool visible)
        //{
        //	Logger.LogInfo("UpdateListButtonClickedStatus");

        //	_updateListButton.visible = visible;
        //}

        //private static Task UpdateGroupStatus()
        //{
        //	Logger.LogInfo("UpdateGroupStatus");

        //	TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
        //	if (MatchmakerAcceptPatches.Grouping != null)
        //	{
        //		//MatchmakerAcceptPatches.Grouping.UpdateStatus(this.string_3, this.edateTime_0, this.string_4, delegate
        //		//{
        //		//	source.SetResult(true);
        //		//});

        //		source.SetResult(true);
        //		return source.Task;
        //	}
        //	return source.Task;
        //}

        //private static void GetMatchStatus(Action onLoading, Action onNothing)
        //{

        //    if (MatchmakerAcceptPatches.Grouping != null
        //        && MatchmakerAcceptPatches.GetGroupPlayers() != null
        //        && MatchmakerAcceptPatches.GetGroupPlayers().Count > 0 
        //        && !MatchmakerAcceptPatches.IsGroupOwner()
        //        && !string.IsNullOrEmpty(MatchmakerAcceptPatches.GetGroupId())
        //        )
        //    {
        //        Logger.LogInfo("GetMatchStatus");
        //        string text = new Request().PostJson("/client/match/group/server/status", JsonConvert.SerializeObject(MatchmakerAcceptPatches.GetGroupId()));
        //        if (!string.IsNullOrEmpty(text))
        //        {
        //            Debug.LogError("GetMatchStatus[1] ::" + text.Length);
        //            ServerStatus serverStatus = JsonConvert.DeserializeObject<ServerStatus>(text);
        //            Debug.LogError("GetMatchStatus[2] ::" + serverStatus.status);
        //            if (serverStatus.status == "LOADING" || serverStatus.status == "INGAME")
        //            {
        //                Debug.LogError("GetMatchStatus[3] :: Starting up");
        //                MatchmakerAcceptPatches.MatchingType = EMatchmakerType.GroupPlayer;
        //                onLoading?.Invoke();
        //            }
        //        }
        //    }
        //    onNothing?.Invoke();
        //}

        //private static void GetInvites(Action onComplete)
        //{
        //	Logger.LogInfo("GetInvites");

        //	if (MatchmakerAcceptPatches.Grouping != null && !MatchmakerAcceptPatches.Grouping.IsInvited && !MatchmakerAcceptPatches.Grouping.IsMeInGroup && !MatchmakerAcceptPatches.Grouping.IsOwner)
        //	{
        //		GetInvites2();
        //	}
        //}

        //private static void GetInvites2()
        //{
        //	Logger.LogInfo("GetInvites2");

        //	string json = Web.WebCallHelper.GetJson("/client/match/group/getInvites");
        //	if (!string.IsNullOrEmpty(json))
        //	{
        //		Logger.LogInfo(json);

        //		GClass1032 gClass = JsonConvert.DeserializeObject<GClass1032>(json);
        //		if (gClass != null && !string.IsNullOrEmpty(gClass.From))
        //		{
        //			//this.InvitePopup(gClass);
        //		}
        //	}
        //}


    }

	
}
