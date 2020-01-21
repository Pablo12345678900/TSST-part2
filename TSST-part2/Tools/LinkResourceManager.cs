using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Tools
{
   public class LinkResourceManager
   {
        public ushort port { get; set; }
        public bool[] slots { get; set; }
        public IPAddress IPofNode { get; set; }
        public LinkResourceManager(ushort port)
        {
            this.port = port;
            slots = new bool[10]; // 10 available slots
            for(int i=0;i<slots.Length;i++)
            {
                slots[i] = true; // at the start, each of slot is available
            }
        }
        public LinkResourceManager()
        {
            slots = new bool[10];
        }

        public byte[] convertToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(port));
          

            for(int i=0; i<slots.Length;i++)
            {
                bytes.AddRange(BitConverter.GetBytes(slots[i]));
            }
            bytes.AddRange(IPofNode.GetAddressBytes());
            return bytes.ToArray();
        }
        public static LinkResourceManager returnLRM(byte[] bytes) // 16 bajtów=1 LRM
        {

            LinkResourceManager link = new LinkResourceManager();
            link.port = (ushort)((bytes[1] << 8) + bytes[0]);
          
            for(int i=0;i<10;i++)
            {
                link.slots[i] = BitConverter.ToBoolean(bytes, i+2);
                
            }
            link.IPofNode= new IPAddress(new byte[] { bytes[12], bytes[13], bytes[14], bytes[15] });
            Console.WriteLine(link.IPofNode);
            return link;
        }
   }
    
}
