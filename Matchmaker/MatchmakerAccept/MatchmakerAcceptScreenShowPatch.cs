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
				 .Single(x => x == typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen));
		}

        protected override MethodBase GetTargetMethod()
        {

            var methodName = "Show";

            return GetThisType().GetMethods(privateFlags).First(x=>x.Name == methodName);

        }


		private static Button _updateListButton;

		[PatchPrefix]
        private static bool PatchPrefix(
            ref object session, ref ESideType side, ref object selectedDateTime, ref object location, ref bool local, ref string keyId,

			ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
			//ref ScreenController ___ScreenController, 
			//ref Button ____updateListButton,
			ref Profile ___profile_0
			)
        {
			Logger.LogInfo("MatchmakerAcceptScreenShow.PatchPrefix");
			//_updateListButton = ____updateListButton;
			MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;
			//MatchmakerAcceptPatches.ScreenController = ___ScreenController;
            local = false;

            var typeOfInstance = __instance.GetType();
            var fields = typeOfInstance.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FieldInfo property in fields)
            {
                if (property.Name.Contains("bool"))
                {
                    Logger.LogInfo($"MatchmakerAcceptScreenShow.PatchPrefix:Set {property.Name} to false");
                    property.SetValue(MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance, false);
                }
            }

            //return false; // dont do anything, think for ourselves?
            return true; // run the original

        }

        [PatchPostfix]
        private static void PatchPostfix(
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

            ____acceptButton.gameObject.SetActive(true);
            ____playersRaidReadyPanel.ShowGameObject();
            ____playersRaidReadyPanel.gameObject.SetActive(true);



            //local = false;

            //try
            //{
            //var obj = Activator.CreateInstance(SIT.Tarkov.Core.PatchConstants.GroupingType, new object[] { session, ___profile_0 });
            //if (obj != null)
            //{
            //    Logger.LogInfo("MatchmakerAcceptScreenShow.PatchPostfix.Grouping object created " + obj.GetType().FullName);
            //}

            //var typeOfInstance = __instance.GetType();
            //Logger.LogInfo(typeOfInstance.Name);

            //Logger.LogInfo(SIT.Tarkov.Core.PatchConstants.GroupingType.Name);

            ////var fields = typeOfInstance.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            ////foreach (FieldInfo property in fields)
            ////{
            ////    if (property.Name.ToLower().Contains(SIT.Tarkov.Core.PatchConstants.GroupingType.Name.ToLower()))
            ////    {
            //var property = Tarkov.Core.PatchConstants.GetAllFieldsForObject(typeOfInstance).Single(x => x.Name.ToLower().Contains(SIT.Tarkov.Core.PatchConstants.GroupingType.Name.ToLower()));
            //MatchmakerAcceptPatches.Grouping = property.GetValue(MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance);
            //Logger.LogInfo($"MatchmakerAcceptScreenShow.PatchPostfix:Found {property.Name} and assigned to Grouping");

            //        break;
            //    }
            //}

            //    }
            //    else
            //    {
            //        Logger.LogError("ERROR creating Grouping object");
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
            Task.Run(PeriodicUpdate);

        }

        //      [PatchPostfix]
        //      private static void PatchPostfix(
        //	ref EFT.UI.Matchmaker.MatchMakerAcceptScreen __instance,
        //	bool local)
        //      {
        //	local = false;
        //	Logger.LogInfo("MatchmakerAcceptScreenShow.PatchPostfix");

        //	if (MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance == null)
        //		MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance = __instance;

        //	if (MatchmakerAcceptPatches.Grouping == null)
        //		MatchmakerAcceptPatches.Grouping = PrivateValueAccessor.GetPrivateFieldValue(
        //							   GetThisType(),
        //							   MatchmakerAcceptPatches.GroupingPropertyName,
        //							   MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance) as Grouping;

        //	_ = PeriodicUpdate();
        //}

        public static async void PeriodicUpdate()
        {
            await Task.Delay(1000);
            if (MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance != null)
            {
                //Logger.LogInfo("PeriodicUpdate");

                //while (MatchmakerAcceptPatches.Grouping != null && !MatchmakerAcceptPatches.Grouping.Disposed)
                //{
                //    await Task.Delay(1000);
                //    //await DoPeriodicUpdateAndSearches();

                //    MatchmakerAcceptPatches.Grouping = PrivateValueAccessor.GetPrivateFieldValue(
                //                               GetThisType(),
                //                               MatchmakerAcceptPatches.GroupingPropertyName,
                //                               MatchmakerAcceptPatches.MatchMakerAcceptScreenIntance) as Grouping;
                //}

                string json = new Request().GetJson("/client/match/group/getInvites");
                if (!string.IsNullOrEmpty(json))
                {
                    //    object gClass = JsonConvert.DeserializeObject<object>(json);
                    //    var from = Tarkov.Core.PatchConstants.GetFieldOrPropertyFromInstance<string>(gClass, "From");
                    //    if (gClass != null && !string.IsNullOrEmpty(from))
                    //    {
                    //        Logger.LogInfo("Invite Popup!");
                    //        //MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance
                    //        //    .GetType()
                    //        //    .GetMethod("method_8", privateFlags)
                    //        //    .Invoke(MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance, new object[] { from });
                    //        //this.InvitePopup(gClass);
                    //    }
                }

                GetMatchStatus(() => { }, () => { });
            }
            PeriodicUpdate();
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

        private static void GetMatchStatus(Action onLoading, Action onNothing)
        {

            if (MatchmakerAcceptPatches.Grouping != null
                && MatchmakerAcceptPatches.GetGroupPlayers() != null
                && MatchmakerAcceptPatches.GetGroupPlayers().Count > 0 
                && !MatchmakerAcceptPatches.IsGroupOwner()
                && !string.IsNullOrEmpty(MatchmakerAcceptPatches.GetGroupId())
                )
            {
                Logger.LogInfo("GetMatchStatus");
                string text = new Request().PostJson("/client/match/group/server/status", JsonConvert.SerializeObject(MatchmakerAcceptPatches.GetGroupId()));
                if (!string.IsNullOrEmpty(text))
                {
                    Debug.LogError("GetMatchStatus[1] ::" + text.Length);
                    ServerStatus serverStatus = JsonConvert.DeserializeObject<ServerStatus>(text);
                    Debug.LogError("GetMatchStatus[2] ::" + serverStatus.status);
                    if (serverStatus.status == "LOADING" || serverStatus.status == "INGAME")
                    {
                        Debug.LogError("GetMatchStatus[3] :: Starting up");
                        MatchmakerAcceptPatches.MatchingType = EMatchmakerType.GroupPlayer;
                        onLoading?.Invoke();
                    }
                }
            }
            onNothing?.Invoke();
        }

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
