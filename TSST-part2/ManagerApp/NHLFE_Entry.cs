
using System.Collections.Generic;

namespace Tools.Table_Entries
{
    public class NHLFE_Entry
    {

        public int NHLFE_ID { get; set; }
        public string action { get; set; }
        public List<ushort> labelsOut { get; set; }
        public ushort portOut { get; set; }

        public int popDepth { get; set; }

        public NHLFE_Entry(int nhlfeId, string action, List<ushort> labelsOut, ushort portOut, int popDepth)
        {
            NHLFE_ID = nhlfeId;
            this.action = action;
            this.labelsOut = labelsOut;
            this.portOut = portOut;
            this.popDepth = popDepth;
        }

        public NHLFE_Entry()
        {
            labelsOut = new List<ushort>();
        }
    }
}