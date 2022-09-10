using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Coop.Core.Player
{
    internal abstract class AReplicatedPlayerProcessor
    {
        List<Dictionary<string, object>> ReceivedPackets { get; set; } = new List<Dictionary<string, object>>();

        public virtual void ProcessPlayer(EFT.Player player)
        {
            ReceivedPackets = ReceivedPackets.OrderBy(x => (float)x["t"]).ToList();
            for (var i = 0; i < ReceivedPackets.Count; i++)
            {
                var cPacket = ReceivedPackets[i];
                ProcessPacket(player, cPacket);
            }
            ReceivedPackets.Clear();
        }

        public abstract void ProcessPacket(EFT.Player player, Dictionary<string, object> packet);
    }

    public interface IReplicatedPlayerProcessor
    {
    }
}
