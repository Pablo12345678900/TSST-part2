using System.Net;

namespace Tools.Table_Entries
{
    public class FEC_Entry
    {
        public string destinationIP { get; set; }
        public int FEC { get; set; }

        public FEC_Entry(string destinationIp, int fec)
        {
            destinationIP = destinationIp;
            FEC = fec;
        }

        public FEC_Entry() { }
    }
}