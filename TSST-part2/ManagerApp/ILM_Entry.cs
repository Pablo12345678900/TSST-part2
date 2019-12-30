
namespace Tools.Table_Entries
{
    public class ILM_Entry
    {
        public ushort portIn { get; set; }
        public ushort labelIn { get; set; }
        public int NHLFE_ID { get; set; }

        public ILM_Entry(ushort portIn, ushort labelIn, int nhlfeId)
        {
            this.portIn = portIn;
            this.labelIn = labelIn;
            NHLFE_ID = nhlfeId;
        }

        public ILM_Entry() { }
    }
}