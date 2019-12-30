namespace Tools.Table_Entries
{
    public class FTN_Entry
    {
        public int NHLFE_ID { get; set; }
        public int FEC { get; set; }

        public FTN_Entry(int nhlfeId, int fec)
        {
            NHLFE_ID = nhlfeId;
            FEC = fec;
        }

        public FTN_Entry() { }
    }
}