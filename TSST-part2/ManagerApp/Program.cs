using System.Collections.Generic;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.Linq;
using Tools.Table_Entries;
using System.Globalization;

namespace ManagerApp
{
    class Program
    {
        public static Database config_DB = new Database();
        public static Router_Database Router_DB = new Router_Database();
        public static int ManagerPort;

        static void Main(string[] args)
        {
            //Manager port
            XmlSerializer serializer1 = new XmlSerializer(typeof(int));
            FileStream fs1 = new FileStream("port.txt", FileMode.Open);
            ManagerPort = (int)serializer1.Deserialize(fs1);

            //Uploading configuration data from a file
            XmlSerializer serializer = new XmlSerializer(typeof(Database));
            FileStream fs = new FileStream("configs.txt", FileMode.Open);
            config_DB = (Database)serializer.Deserialize(fs);

            Thread Thread1 = new Thread(new ThreadStart(AsynchronousSocketListener.StartListening));
            Thread Thread2 = new Thread(new ThreadStart(Management_system));
            Thread1.Start();
            Thread2.Start();


        }

        public static void Management_system()
        {
            Console.WriteLine(">> Welcome to the Management System for the emulated MPLS network. " +
                "This program is a part of a project created for the TSST course. " +
                "For more information about how to use this tool, input \'help\'."
                );
            while (true)
            {
                Console.WriteLine("Available commands: help, send, config, sendall");
                String option = Console.ReadLine();
                switch (option)
                {
                    case "send":
                        TUI_send();
                        break;
                    case "config":
                        Modify_config();
                        break;
                    case "help":
                        Display_help();
                        break;
                    case "sendall":
                        Sendall();
                        break;
                    default:
                        Console.WriteLine("Command not supported.");
                        break;
                }
                Console.WriteLine(">> You are now in the Management System's main menu.");
            }


        }
        public static void Modify_config()
        {
            Console.WriteLine(">> You are now in routers' configuration menu. You can view configurations or modify them.");
            while (true)
            {
                Console.WriteLine("Available configurations:\nID\tName\tDescription");
                int index = 1;
                foreach (R_config RC in config_DB.configs)
                {
                    Console.WriteLine(index + ".\t" + RC.R_name + "\t" + RC.Description);
                    index = index + 1;
                }
                Console.WriteLine("Please input the number (ID) of the configuration to view/update, " +
                    "\'n\' if you desire to create a new configuration, or \'q\' to quit:");
                String option = Console.ReadLine();
                if (option.Equals("q")) { break; }
                if (option.Equals("n")) 
                {
                    Console.WriteLine("Input the name of the new configuration:");
                    String name = Console.ReadLine();
                    Console.WriteLine("Input the description of the new configuration");
                    String desc = Console.ReadLine();
                    bool flag = false;
                    foreach(R_config RC in config_DB.configs)
                    {
                        if (RC.R_name.Equals(name)) { if (RC.Description.Equals(desc)) { flag = true; break; } }

                    }
                    if (flag) { Console.WriteLine("Combination of values already in use!"); break; }
                    else
                    {
                        R_config newconfig = new R_config();
                        newconfig.R_name = name;
                        newconfig.Description = desc;
                        config_DB.configs.Add(newconfig);
                        Console.WriteLine("Configuration created successfully." +
                            " To set values of the new config, update it in the config menu.");

                    }
                    
                    break; 
                }

                if (int.TryParse(option, out index) == false)
                {
                    Console.WriteLine("Input not supported.");
                    continue;
                }
                if (index > 0 && index <= config_DB.configs.Count())
                {
                    index = index - 1;
                }
                else
                {
                    Console.WriteLine("Input not supported.");
                    continue;
                }
                while (true)
                {
                    int index1 = 1;
                    Console.WriteLine("Selected configuration is: " + config_DB.configs[index].R_name + " / " + config_DB.configs[index].Description);
                    Console.WriteLine("Available configuration tables are:\n1.\tFEC\n2.\tFTN\n3.\tILM\n4.\tNHLFE\n5.\tFIB");
                    Console.WriteLine("Please input the number of the table to view / update or \'q\' to quit:");
                    option = Console.ReadLine();
                    if (option.Equals("q")) { break; }
                    switch (option)
                    {
                        case "1": //FEC
                            Console.WriteLine("Selected table is FEC.\nEntry ID \tDestination Address\tAssigned FEC");
                            foreach (FEC_Entry FC in config_DB.configs[index].FEC)
                            {
                                
                                Console.WriteLine(index1 + ".\t\t" + FC.destinationIP + "\t\t" + FC.FEC);
                                index1 = index1 + 1;
                            }
                            Console.WriteLine("Please input the number (ID) of the entry to update, \'n\' to create a new entry, or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            if (option.Equals("n")) {
                                FEC_Entry newFEC = new FEC_Entry();
                                Console.WriteLine("Input new destination address for this FEC entry or \'q\' to quit:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (IPAddress.TryParse(option, out IPAddress temp1))
                                    {
                                        newFEC.destinationIP = option;
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                Console.WriteLine("Input new FEC ID for this entry(must be unique):");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }

                                if (int.TryParse(option, out int temp))
                                {
                                    bool flag = true;
                                    foreach (FEC_Entry FC in config_DB.configs[index].FEC)
                                    {
                                        if (FC.FEC == int.Parse(option))
                                        { flag = false; }
                                    }
                                    if (flag)
                                    {
                                        newFEC.FEC = int.Parse(option);
                                    }
                                    else { Console.WriteLine("Value already in use!"); break; }
                                }

                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                
                                config_DB.configs[index].FEC.Add(newFEC);
                                Console.WriteLine("Entry added successfully!");

                                break; 
                            }

                            if (int.TryParse(option, out index1) == false)
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            if (index1 > 0 && index1 <= config_DB.configs[index].FEC.Count())
                            {
                                index1 = index1 - 1;
                            }
                            else
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            Console.WriteLine("Input new destination address for this FEC entry or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (IPAddress.TryParse(option, out IPAddress temp))
                                {
                                    config_DB.configs[index].FEC[index1].destinationIP = option;
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Input new FEC ID for this entry:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (int.TryParse(option, out int temp))
                                {
                                    config_DB.configs[index].FEC[index1].FEC = int.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Entry updated successfully!");

                            break;
                        case "2": //FTN
                            Console.WriteLine("Selected table is FTN.\nEntry ID \tFEC\tNHLFE");
                            foreach (FTN_Entry FC in config_DB.configs[index].FTN)
                            {

                                Console.WriteLine(index1 + ".  \t" + FC.FEC + "  \t" + FC.NHLFE_ID);
                                index1 = index1 + 1;
                            }
                            Console.WriteLine("Please input the number (ID) of the entry to update, \'n\' to create a new entry, or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            if (option.Equals("n"))
                            {
                                FTN_Entry newFEC = new FTN_Entry();
                                Console.WriteLine("Input FEC ID for this FTN entry or \'q\' to quit:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (int.TryParse(option, out int temp))
                                    {
                                        newFEC.FEC = int.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                Console.WriteLine("Input new NHLFE ID for this entry:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (int.TryParse(option, out int temp))
                                    {
                                        newFEC.NHLFE_ID = int.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                config_DB.configs[index].FTN.Add(newFEC);
                                Console.WriteLine("Entry added successfully!");

                                break;
                            }

                            if (int.TryParse(option, out index1) == false)
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            if (index1 > 0 && index1 <= config_DB.configs[index].FTN.Count())
                            {
                                index1 = index1 - 1;
                            }
                            else
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            Console.WriteLine("Input new FEC ID for this FTN entry or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (int.TryParse(option, out int temp))
                                {
                                    config_DB.configs[index].FTN[index1].FEC = int.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Input new NHLFE ID for this entry:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (int.TryParse(option, out int temp))
                                {
                                    config_DB.configs[index].FTN[index1].NHLFE_ID = int.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Entry updated successfully!");
                            break;


                        case "3": //ILM
                            Console.WriteLine("Selected table is ILM.\nEntry ID \tIngress Port\tIngress Label\tNHLFE ID");
                            foreach (ILM_Entry FC in config_DB.configs[index].ILM)
                            {

                                Console.WriteLine(index1 + ".\t\t" + FC.portIn + "\t\t" + FC.labelIn + "\t\t" + FC.NHLFE_ID);
                                index1 = index1 + 1;
                            }
                            Console.WriteLine("Please input the number (ID) of the entry to update, \'n\' to create a new entry, or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            if (option.Equals("n"))
                            {
                                ILM_Entry newFEC = new ILM_Entry();
                                Console.WriteLine("Input the ingress port for this ILM entry or \'q\' to quit:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (ushort.TryParse(option, out ushort temp))
                                    {
                                        newFEC.portIn = ushort.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                Console.WriteLine("Input new ingress label for this entry:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (ushort.TryParse(option, out ushort temp))
                                    {
                                        newFEC.labelIn = ushort.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                Console.WriteLine("Input new NHLFE ID for this entry:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (int.TryParse(option, out int temp))
                                    {
                                        newFEC.NHLFE_ID = int.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                config_DB.configs[index].ILM.Add(newFEC);
                                Console.WriteLine("Entry added successfully!");

                                break;
                            }

                            if (int.TryParse(option, out index1) == false)
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            if (index1 > 0 && index1 <= config_DB.configs[index].ILM.Count())
                            {
                                index1 = index1 - 1;
                            }
                            else
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            Console.WriteLine("Input new ingress port for this ILM entry or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (ushort.TryParse(option, out ushort temp))
                                {
                                    config_DB.configs[index].ILM[index1].portIn = ushort.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Input new ingress label for this entry:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (ushort.TryParse(option, out ushort temp))
                                {
                                    config_DB.configs[index].ILM[index1].labelIn = ushort.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Input new NHLFE ID for this entry:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (int.TryParse(option, out int temp))
                                {
                                    config_DB.configs[index].ILM[index1].NHLFE_ID = int.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }


                            Console.WriteLine("Entry updated successfully!");
                            break;
                        case "4": //NHLFE
                            Console.WriteLine("Selected table is NHLFE.\nEntry ID\tNHLFE_ID\tAction\tNumber of labels to pop\tEgress port\tLabels to push");
                            foreach (NHLFE_Entry FC in config_DB.configs[index].NHLFE)
                            {
                                String labels = "";
                                foreach (ushort label in FC.labelsOut) { labels = labels +" " +label; }
                                labels = labels + " (top)";

                                Console.WriteLine(index1 + ".\t\t" + FC.NHLFE_ID + "\t\t" + FC.action + "\t\t" + FC.popDepth+"\t\t"+FC.portOut 
                                    + "\t\t"+ labels);
                                index1 = index1 + 1;
                            }
                            Console.WriteLine("Please input the number (ID) of the entry to update, \'n\' to create a new entry, or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            if (option.Equals("n"))
                            {
                                NHLFE_Entry newFEC = new NHLFE_Entry();
                                Console.WriteLine("Input the NHLFE_id for this NHLFE entry or \'q\' to quit (must be unique):");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (int.TryParse(option, out int temp))
                                    {
                                        bool flag = true;
                                        foreach (NHLFE_Entry FC in config_DB.configs[index].NHLFE) { if (FC.NHLFE_ID == int.Parse(option))
                                            { flag = false; } }
                                        if (flag)
                                        {
                                            newFEC.NHLFE_ID = int.Parse(option);
                                        }
                                        else { Console.WriteLine("Value already in use!"); break; }
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                Console.WriteLine("Input \'1\' for action to be \'pop\', \'2\' for action to be \'push\' or \'3\' for \'swap\'," +
                                    " or \'q\' to quit:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    bool flag = false;
                                    switch (option)
                                    {
                                        case "1": newFEC.action = "pop"; break;
                                        case "2": newFEC.action = "push"; break;
                                        case "3": newFEC.action = "swap"; break;
                                        default: Console.WriteLine("Incorrect input!"); flag = true; break;
                                    }
                                    if (flag) { break; }
                                }
                                if (newFEC.action.Equals("pop"))
                                {
                                    Console.WriteLine("Input number of labels to pop for this entry or \'q\' to quit:");
                                    option = Console.ReadLine();
                                    if (option.Equals("q")) { break; }
                                    else
                                    {
                                        if (int.TryParse(option, out int temp))
                                        {
                                            newFEC.popDepth = int.Parse(option);
                                        }
                                        else { Console.WriteLine("Input could not be parsed."); break; }
                                    }
                                }
                                else { newFEC.popDepth=0;}
                                    Console.WriteLine("Input the egress port for this entry or \'q\' to quit:");
                                    option = Console.ReadLine();
                                    if (option.Equals("q")) { break; }
                                    else
                                    {
                                        if (ushort.TryParse(option, out ushort temp))
                                        {
                                            newFEC.portOut = ushort.Parse(option);
                                        }
                                        else { Console.WriteLine("Input could not be parsed."); break; }
                                    }

                                if (newFEC.action.Equals("push"))
                                {
                                    Console.WriteLine("Input labels to be pushed, press ENTER after each one," +
                                        "to stop inputing, write \'last\'. The last label provided will be the top one. Use \'q\' to quit.");
                                    bool flag = false;
                                    int counter = 0;
                                    while (true)
                                    {
                                        Console.WriteLine("Input one label:");
                                        option = Console.ReadLine();
                                        if (option.Equals("q")) { flag = true; break; }
                                        if (option.Equals("last") && counter > 0) { break; }
                                        if (ushort.TryParse(option, out ushort temp))
                                        {
                                            counter = counter + 1;
                                            ushort temp_label = ushort.Parse(option);
                                            newFEC.labelsOut.Add(temp_label);
                                        }
                                        else { Console.WriteLine("Input could not be parsed."); flag = true; break; }
                                    }
                                    if (flag) { break; }
                                }

                                if (newFEC.action.Equals("swap"))
                                {
                                    Console.WriteLine("Input a label to be swapped to. Use \'q\' should you want to quit.");
                                        Console.WriteLine("Input one label:");
                                        option = Console.ReadLine();
                                        if (option.Equals("q")) { break; }
                                        if (ushort.TryParse(option, out ushort temp))
                                        {
                                            ushort temp_label = ushort.Parse(option);
                                            newFEC.labelsOut.Add(temp_label);
                                        }
                                        else { Console.WriteLine("Input could not be parsed."); break; }
                                    }


                                config_DB.configs[index].NHLFE.Add(newFEC);
                                Console.WriteLine("Entry added successfully!");

                                break;
                            }

                            if (int.TryParse(option, out index1) == false)
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            if (index1 > 0 && index1 <= config_DB.configs[index].ILM.Count())
                            {
                                index1 = index1 - 1;
                            }
                            else
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            Console.WriteLine("Input the NHLFE_id for this NHLFE entry or \'q\' to quit (must be unique):");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (int.TryParse(option, out int temp))
                                {
                                    bool flag = true;
                                    foreach (NHLFE_Entry FC in config_DB.configs[index].NHLFE)
                                    {
                                        if (FC.NHLFE_ID == int.Parse(option))
                                        { flag = false; }
                                    }
                                    if (flag)
                                    {
                                        config_DB.configs[index].NHLFE[index1].NHLFE_ID = int.Parse(option);
                                    }
                                    else { Console.WriteLine("Value already in use!"); break; }
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Input \'1\' for action to be \'pop\' and \'2\' for action to be \'push\' or \'switch:\'," +
                                " or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                bool flag = false;
                                switch (option)
                                {
                                    case "1": config_DB.configs[index].NHLFE[index1].action = "pop"; break;
                                    case "2": config_DB.configs[index].NHLFE[index1].action = "push"; break;
                                    default: Console.WriteLine("Incorrect input!"); flag = true; break;
                                }
                                if (flag) { break; }
                            }
                            if (config_DB.configs[index].NHLFE[index1].action.Equals("pop"))
                            {
                                Console.WriteLine("Input number of labels to pop for this entry or \'q\' to quit:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (int.TryParse(option, out int temp))
                                    {
                                        config_DB.configs[index].NHLFE[index1].popDepth = int.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                            }
                            else { config_DB.configs[index].NHLFE[index1].popDepth = 0; }
                            Console.WriteLine("Input the egress port for this entry or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (ushort.TryParse(option, out ushort temp))
                                {
                                    config_DB.configs[index].NHLFE[index1].portOut = ushort.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }

                            if (config_DB.configs[index].NHLFE[index1].action.Equals("push"))
                            {
                                Console.WriteLine("Input labels to be pushed (one if switched), press ENTER after each one," +
                                    "to stop inputing, write \'last\'. The last label provided will be the top one. Use \'q\' to quit.");
                                bool flag = false;
                                int counter = 0;
                                while (true)
                                {
                                    Console.WriteLine("Input one label:");
                                    option = Console.ReadLine();
                                    if (option.Equals("q")) { flag = true; break; }
                                    if (option.Equals("last") && counter > 0) { break; }
                                    if (ushort.TryParse(option, out ushort temp))
                                    {
                                        counter = counter + 1;
                                        config_DB.configs[index].NHLFE[index1].labelsOut.Add(ushort.Parse(option));
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); flag = true; break; }
                                }
                                if (flag) { break; }
                            }
                            else { config_DB.configs[index].NHLFE[index1].labelsOut.Add(0); }

                            Console.WriteLine("Entry updated successfully!");


                            break;
                        case "5": //FIB
                            Console.WriteLine("Selected table is FIB.\nEntry ID \tDestination Address\tEgress port");
                            foreach (FIB_Entry FC in config_DB.configs[index].FIB)
                            {

                                Console.WriteLine(index1 + ".\t\t" + FC.destinationIP + "\t\t" + FC.portOut);
                                index1 = index1 + 1;
                            }
                            Console.WriteLine("Please input the number (ID) of the entry to update, \'n\' to create a new entry, or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            if (option.Equals("n"))
                            {
                                FIB_Entry newFEC = new FIB_Entry();
                                Console.WriteLine("Input new destination address for this FIB entry or \'q\' to quit:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (IPAddress.TryParse(option, out IPAddress temp))
                                    {
                                        newFEC.destinationIP = option;
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                Console.WriteLine("Input new egress port for this entry:");
                                option = Console.ReadLine();
                                if (option.Equals("q")) { break; }
                                else
                                {
                                    if (ushort.TryParse(option, out ushort temp))
                                    {
                                        newFEC.portOut = ushort.Parse(option);
                                    }
                                    else { Console.WriteLine("Input could not be parsed."); break; }
                                }
                                config_DB.configs[index].FIB.Add(newFEC);
                                Console.WriteLine("Entry added successfully!");

                                break;
                            }

                            if (int.TryParse(option, out index1) == false)
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            if (index1 > 0 && index1 <= config_DB.configs[index].FIB.Count())
                            {
                                index1 = index1 - 1;
                            }
                            else
                            {
                                Console.WriteLine("Input not supported.");
                                continue;
                            }
                            Console.WriteLine("Input new destination address for this FIB entry or \'q\' to quit:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (IPAddress.TryParse(option, out IPAddress temp))
                                {
                                    config_DB.configs[index].FIB[index1].destinationIP = option;
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Input new egress port for this entry:");
                            option = Console.ReadLine();
                            if (option.Equals("q")) { break; }
                            else
                            {
                                if (ushort.TryParse(option, out ushort temp))
                                {
                                    config_DB.configs[index].FIB[index1].portOut = ushort.Parse(option);
                                }
                                else { Console.WriteLine("Input could not be parsed."); break; }
                            }
                            Console.WriteLine("Entry updated successfully!");

                            break;
                        default:
                            Console.WriteLine("Input not supported.");
                            break;
                    }
                }

            }
        }
        public static void TUI_send()
        {
            Console.WriteLine(">> You are now in the sending menu.");
            while (true)
            {
                Console.WriteLine("Available network nodes:");
                foreach (Router_entry i in Router_DB.Routers)
                {
                    Console.WriteLine(i.Router_id);
                }
                Console.WriteLine("Input the node to communicate with or exit using \'q\':");
                String option = Console.ReadLine();
                if (option.Equals("q")) { return; }
                int flag = 0;
                int selected_router = -1;
                for (int i = 0; i < Router_DB.Routers.Count(); i++)
                {
                    selected_router = i;
                    if (Router_DB.Routers[i].Router_id.Equals(option)) { flag = 1; break; }
                }
                if (flag == 0) { Console.WriteLine("No entry found for: " + option); continue; }

                while (true)
                {
                    Console.WriteLine("Available configurations:\nID\tName\tDescription");
                    int index = 1;
                    foreach (R_config RC in config_DB.configs)
                    {
                        Console.WriteLine(index + ".\t" + RC.R_name + "\t" + RC.Description);
                        index = index + 1;
                    }
                    Console.WriteLine("Please input the number (ID) of the configuration to send or \'q\' to quit:");
                    option = Console.ReadLine();
                    if (option.Equals("q")) 
                    { break; }
                    if (int.TryParse(option, out index) == false)
                    {
                        Console.WriteLine("Input not supported.");
                        continue;
                    }
                    if (index > 0 && index <= config_DB.configs.Count())
                    {
                        index = index - 1;
                    }
                    else
                    {
                        Console.WriteLine("Input not supported.");
                        continue;
                    }

                     Send_config(selected_router, index);
                    break;

                }
            }
        }

        public static void Sendall()
        {
            Console.WriteLine(">> You are now in the mass sending menu.");
            Console.WriteLine("Input the description to send to all nodes with a described " +
                "configuration or exit using \'q\':");
            String option = Console.ReadLine();
            if (option.Equals("q")) { return; }


            for (int i = 0; i < config_DB.configs.Count(); i++)
            {
                if (config_DB.configs[i].Description.Equals(option))
                {
                    for (int k = 0; k < Router_DB.Routers.Count(); k++)
                    {
                        if (Router_DB.Routers[k].Router_id.Equals(config_DB.configs[i].R_name))
                        {
                            Send_config(k, i);
                        }
                    }

                }
            }
        }

        public static void Send_config(int Router_index, int Config_index)
        {

            try
            {
 
                try
                {

                    Socket sender = Router_DB.Routers[Router_index].Router_connection;
                    XmlSerializer serializer = new XmlSerializer(typeof(R_config));
                    StringWriter textWriter = new StringWriter();

                    serializer.Serialize(textWriter, config_DB.configs[Config_index]);
                    String content = textWriter.ToString();
 
                    byte[] msg = Encoding.ASCII.GetBytes(content);
  
                   int bytesSent = sender.Send(msg);

                    Console.WriteLine("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] "
                                            + "Configuration sent to " + Router_DB.Routers[Router_index].Router_id);
 

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static void Display_help()
        {
            Console.WriteLine("The system should provide all information about actions needed to be taken " +
                "in order to be able to use it successfully. From the main menu you can use \'config\' to view and " +
                "update configurations of routers, and \'send\' to send an updated or different configuration to a specific" +
                " router that exists in the database - in order to be in the database, the router needs to call in first. When a router " +
                "calls in for the first time, a default configuration is sent as a response.");
        }


        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
        }

        public class AsynchronousSocketListener
        {
            public static ManualResetEvent allDone = new ManualResetEvent(false);

            public AsynchronousSocketListener()
            {
            }

            public static void StartListening()
            {

                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), ManagerPort);

                Socket listener = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (true)
                    {

                        allDone.Reset();


                        listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            listener);

                        allDone.WaitOne();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("\nPress ENTER to continue...");
                Console.Read();

            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                allDone.Set();

                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }

            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));


                    content = state.sb.ToString();

                    Console.WriteLine("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] "
                                            + content + " called in.");



                    Router_entry RE = new Router_entry(content, handler);
                    Router_DB.Routers.Add(RE);

                    XmlSerializer serializer = new XmlSerializer(typeof(R_config));
                    StringWriter textWriter = new StringWriter();

                    int index = -1;

                    for (int i = 0; i < config_DB.configs.Count(); i++)
                    {
                        index = i;
                        if (content == config_DB.configs[i].R_name && config_DB.configs[i].Description == "default")
                        {
                            break;
                        }
                    }
                    String name_r = "";
                    if (index != -1)
                    {
                        name_r = content;
                        serializer.Serialize(textWriter, config_DB.configs[index]);
                        content = textWriter.ToString();
                        Send(handler, content);

                        Console.WriteLine("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                            CultureInfo.InvariantCulture) + "] "
                            + "Default configuration sent to " + name_r);
                    }
                }
            }

            private static void Send(Socket handler, String data)
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    Socket handler = (Socket)ar.AsyncState;

                    int bytesSent = handler.EndSend(ar);


                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}

