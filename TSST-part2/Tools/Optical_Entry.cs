using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class Optical_Entry
    {
        public ushort inPort { get; set; }
        public uint startSlot { get; set; }
        public uint lastSlot { get; set; }
        public ushort outPort { get; set; }
        public Optical_Entry()
        {

        }
    }
}
