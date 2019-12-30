using System;
using System.Collections.Generic;
using System.Text;
using Tools.Table_Entries;

namespace ManagerApp
{
    public class R_config
    {
         public String R_name;
        public String Description;

        public List<FEC_Entry> FEC;
        public List<FIB_Entry> FIB;
        public List<FTN_Entry> FTN;
        public List<ILM_Entry> ILM;
        public List<NHLFE_Entry> NHLFE;

        public R_config()
        {

            FEC = new List<FEC_Entry>();
            FIB = new List<FIB_Entry>();
            FTN = new List<FTN_Entry>();
            ILM = new List<ILM_Entry>();
            NHLFE = new List<NHLFE_Entry>();
        
        }
    }
}
