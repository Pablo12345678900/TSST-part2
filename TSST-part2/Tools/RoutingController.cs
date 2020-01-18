using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace Tools
{
    public class RoutingController
    {
        public List<Cable> cables = new List<Cable>();

        public List<LinkResourceManager> lrms = new List<LinkResourceManager>();
        public List<IPAddress> nodesToAlgorithm = new List<IPAddress>();
        public List<LinkResourceManager> links = new List<LinkResourceManager>();

        public RoutingController()
        {

        }
        public void readTopology(String conFile)
        {

        }

        /////////////////////////////////
        private class NetworkNode
        {
            public IPAddress ipadd;
            public int distance;
            public bool visited;
            public NetworkNode predecessor;
            public List<Edge> adjacentEdges;
            public int[] slottable;
            public NetworkNode(IPAddress ipaddr)
            {
                ipadd = ipaddr;
                distance = int.MaxValue;
                visited = false;
                predecessor = null;
                adjacentEdges = new List<Edge>();
                slottable = new int[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            }
            public void addedge(Edge e)
            {
                adjacentEdges.Add(e);
            }
        }

        private class Edge
        {
            public NetworkNode NodeA;
            public NetworkNode NobeB;
            public bool[] slots;
            public int length;
            public Edge(NetworkNode N1, NetworkNode N2, bool[] slots, int len)
            {
                NodeA = N1;
                NobeB = N2;
                this.slots = slots;
                length = len;
            }
        }

        public class RoutingResult
        {
            public List<IPAddress> Path;
            public bool[] slots;

            public RoutingResult()
            {
                slots = new bool[10];
                Path = new List<IPAddress>();

            }
            public void addToPath(IPAddress ad)
            {
                Path.Add(ad);
            }
        }


        public RoutingResult DijkstraAlgorithm1(IPAddress source, IPAddress destination, List<Cable> cables, List<LinkResourceManager> links, int requiredSpeed, int numOfSlots, List<IPAddress> Q, List<IPAddress> S)
        {
            //building the graph
            List<NetworkNode> Nodes = new List<NetworkNode>();
            List<Edge> Edges = new List<Edge>();
            foreach (IPAddress node in nodesToAlgorithm)
            {
                NetworkNode Node1 = new NetworkNode(node);
                Nodes.Add(Node1);
            }
            foreach (Cable c in cables)
            {

                bool[] s = new bool[10];
                NetworkNode n1 = new NetworkNode(IPAddress.Parse("0.0.0.0"));
                NetworkNode n2 = new NetworkNode(IPAddress.Parse("0.0.0.0"));
                foreach (LinkResourceManager lrm in lrms)
                {
                    if (c.Node1.Equals(lrm.IPofNode) || c.Node2.Equals(lrm.IPofNode))
                    {
                        if (c.port1.Equals(lrm.port) || c.port2.Equals(lrm.port))
                        {
                            s = lrm.slots;
                            break;
                        }
                    }
                }
                foreach (NetworkNode n in Nodes)
                {
                    int counter = 0;
                    if (n.ipadd.Equals(c.Node1) || n.ipadd.Equals(c.Node2))
                    {
                        if (counter == 0)
                        {
                            counter++;
                            n1 = n;
                        }
                        if (counter == 1)
                        {
                            n2 = n;
                            break;
                        }
                    }
                }
                Edge e = new Edge(n1, n2, s, c.length);
            }
            foreach (NetworkNode n in Nodes) {
                foreach (Edge e in Edges)
                {
                    if (n.ipadd.Equals(e.NodeA) || n.ipadd.Equals(e.NobeB)) { n.addedge(e); }
                }
            }
            //graph is built

            //tworze result bo inaczej ma error ze nic nie zwraca jak jest w bloku
            RoutingResult result = new RoutingResult();


            //determine the number of slots required
            int estimated_length = 200; //wczytac trzeba z pliku
            int modulation = 0;
            if (estimated_length >= 0 && estimated_length <= 100) modulation = 64;
            else if (estimated_length > 100 && estimated_length <= 200) modulation = 32;
            else if (estimated_length > 200 && estimated_length <= 300) modulation = 16;
            else if (estimated_length > 300 && estimated_length <= 400) modulation = 8;
            else if (estimated_length > 400 && estimated_length <= 500) modulation = 4;
            else if (estimated_length > 500) modulation = 2;

            double usedFrequency = ((requiredSpeed * 2) / (Math.Log(modulation, 2))) + 10;
            int slots_required = (int)Math.Ceiling(usedFrequency / 12.5);

            bool flag = true;


            while (flag)
            {

                //finding the source node
                int index = -1;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    if (Nodes[i].Equals(source))
                    {
                        Nodes[i].distance = 0;
                        index = i;
                        break;
                    }
                }



                //kolejka
                List<NetworkNode> NetworkQueue = new List<NetworkNode>();
                NetworkQueue.Add(Nodes[index]);

                //chodzenie po grafie
                while (NetworkQueue.Count > 0)
                {
                    //popping the element
                    NetworkNode current_node = NetworkQueue[0];
                    NetworkQueue.RemoveAt(0);

                    //inspecting every neighbor
                    foreach (Edge e in current_node.adjacentEdges)
                    {
                        NetworkNode n = new NetworkNode(IPAddress.Parse("0.0.0.0"));
                        if (e.NodeA.Equals(current_node))
                        {
                            n = e.NobeB;
                        }
                        else { n = e.NodeA; }

                        if (!n.visited)
                        {
                            //inspecting available slots
                            int[] slots_available = new int[10];
                            for (int i = 0; i < 10; i++)
                            {
                                if (e.slots[i]) { slots_available[i] = 1; }
                                else { slots_available[i] = 0; }
                            }
                            //taking into account previous links
                            for (int i = 0; i < 10; i++)
                            {
                                slots_available[i] = slots_available[i] * current_node.slottable[i];
                            }
                            //calculating if we have enough slots
                            int slots_to_use = 0; //highest number of consecutive slots found so far
                            int temp_slots = 0; //current number fo consecutive slots available
                            bool reset = false;
                            for (int i = 0; i < 10; i++)
                            {
                                if (slots_available[i] == 1 && !reset) { temp_slots++; }
                                if (slots_available[i] == 1 && reset) { temp_slots = 1; reset = false; }
                                if (slots_available[i] == 0) { reset = true; }

                                if (temp_slots > slots_to_use) { slots_to_use = temp_slots; }
                            }
                            //if we have enough slots then proceed
                            if (slots_to_use >= slots_required)
                            {
                                if (current_node.distance + e.length < n.distance)
                                {
                                    n.distance = current_node.distance + e.length;
                                    n.predecessor = current_node;
                                    NetworkQueue.Add(n);
                                    n.slottable = slots_available;
                                }
                            }

                        }
                        current_node.visited = true;
                    }

                }
                //finding the target/destination and slots to use to provide the result
                List<IPAddress> ReversedPath = new List<IPAddress>();
                for (int i = 0; i < Nodes.Count; i++)
                {
                    if (Nodes[i].Equals(destination))
                    {
                        index = i;
                        break;
                    }
                }
                NetworkNode temp_node = Nodes[index];

                if (temp_node.predecessor.Equals(null))
                {
                    //zwraca pusty wynik kiedy nie doszedł algorytm do konca - nie mozna wyznaczyc sciezki
                    return result;
                }

                //total length as a path metric, needed to verify if current slots will suffice
                int total_length = temp_node.distance;

                modulation = 0;
                if (total_length >= 0 && total_length <= 100) modulation = 64;
                else if (total_length > 100 && total_length <= 200) modulation = 32;
                else if (total_length > 200 && total_length <= 300) modulation = 16;
                else if (total_length > 300 && total_length <= 400) modulation = 8;
                else if (total_length > 400 && total_length <= 500) modulation = 4;
                else if (total_length > 500) modulation = 2;

                usedFrequency = ((requiredSpeed * 2) / (Math.Log(modulation, 2))) + 10;
                int slots_for_path = (int)Math.Ceiling(usedFrequency / 12.5);

                if (slots_for_path <= slots_required) { flag = false; }
                else { continue; }


                //creating the result if slot numbers agree
                int[] slot_table = new int[10];
                slot_table = temp_node.slottable;

                ReversedPath.Add(temp_node.ipadd);
                while (!temp_node.predecessor.Equals(null))
                {
                    temp_node = temp_node.predecessor;
                    ReversedPath.Add(temp_node.ipadd);

                }
 
                for (int i = 0; i < ReversedPath.Count; i++)
                {
                    result.Path.Add(ReversedPath[ReversedPath.Count - 1 - i]);
                }
                for (int i = 0; i < 10; i++)
                {
                    if (slot_table[i] == 1)
                    {
                        result.slots[i] = true;
                    }
                    else { result.slots[i] = false; }
                 }

            }

            return result;
        }

    }
}

