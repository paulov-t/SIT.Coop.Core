using EFT.UI;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.Matchmaker.MatchmakerAccept
{
    public class MatchmakerAcceptScreenShowContextPatch : ModulePatch
    {

        static BindingFlags publicFlag = BindingFlags.Public | BindingFlags.Instance;

        public static Type GetThisType()
        {
            return Tarkov.Core.PatchConstants.EftTypes
                 .Single(x => x == typeof(EFT.UI.Matchmaker.MatchMakerAcceptScreen));
        }

        protected override MethodBase GetTargetMethod()
        {

            var methodName = "ShowContextMenu";

            return GetThisType()
                .GetMethod(methodName, publicFlag);

        }

        [PatchPrefix]
        private static bool PatchPrefix(object player, Vector2 position, ref SimpleContextMenu ____contextMenu)
        {
            Logger.LogInfo("MatchmakerAcceptScreenShowContextPatch.PatchPrefix");
            return false;
        }
    }
}
