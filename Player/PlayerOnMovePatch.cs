using SIT.Coop.Core.Matchmaker;
using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnMovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnMovePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => 
                x.GetParameters().Length == 1
                && x.GetParameters()[0].Name.Contains("direction")
                && x.Name == "Move"
                );

            Logger.LogInfo($"PlayerOnMovePatch:{t.Name}:{method.Name}");
            return method;
        }

        //[PatchPrefix]
        //public static void PatchPrefix(object gesture)
        //{
        //    Logger.LogInfo("OnGesturePatch.PatchPrefix");
        //}

        public static DateTime? LastMoveTime { get; set; }
        public static DateTime? LastAIMovesTime { get; set; }

        public static List<Vector2> StoredMoves = new List<Vector2>();
        public static DateTime? LastStoredMoveTime { get; set; }


        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance,
            Vector2 direction)
        {

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("nP", __instance.Transform.position);
            dictionary.Add("dX", direction.x.ToString());
            dictionary.Add("dY", direction.y.ToString());
            dictionary.Add("m", "Move");
            Task.Run(delegate
            {
               ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            });
            //Logger.LogInfo("PlayerOnMovePatch.PatchPostfix");
            //if (MatchmakerAcceptPatches.IsServer && IsAI)
            //{
            //    StoredMoves.Add(direction);
            //}

            //var timeChecker = -0.3;

            //if (
            //    //((MatchMakerAcceptScreen.IsServer && (IsMyPlayer || IsAI)) || IsMyPlayer)
            //    (IsMyPlayer)
            //    && (!LastMoveTime.HasValue || LastMoveTime < DateTime.Now.AddSeconds(timeChecker))
            //    )
            //{
            //    LastMoveTime = DateTime.Now;
            //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //    dictionary.Add("nP", Transform.position);
            //    dictionary.Add("dX", direction.x.ToString());
            //    dictionary.Add("dY", direction.y.ToString());
            //    dictionary.Add("m", "Move");
            //    Task.Run(delegate
            //    {
            //        ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //    });
            //}

            //if (
            //    (MatchmakerAcceptPatches.IsServer && IsAI)
            //    &&
            //    (!LastAIMovesTime.HasValue || LastAIMovesTime < DateTime.Now.AddSeconds(-0.9))
            //    )
            //{
            //    LastAIMovesTime = DateTime.Now;

            //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //    dictionary.Add("store", StoredMoves);
            //    dictionary.Add("nP", Transform.position);
            //    dictionary.Add("m", "AIMoves");
            //    Task.Run(delegate
            //    {
            //        ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //    });

            //    StoredMoves.Clear();
            //}


            //Logger.LogInfo("OnGesturePatch.PatchPostfix:Sent");

        }
    }
}
