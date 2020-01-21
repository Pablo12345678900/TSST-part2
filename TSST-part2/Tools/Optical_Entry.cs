using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class Optical_Entry // routery będą miały 1 tablicę w sumie ( tak było na slajdach Komandosa)
    {
        public ushort inPort { get; set; }
        public int startSlot { get; set; }
        public int lastSlot { get; set; }
        public ushort outPort { get; set; }
        public Optical_Entry()
        {

        }
    }
}
