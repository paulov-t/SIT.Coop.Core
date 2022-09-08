using EFT;
using EFT.UI.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EFT.UI.Matchmaker.MatchMakerAcceptScreen;
//using ScreenController = EFT.UI.Matchmaker.MatchMakerAcceptScreen.GClass2426;
//using Grouping = GClass2434;
using SIT.Coop.Core.Matchmaker.MatchmakerAccept;
using System.Reflection;
using Newtonsoft.Json;
using SIT.Coop.Core.Matchmaker.MatchmakerAccept;
using SIT.Coop.Core.Matchmaker.MatchmakerAccept.Grouping;
using SIT.Tarkov.Core;

namespace SIT.Coop.Core.Matchmaker
{
    public enum EMatchmakerType
    {
        Single = 0,
        GroupPlayer = 1,
        GroupLeader = 2
    }

    [Serializable]
    public class ServerStatus
    {
        [JsonProperty("ip")]
        public string ip { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }
    }

    public static class MatchmakerAcceptPatches
    {
        public static EFT.UI.Matchmaker.MatchMakerAcceptScreen MatchMakerAcceptScreenInstance { get; set; }

        public static object MatchmakerScreenController 
        { 
            get 
            {
                var screenController = PatchConstants.GetFieldOrPropertyFromInstance<object>(MatchMakerAcceptScreenInstance, "ScreenController", false);
                if (screenController != null)
                {
                    PatchConstants.Logger.LogInfo("MatchmakerAcceptPatches.Found ScreenController Instance");

                    return screenController;

                }
                return null;
            } 
        }
        //public static string GroupId { get; set; }
        public static EMatchmakerType MatchingType { get; set; } = EMatchmakerType.Single;
        public static bool ForcedMatchingType { get; set; }
        //public static ScreenController ScreenController { get; set; }
        public static Profile Profile { get; set; }

        private static object _grouping;
        /// <summary>
        /// The Grouping object - you must reflect to get its properties and methods
        /// </summary>
        public static object Grouping { get { return GetGrouping(); } set { _grouping = value; } }

        public static object GetGrouping()
        {
            if (MatchMakerAcceptScreenInstance == null)
                return null;

            var typeOfInstance = MatchMakerAcceptScreenInstance.GetType();
            //PatchConstants.Logger.LogInfo(typeOfInstance.Name);
            //PatchConstants.Logger.LogInfo(SIT.Tarkov.Core.PatchConstants.GroupingType.Name);

            //foreach(var f in Tarkov.Core.PatchConstants.GetAllFieldsForObject(MatchMakerAcceptScreenInstance))
            //{
            //    PatchConstants.Logger.LogInfo($"{f}");
            //}

            //foreach (var p in Tarkov.Core.PatchConstants.GetAllPropertiesForObject(MatchMakerAcceptScreenInstance))
            //{
            //    PatchConstants.Logger.LogInfo($"{p}");
            //}

            var property = Tarkov.Core.PatchConstants.GetAllFieldsForObject(MatchMakerAcceptScreenInstance)
                .Single(x => x.Name.ToLower().Contains(SIT.Tarkov.Core.PatchConstants.GroupingType.Name.ToLower()));
            _grouping = property.GetValue(MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance);
            //PatchConstants.Logger.LogInfo($"MatchmakerAcceptScreenShow.PatchPostfix:Found {property.Name} and assigned to Grouping");
            return _grouping;
        }

        public static IList<object> GetGroupPlayers()
        {
            if (Grouping == null)
                return null;
                //return new List<object>();

            var properties = Grouping.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                if (property.Name.Contains("GroupPlayers"))
                {
                    // Log test
                    // SIT.Tarkov.Core.PatchConstants.Logger.LogInfo("Found GroupPlayers");
                    // Do a bit of serialization magic to stop a crash occurring due to change of enumerable type (we don't want to know its type, its a GClass)
                    return JsonConvert.DeserializeObject<IList<object>>(JsonConvert.SerializeObject(property.GetValue(Grouping)));
                }
            }

            return new List<object>();
        }

        public static bool IsGroupOwner()
        {
            if (Grouping == null)
                return false;

            return Tarkov.Core.PatchConstants.GetFieldOrPropertyFromInstance<bool>(Grouping, "IsOwner", true);
        }

        private static string groupId;

        public static string GetGroupId()
        {
            return groupId;
            //if (Grouping == null)
            //    return string.Empty;

            //return Tarkov.Core.PatchConstants.GetFieldOrPropertyFromInstance<string>(Grouping, "GroupId", true);
        }

        public static void SetGroupId(string newId)
        {
            groupId = newId;
            //if (Grouping == null)
            //    return;

            //Grouping.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).First(x => x.Name == "GroupId").SetValue(Grouping, newId);
        }

        public static bool CheckForMatch()
        {
            if (MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance != null)
            {
                string json = new SIT.Tarkov.Core.Request().GetJson("/client/match/group/getInvites");
                if (!string.IsNullOrEmpty(json))
                {
                    var gClass = Activator.CreateInstance(MatchmakerAcceptPatches.InviteType);
                    gClass = JsonConvert.DeserializeObject(json, MatchmakerAcceptPatches.InviteType);
                    var from = Tarkov.Core.PatchConstants.GetFieldOrPropertyFromInstance<string>(gClass, "From");
                    if (gClass != null && !string.IsNullOrEmpty(from))
                    {
                        PatchConstants.Logger.LogInfo($"Invite Popup! {gClass} {from}");

                        PatchConstants.Logger.LogInfo("GetMatchStatus");
                        string text = new SIT.Tarkov.Core.Request().PostJson("/client/match/group/server/status", JsonConvert.SerializeObject(from));
                        if (!string.IsNullOrEmpty(text))
                        {
                            //PatchConstants.Logger.LogInfo("GetMatchStatus[1] ::" + text.Length);
                            ServerStatus serverStatus = JsonConvert.DeserializeObject<ServerStatus>(text);
                            PatchConstants.Logger.LogInfo("GetMatchStatus[2] ::" + serverStatus.status);
                            if (serverStatus.status == "LOADING" || serverStatus.status == "INGAME")
                            {
                                PatchConstants.Logger.LogInfo("GetMatchStatus[3] :: Starting up");
                                //MatchmakerAcceptPatches.SetGroupId(from);
                                MatchmakerAcceptPatches.MatchingType = EMatchmakerType.GroupPlayer;
                                MatchmakerAcceptPatches.SetGroupId(from);
                                PatchConstants.DisplayMessageNotification("Server is running and waiting for you to join...");
                                return true;
                                //MatchmakerAcceptPatches.MatchmakerScreenController.GetType().GetMethod("ShowNextScreen", BindingFlags.Public | BindingFlags.Instance).Invoke(MatchmakerAcceptPatches.MatchmakerScreenController, new object[] { from, EFT.UI.Matchmaker.EMatchingType.GroupPlayer });

                            }
                        }
                    }
                    else
                    {
                        //MatchmakerAcceptPatches.MatchingType = EMatchmakerType.Single;
                    }
                }
            }
            return false;
        }

        //public static string GroupingPropertyName { get { return "gclass2434_0"; } }

        public static bool IsServer => MatchingType == EMatchmakerType.GroupLeader;

        public static bool IsClient => MatchingType == EMatchmakerType.GroupPlayer;

        public static bool IsSinglePlayer => MatchingType == EMatchmakerType.Single;
        public static int HostExpectedNumberOfPlayers { get; set; }

        public static Type InviteType { get; } = PatchConstants.EftTypes.Single(x =>
            (PatchConstants.GetPropertyFromType(x, "Id") != null
            || PatchConstants.GetFieldFromType(x, "Id") != null)
            && (PatchConstants.GetPropertyFromType(x, "From") != null
            || PatchConstants.GetFieldFromType(x, "From") != null)
            && (PatchConstants.GetPropertyFromType(x, "To") != null
            || PatchConstants.GetFieldFromType(x, "To") != null)
            && (PatchConstants.GetPropertyFromType(x, "GroupId") != null
            || PatchConstants.GetFieldFromType(x, "GroupId") != null)
            && (PatchConstants.GetPropertyFromType(x, "FromProfile") != null
            || PatchConstants.GetFieldFromType(x, "FromProfile") != null)
        );

        public static MethodInfo InvitePopupMethod { get; } = PatchConstants.GetAllMethodsForType(typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen))
            .First(x => x.GetParameters().Length >= 1 && x.GetParameters()[0].ParameterType == InviteType && x.GetParameters()[0].Name == "invite");

        public static void Run()
        {
            new MatchmakerAcceptScreenAwakePatch().Enable();
            new MatchmakerAcceptScreenShowPatch().Enable();
            new AcceptInvitePatch().Enable();
            new SendInvitePatch().Enable();
        }
    }
}
