using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
   public class LinkResourceManager
   {
        public ushort port { get; set; }
        public bool[] slots { get; set; }
        public LinkResourceManager(ushort port)
        {
            this.port = port;
            slots = new bool[10]; // 10 available slots
            for(int i=0;i<slots.Length;i++)
            {
                slots[i] = true; // at the start, each of slot is available
            }
        }
   }
}
