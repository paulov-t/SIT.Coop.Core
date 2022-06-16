using SIT.Coop.Core.Matchmaker;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Matchmaker.MatchmakerAccept.Grouping
{
    public class SendInvitePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return SIT.Tarkov.Core.PatchConstants.GroupingType.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.Name == "SendInvite");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref string playerId, ref string locationId)
        {
            //Logger.LogInfo("SendInvitePatch.PatchPrefix");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref string playerId, ref string locationId)
        {
            Logger.LogInfo("SendInvitePatch.PatchPostfix");
            MatchmakerAcceptPatches.MatchingType = EMatchmakerType.GroupLeader;
            MatchmakerAcceptPatches.HostExpectedNumberOfPlayers++;
            MatchmakerAcceptPatches.SetGroupId(PatchConstants.GetPHPSESSID());
        }
    }
}
