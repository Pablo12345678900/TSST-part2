using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
namespace Tools
{
    public class DataStream
    {
        public IPAddress sourceHost { get; set; } 
        public IPAddress destinationHost { get; set; } 
        public IPAddress currentNode { get; set; }
        public ushort currentPort { get; set; }
        public string payload { get; set; }
        
        public int firstFrequencySlot { get; set; } // these slots will be given by host ( if requested path will be free)
        public int lastFrequencySlot { get; set; }

        public int modulation { get; set; }
        public uint streamLength { get; set; }

        /*po analizie stwierdziłem, że nie są potrzebne informacje o destination host  w strumieniu skoro w tablicach jest info tylko o portach wej/wyj i slotach  forwardujemy na podstawie
         * szczelin*/
        public DataStream()
        {

        }

        public byte[] toBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(sourceHost.GetAddressBytes());
            bytes.AddRange(destinationHost.GetAddressBytes());
            bytes.AddRange(currentNode.GetAddressBytes());
            bytes.AddRange(BitConverter.GetBytes(currentPort));
            bytes.AddRange(BitConverter.GetBytes(firstFrequencySlot));
            bytes.AddRange(BitConverter.GetBytes(lastFrequencySlot));
            streamLength = (uint)(22 + payload.Length);
            bytes.AddRange(BitConverter.GetBytes(streamLength));
            bytes.AddRange(Encoding.ASCII.GetBytes(payload ?? ""));
            return bytes.ToArray();
        }
        public static DataStream toData(byte[] bytes)
        {
            DataStream dataStream = new DataStream();
            dataStream.sourceHost = new IPAddress(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3] });
            dataStream.destinationHost = new IPAddress(new byte[] { bytes[4], bytes[5], bytes[6], bytes[7] });
            dataStream.currentNode = new IPAddress(new byte[] { bytes[8], bytes[9], bytes[10], bytes[11] });
            dataStream.currentPort = (ushort)((bytes[13] << 8) + bytes[12]);
            dataStream.firstFrequencySlot = BitConverter.ToInt32(bytes, 14);
            dataStream.lastFrequencySlot = BitConverter.ToInt32(bytes, 18);
            dataStream.streamLength = BitConverter.ToUInt32(bytes, 22);
            dataStream.payload = Encoding.ASCII.GetString(bytes.ToList().GetRange(26, (int)(dataStream.streamLength - 26)).ToArray());


            return dataStream;
        }
    }
}