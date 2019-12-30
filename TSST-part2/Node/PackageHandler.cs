using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tools;

namespace Node
{
    public class PackageHandler
    {
        public List<Optical_Entry> Optical_Table { get; set; }
     

        public PackageHandler()
        {
            Optical_Table = new List<Optical_Entry>();
         
        }

        public void handlePackage(DataStream dataStream)
        {

           Optical_Entry optical=FindOpticalEntry(dataStream);
            dataStream.currentPort = optical.outPort;
            
        }
        public Optical_Entry FromBytesToEntry(byte[] bytes)
        {
            Optical_Entry optical = new Optical_Entry();
            optical.inPort = (ushort)((bytes[1] << 8) + bytes[0]);
            optical.startSlot = BitConverter.ToUInt32(bytes, 2);
            optical.lastSlot = BitConverter.ToUInt32(bytes, 6);
            optical.outPort = (ushort)((bytes[11] << 8) + bytes[10]);
            return optical;
        }

        public Optical_Entry FindOpticalEntry(DataStream dataStream)
        {
            Optical_Entry optEntry = null;
            foreach(Optical_Entry item in Optical_Table)
            {
                if(dataStream.currentPort==item.inPort && dataStream.firstFrequencySlot==item.startSlot && dataStream.lastFrequencySlot==item.lastSlot)
                {
                    optEntry = item;
                    break;
                }
            }
             return optEntry;
        }
        
        public void displayTables()
        {    

            Console.WriteLine("\nMy tables:");
            
            Console.WriteLine("NHLFE_Table:");
            
            
            Console.WriteLine("");
        }
    }
}