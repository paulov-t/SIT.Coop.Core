using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnTiltPatch
    { 
        public static void TiltReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("tilt") && float.TryParse(dict["tilt"].ToString(), out var result))
            {
                player.CurrentState.SetTilt(result);
            }
        }
    }
}
