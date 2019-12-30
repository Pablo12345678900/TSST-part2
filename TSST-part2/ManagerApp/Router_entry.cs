using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ManagerApp
{
    class Router_entry
    {
        public String Router_id;
        public Socket Router_connection;

        public Router_entry(String a, Socket b) {
            Router_id = a;
            Router_connection = b;
        }
    }


}
