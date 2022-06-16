using EFT.UI.Matchmaker;
using SIT.Coop.Core.Matchmaker;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Matchmaker.MatchmakerAccept
{
    public class AcceptInvitePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchMakerAcceptScreen).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).LastOrDefault(
                x =>
                x.GetParameters().Length > 0 &&
                x.GetParameters()[0].Name == "invite");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref object invite)
        {
            Logger.LogInfo("AcceptInvitePatch.PatchPostfix");
            MatchmakerAcceptPatches.MatchingType = EMatchmakerType.GroupPlayer;
            //MatchmakerAcceptPatches.SetGroupId(PatchConstants.GetFieldOrPropertyFromInstance<string>(invite, "From"));
        }
    }
}
