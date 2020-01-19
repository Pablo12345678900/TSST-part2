using System;
using System.Collections.Generic;
using System.Net;
using Tools;

namespace Node
{
    class NodeProgram
    {
            
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