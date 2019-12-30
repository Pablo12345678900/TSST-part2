using System;
using System.Collections.Generic;
using System.Net;
using Tools;

namespace Node
{
    class NodeProgram
    {
        
        private string nodeName { get; set; }
        private IPAddress ipAddress { get; set; }
        private IPAddress cloudIpAddress { get; set; }
        private IPAddress managerIpAddress { get; set; }

        static PackageHandler packageHandler;

        private short managerPort;
        private short cloudPort;

        NodeProgram()
        {
            packageHandler = new PackageHandler();
        }
        
            
        static void Main(string[] args)
        {
            Routing routing;
            try
            { 
                routing=Routing.createRouter(args[0]);
            }
            catch (Exception e)
            { 
                Console.WriteLine(e);
                throw;
            }
            routing.ActivateRouter();
            
        }

    }
}