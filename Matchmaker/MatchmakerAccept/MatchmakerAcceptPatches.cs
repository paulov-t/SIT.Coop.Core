using EFT;
using EFT.UI.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EFT.UI.Matchmaker.MatchMakerAcceptScreen;
//using ScreenController = EFT.UI.Matchmaker.MatchMakerAcceptScreen.GClass2426;
using Grouping = GClass2434;
using SIT.Coop.Core.Matchmaker.MatchmakerAccept;
using System.Reflection;
using Newtonsoft.Json;
using SIT.Z.Coop.Core.Matchmaker.MatchmakerAccept;
using SIT.Z.Coop.Core.Matchmaker.MatchmakerAccept.Grouping;

namespace SIT.Coop.Core.Matchmaker
{
    public enum EMatchmakerType
    {
        Single = 0,
        GroupPlayer = 1,
        GroupLeader = 2
    }

    public static class MatchmakerAcceptPatches
    {
        public static EFT.UI.Matchmaker.MatchMakerAcceptScreen MatchMakerAcceptScreenInstance { get; set; }
        //public static string GroupId { get; set; }
        public static EMatchmakerType MatchingType { get; set; }
        public static bool ForcedMatchingType { get; set; }
        //public static ScreenController ScreenController { get; set; }
        public static Profile Profile { get; set; }

        /// <summary>
        /// The Grouping object - you must reflect to get its properties and methods
        /// </summary>
        public static object Grouping { get; set; }

        public static IList<object> GetGroupPlayers()
        {
            if (Grouping == null)
                return new List<object>();

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

        public static string GetGroupId()
        {
            if (Grouping == null)
                return string.Empty;

            return Tarkov.Core.PatchConstants.GetFieldOrPropertyFromInstance<string>(Grouping, "GroupId", true);
        }

        public static void SetGroupId(string newId)
        {
            if (Grouping == null)
                return;

            Grouping.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).First(x => x.Name == "GroupId").SetValue(Grouping, newId);
        }

        //public static string GroupingPropertyName { get { return "gclass2434_0"; } }

        public static bool IsServer => MatchingType == EMatchmakerType.GroupLeader;

        public static bool IsClient => MatchingType == EMatchmakerType.GroupPlayer;

        public static bool IsSinglePlayer => MatchingType == EMatchmakerType.Single;
        public static int HostExpectedNumberOfPlayers { get; set; }

        public static void Run()
        {
            //new MatchmakerAcceptScreenUpdate().Enable();

            new MatchmakerAcceptScreenAwakePatch().Enable();
            new MatchmakerAcceptScreenShowPatch().Enable();
            new AcceptInvitePatch().Enable();
            new SendInvitePatch().Enable();
            //new MatchmakerAcceptScreenShowContextPatch().Enable();

            //new ConsistencySinglePatch().Enable();
            //new ConsistencyMultiPatch().Enable();
            //new BattlEyePatch().Enable();
            //new SslCertificatePatch().Enable();
            //new UnityWebRequestPatch().Enable();
            //new WebSocketPatch().Enable();
        }
    }
}
