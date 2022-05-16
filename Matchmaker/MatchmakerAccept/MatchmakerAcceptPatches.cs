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

namespace SIT.Coop.Core.Matchmaker
{
    public static class MatchmakerAcceptPatches
    {
        public static EFT.UI.Matchmaker.MatchMakerAcceptScreen MatchMakerAcceptScreenInstance { get; set; }
        public static string GroupId { get; set; }
        public static EMatchingType MatchingType { get; set; }
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

            var properties = Grouping.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                if (property.Name.Contains("IsOwner"))
                {
                    // Log test
                    // SIT.Tarkov.Core.PatchConstants.Logger.LogInfo("Found GroupPlayers");
                    // Do a bit of serialization magic to stop a crash occurring due to change of enumerable type (we don't want to know its type, its a GClass)
                    return Tarkov.Core.PatchConstants.DoSafeConversion<bool>(property.GetValue(Grouping));
                }
            }

            return false;
        }

        //public static string GroupingPropertyName { get { return "gclass2434_0"; } }

        public static bool IsServer => MatchingType == EMatchingType.GroupLeader;

        public static bool IsClient => MatchingType == EMatchingType.GroupPlayer;

        public static bool IsSinglePlayer => MatchingType == EMatchingType.Single;
        public static int HostExpectedNumberOfPlayers { get; set; }

        public static void Run()
        {
            //new MatchmakerAcceptScreenUpdate().Enable();

            //new MatchmakerAcceptScreenAwake().Enable();
            new MatchmakerAcceptScreenShow().Enable();
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
