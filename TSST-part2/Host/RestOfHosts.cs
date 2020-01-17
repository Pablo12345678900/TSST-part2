using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Host
{
    public class RestOfHosts
    {
        public string Name { get; set; }
        public IPAddress ip { get; set; }
        public int modulation { get; set; }
        public uint firstFrequencySlot { get; set; } // these slots will be given by host ( if requested path will be free)
        public uint lastFrequencySlot { get; set; }
       // public bool isOnline { get; set; }
        public RestOfHosts(string path)
        {
            var data = path.Split(' ');
            Name = data[0];
            ip = IPAddress.Parse(data[1]);
            
        }
        public override string ToString() // to display it correctly in comboBox
        {
            return $"{Name} {ip.ToString()}";
        }
    }
}
