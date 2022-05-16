using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Matchmaker
{
    internal class MatchmakerAcceptScreenUpdate : ModulePatch
    {

        static BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        protected override MethodBase GetTargetMethod()
        {

            var methodName = "Update";

            return PatchConstants.EftTypes
                .Single(x => x == typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen))
                .GetMethod(methodName, privateFlags);

        }

        [PatchPrefix]
        private static void PatchPrefix(ref GClass2434 ___gclass2434_0)
        {
            if(___gclass2434_0 != null)
            {
                MatchmakerAcceptPatches.Grouping = ___gclass2434_0;
            }
        }
    }
}
