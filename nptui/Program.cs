using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Linq;
using System.ComponentModel;

namespace NPTUI
{
    class NPTUI
    {
        public static string nptui_version = "v1.5";
        public static string nptui_date = "03-06-25";
        public static List<Ethernet> ethernets = new List<Ethernet>();
        public static string netplanPath = "";
        public static void Main(string[] args)
        {
            Console.Clear();
            netplanPath = "";
            if (args.Length > 0) netplanPath = args[0];
            else netplanPath = "/etc/netplan/25-nptui.yaml";
            if (netplanPath != "")
            {
                if (File.Exists(netplanPath))
                {
                    if (netplanPath.StartsWith('/')) Load(netplanPath);
                    else { Console.WriteLine("NPTUI requires an absolute file path, not relative (your path must start with a /). Press ENTER to continue"); netplanPath = ""; Console.ReadLine(); }
                }
                else
                {
                    Console.WriteLine($"No such file {netplanPath}. Would you like to create one? [Y/n]");
                    if (netplanPath == "/etc/netplan/25-nptui.yaml" || Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        try
                        {
                            File.WriteAllText(netplanPath, "network:\n  version: 2");
                            UnixFileMode permissions = UnixFileMode.UserRead | UnixFileMode.UserWrite |
                            UnixFileMode.GroupRead |
                            UnixFileMode.OtherRead;

                            File.SetUnixFileMode(netplanPath, permissions);
                            Load(netplanPath);
                        }
                        catch
                        {
                            Console.WriteLine("An error occurred creating the file. Try sudo?");
                            Console.WriteLine("Press ENTER to continue");
                            Console.ReadLine();
                        }
                    }
                    else netplanPath = "";
                }
            }
            else netplanPath = "";


            MainMenu();

        }

        public static void MainMenu()
        {
            int selected_item = 0;
            string[] menu_options = ["Edit Interfaces", "Load Netplan File", "About NPTUI", "Preview and Save", "Exit"];
            while (true)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("\n");
                Console.WriteLine($"    NPTUI [{nptui_version}] (netplanPath: {netplanPath})");
                Console.WriteLine($"");
                for (int i = 0; i < menu_options.Length; i++)
                {
                    if (i == selected_item)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    } else {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine($"    {i + 1}. {menu_options[i].PadRight(32)}");
                }

                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        selected_item -= 1;
                        if (selected_item < 0) selected_item = menu_options.Length - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selected_item += 1;
                        if (selected_item >= menu_options.Length) selected_item = 0;
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        switch (selected_item)
                        {
                            case 0:
                                InterfacesMenu();
                                break;
                            case 1:
                                Console.Clear();
                                Console.Write("Enter path: ");
                                netplanPath = Console.ReadLine();
                                try
                                {
                                    if (File.Exists(netplanPath))
                                    {
                                        if (netplanPath.StartsWith('/')) Load(netplanPath);
                                        else { Console.WriteLine("NPTUI requires an absolute file path, not relative (your path must start with a /). Press ENTER to continue"); netplanPath = ""; Console.ReadLine(); }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"No such file {netplanPath}. Would you like to create one? [Y/n]");
                                        if (Console.ReadKey().Key == ConsoleKey.Y)
                                        {
                                            try
                                            {
                                                File.WriteAllText(netplanPath, "network:\n  version: 2");
                                                UnixFileMode permissions = UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                                UnixFileMode.GroupRead |
                                                UnixFileMode.OtherRead;

                                                File.SetUnixFileMode(netplanPath, permissions);
                                                Load(netplanPath);
                                            }
                                            catch
                                            {
                                                Console.WriteLine("An error occurred creating the file. Try sudo?");
                                                Console.WriteLine("Press ENTER to continue");
                                                Console.ReadLine();
                                            }
                                        }
                                        else netplanPath = "";
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("An error occurred trying to do that. Try sudo?");
                                    Console.ReadLine();
                                }
                                break;
                            case 2:
                                Console.Clear();
                                Console.WriteLine($"NetPlan Terminal User Interface (NPTUI)\n{nptui_version} | {nptui_date} | By Simmo <3\nhttps://github.com/simmotipo/nptui");
                                Console.WriteLine("Press ENTER to continue");
                                Console.ReadLine();
                                break;
                            case 3:
                                Console.Clear();
                                Console.WriteLine($"\n   ==== Preview ({netplanPath}) ".PadRight(64, '='));
                                Save(ethernets.ToArray(), netplanPath, previewOnly: true);
                                Console.WriteLine("\n   ".PadRight(64, '=') + "\n");
                                bool waitingForConfirmation = true;
                                bool commitToSave = false;
                                while (waitingForConfirmation)
                                {
                                    Console.Write("Are you sure you want to save this netplan data? [Y/n] ");
                                    string response = Console.ReadLine().Replace(" ", "");
                                    if (response.ToLower() == "n") { commitToSave = false; waitingForConfirmation = false; }
                                    else if (response == "Y") { commitToSave = true; waitingForConfirmation = false; }
                                }
                                if (commitToSave) {
                                    Save(ethernets.ToArray(), netplanPath, previewOnly: false);
                                    Console.WriteLine($"Saved netplan config to {netplanPath}\n Remember to SUDO NETPLAN APPLY after exiting NPTUI! Press ENTER to continue");
                                } else {
                                    Console.WriteLine($"Config NOT saved.\nPress ENTER to continue");
                                }
                                Console.ReadLine();
                                break;
                            case 4:
                                Console.BackgroundColor = ConsoleColor.Black;
                                Console.ForegroundColor = ConsoleColor.White;
                                Environment.Exit(0);
                                break;
                        }
                        break;
                }
            }
        }

        public static void InterfacesMenu()
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    foreach (Ethernet ethernet in ethernets) menuOptionsList.Add(ethernet.name);
                    menuOptionsList.Add("");
                    menuOptionsList.Add("+ Add Interface");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("\n");
                Console.WriteLine($"    NPTUI [{nptui_version}] (netplanPath: {netplanPath})");
                Console.WriteLine($"");
                for (int i = 0; i < menu_options.Length; i++)
                {
                    if (i == selected_item)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine($"    {i + 1}. {menu_options[i].PadRight(32)}");
                }
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        selected_item -= 1;
                        if (selected_item < 0) selected_item = menu_options.Length - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selected_item += 1;
                        if (selected_item >= menu_options.Length) selected_item = 0;
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) return;
                        else if (menu_options[selected_item].Contains("Add Interface"))
                        {
                            AddInterfaceMenu();
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item] != "")
                        {
                            foreach (Ethernet e in ethernets) if (e.name == menu_options[selected_item]) { EditInterface(e); break; }
                            refreshMenuOptions = true;
                        }
                        break;
                }
            }
        }

        public static void AddInterfaceMenu()
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        bool can_add = true;
                        foreach (Ethernet ethernet in ethernets) if (ethernet.name == ni.Name) { can_add = false; break; } // I know this is inefficient; I'll come back to this.
                        if (can_add) menuOptionsList.Add(ni.Name);
                    }
                    menuOptionsList.Add("");
                    menuOptionsList.Add("+ Custom / Other Interface");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("\n");
                Console.WriteLine($"    NPTUI [{nptui_version}] (netplanPath: {netplanPath})");
                Console.WriteLine($"");
                for (int i = 0; i < menu_options.Length; i++)
                {
                    if (i == selected_item)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine($"    {i + 1}. {menu_options[i].PadRight(32)}");
                }
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        selected_item -= 1;
                        if (selected_item < 0) selected_item = menu_options.Length - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selected_item += 1;
                        if (selected_item >= menu_options.Length) selected_item = 0;
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) return;
                        else if (menu_options[selected_item].Contains("Custom / Other Interface"))
                        {
                            Console.Clear();
                            Console.Write($"Enter interface name: ");
                            string new_name = Console.ReadLine().Replace(" ", "");
                            ethernets.Add(new Ethernet($"    {new_name}\n      dhcp4: true"));
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item] != "")
                        {
                            ethernets.Add(new Ethernet($"    {menu_options[selected_item]}\n      dhcp4: true"));
                            refreshMenuOptions = true;
                        }
                        break;
                }
            }
        }

        public static void EditInterface(Ethernet e)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add($"Name                  | {e.name}".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Config ".PadRight(64, '-'));
                    menuOptionsList.Add($"DHCP                  | {e.dhcp4}".PadRight(64));
                    if (e.dhcp4 == "no")
                    {
                        bool found_gateway = false;
                        foreach (string route in e.routes)
                        {
                            if (route.Split("%")[0] == "default")
                            {
                                menuOptionsList.Add($"Gateway               | {route.Split("%")[1]}".PadRight(64));
                                if (route.Split("%")[2] != "-1") menuOptionsList.Add($"---- Metric           | {route.Split("%")[2]}".PadRight(64));
                                found_gateway = true;
                                break;
                            }
                        }
                        if (!found_gateway)
                        {
                            menuOptionsList.Add($"+ Add Gateway".PadRight(64));
                        }
                        for (int i = 0; i < e.addresses.Count(); i++) menuOptionsList.Add($"Address {i + 1}             | {e.addresses[i]}".PadRight(64));
                        menuOptionsList.Add($"+ Add Address         ".PadRight(64));
                    }
                    int route_count = 0;
                    foreach (string route in e.routes) if (!route.Contains("default")) route_count += 1;
                        menuOptionsList.Add("");
                    for (int i = 0; i < e.nameservers.Count(); i++) menuOptionsList.Add($"Nameserver {i + 1}          | {e.nameservers[i]}".PadRight(64));
                    menuOptionsList.Add($"+ Add Nameserver      ".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Routing ".PadRight(64, '-'));
                    menuOptionsList.Add($"IPv4 Routes          | {route_count} custom route(s)".PadRight(64));
                    menuOptionsList.Add("");
                    menuOptionsList.Add("- Remove Interface");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("\n");
                Console.WriteLine($"    NPTUI [{nptui_version}] (netplanPath: {netplanPath})");
                Console.WriteLine($"");
                for (int i = 0; i < menu_options.Length; i++)
                {
                    if (i == selected_item)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine($"    {i + 1}. {menu_options[i].PadRight(32)}");
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n    Press 'e', ENTER, or SPACE to Edit/Select entry.");
                Console.WriteLine($"    Press 'x'. to delete entry.");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        selected_item -= 1;
                        if (selected_item < 0) selected_item = menu_options.Length - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selected_item += 1;
                        if (selected_item >= menu_options.Length) selected_item = 0;
                        break;
                    case ConsoleKey.X:
                        if (menu_options[selected_item].Contains("Address "))
                        {
                            e.addresses.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Nameserver "))
                        {
                            e.nameservers.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Gateway "))
                        {
                            foreach (string route in e.routes)
                            {
                                if (route.Contains("default")) { e.routes.Remove(route); break; }
                            }
                            refreshMenuOptions = true;
                        }
                        break;
                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) return;
                        else if (menu_options[selected_item].Contains("DHCP"))
                        {
                            if (e.dhcp4 == "yes") e.dhcp4 = "no";
                            else e.dhcp4 = "yes";
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Name "))
                        {
                            Console.Write($"Provide new name [{e.name}]");
                            string resp = Console.ReadLine();
                            if (resp != "") { e.name = resp.Replace(" ", ""); refreshMenuOptions = true; }
                        }
                        else if (menu_options[selected_item].Contains("IPv4 Routes"))
                        {
                            EditRoutes(e);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Add Address"))
                        {
                            Console.Write("Provide new IP [x.x.x.x/xx] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0) { e.addresses.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Add Nameserver"))
                        {
                            Console.Write("Provide new IP [x.x.x.x] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4) { e.nameservers.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Remove Interface"))
                        {
                            Console.Clear();
                            Console.Write($"Are you sure you wish to remove this interface? [Y/n]: ");
                            if (Console.ReadKey().Key == ConsoleKey.Y)
                            {
                                ethernets.Remove(e);
                                return;
                            }
                        }
                        else if (menu_options[selected_item].Contains("Address "))
                        {
                            Console.Write($"Enter new address [{menu_options[selected_item].Split("| ")[1].Split(' ')[0]}] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Replace(" ", "") != "")
                                {
                                    if (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0)
                                    {
                                        e.addresses.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                                        e.addresses.Add(resp);
                                        refreshMenuOptions = true;
                                    }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                                }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Nameserver "))
                        {
                            Console.Write($"Enter new nameserver [{menu_options[selected_item].Split("| ")[1].Split(' ')[0]}] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Replace(" ", "") != "")
                                {
                                    if (resp.Split('.').Length == 4)
                                    {
                                        e.nameservers.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                                        e.nameservers.Add(resp);
                                        refreshMenuOptions = true;
                                    }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                                }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Gateway "))
                        {
                            string current_gateway = "";
                            if (menu_options[selected_item].Contains("|")) current_gateway = menu_options[selected_item].Split("| ")[1].Split(' ')[0];
                            Console.Write($"Enter new gateway [{current_gateway}] ");
                            string resp = Console.ReadLine();
                            if (resp == "") resp = current_gateway;
                            try
                            {
                                if (resp.Replace(" ", "") != "" && resp.Split('.').Length == 4)
                                {
                                    string current_metric = "";
                                    if (!menu_options[selected_item].Contains("Add"))
                                    {
                                        foreach (string route in e.routes)
                                        {
                                            if (route.Contains("default"))
                                            {
                                                current_metric = route.Split("%")[2];
                                                break;
                                            }
                                        }
                                    }
                                    
                                    Console.Write($"Provide a gateway metric? Enter -1 for no metric [{current_metric}]");
                                    string metric_resp = Console.ReadLine().Replace(" ", "");
                                    if (metric_resp == "") metric_resp = current_metric;
                                    if (metric_resp == "") metric_resp = "-1";
                                    try
                                    {
                                        if (Convert.ToInt32(metric_resp) < -2) { Console.WriteLine("Invalid metric. Press ENTER to continue"); Console.ReadLine(); break; }
                                    }
                                    catch { Console.WriteLine("Invalid metric."); Console.ReadLine(); break; }
                                    if (menu_options[selected_item].Contains("Add"))
                                    {
                                        e.routes.Add($"default%{resp}%{metric_resp}");
                                    }
                                    else
                                    {
                                        foreach (string route in e.routes)
                                        {
                                            if (route.Contains("default"))
                                            {
                                                e.routes.Remove(route);
                                                break;
                                            }
                                        }
                                        e.routes.Add($"default%{resp}%{metric_resp}");
                                    }
                                    refreshMenuOptions = true;
                                }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }

                        }
                        break;
                }
            }
        }

        public static void EditRoutes(Ethernet e)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add("Destination      | via Next Hop         | Metric (-1 = no metric)");
                    menuOptionsList.Add("".PadRight(48,'-'));
                    foreach (string route in e.routes) menuOptionsList.Add($"{route.Split("%")[0].PadRight(16)} | via {route.Split("%")[1].PadRight(16)} | (metric {route.Split("%")[2]})".PadRight(48));
                    menuOptionsList.Add("");
                    menuOptionsList.Add("+ Add Route");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("\n");
                Console.WriteLine($"    NPTUI [{nptui_version}] (netplanPath: {netplanPath})");
                Console.WriteLine($"");
                for (int i = 0; i < menu_options.Length; i++)
                {
                    if (i == selected_item)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine($"    {i + 1}. {menu_options[i].PadRight(32)}");
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n    Press 'e', ENTER, or SPACE to Edit/Select entry.");
                Console.WriteLine($"    Press 'x'. to delete entry.");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        selected_item -= 1;
                        if (selected_item < 0) selected_item = menu_options.Length - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selected_item += 1;
                        if (selected_item >= menu_options.Length) selected_item = 0;
                        break;
                    case ConsoleKey.X:
                        try
                        {
                            e.routes.Remove(ConvertMenuItemToRoute(menu_options[selected_item]));
                            refreshMenuOptions = true;
                        }
                        catch { }
                        break;
                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) return;
                        else if (menu_options[selected_item].Contains("Add Route"))
                        {
                            Console.Write("Enter Destination [x.x.x.x/xx] ");
                            string resp = Console.ReadLine();
                            string route = "";
                            try
                            {
                                if (resp == "default" || (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0)) { route += $"{resp}%"; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); break; }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); break; }

                            Console.Write("Enter Next Hop [x.x.x.x] ");
                            resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4 && !resp.Contains('/')) { route += $"{resp}%"; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); break; }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); break; }

                            Console.Write("Provide a Metric? Leave blank or -1 for no metric [] ");
                            resp = Console.ReadLine().Replace(" ", "");
                            if (resp == "") resp = "-1";
                            try
                            {
                                if (Convert.ToInt32(resp) > -2) { route += $"{resp}"; }
                                else { Console.WriteLine("Invalid metric. Press ENTER to continue"); Console.ReadLine(); break; }
                            }
                            catch { Console.WriteLine("Invalid metric."); Console.ReadLine(); break; }
                            e.routes.Add(route);
                            refreshMenuOptions = true;
                        }
                        else
                        {
                            try
                            {
                                string current_route = ConvertMenuItemToRoute(menu_options[selected_item]);
                                Console.Write($"Enter Destination [{current_route.Split('%')[0]}]");
                                string resp = Console.ReadLine().Replace(" ", "");
                                if (resp == "") resp = current_route.Split('%')[0];
                                string route = "";
                                try
                                {
                                    if (resp == "default" || (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0)) { route += $"{resp}%"; }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); break; }
                                }
                                catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); break; }

                                Console.Write($"Enter Next Hop [{current_route.Split('%')[1]}]");
                                resp = Console.ReadLine().Replace(" ", "");
                                if (resp == "") resp = current_route.Split('%')[1];
                                try
                                {
                                    if (resp.Split('.').Length == 4 && !resp.Contains('/')) { route += $"{resp}%"; }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); break; }
                                }
                                catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); break; }

                                Console.Write($"Provide a Metric? Leave blank or -1 for no metric [{current_route.Split('%')[2]}]");
                                resp = Console.ReadLine().Replace(" ", "");
                                if (resp == "") resp = current_route.Split('%')[2];
                                try
                                {
                                    if (Convert.ToInt32(resp) > -2) { route += $"{resp}"; }
                                    else { Console.WriteLine("Invalid metric. Press ENTER to continue"); Console.ReadLine(); break; }
                                }
                                catch { Console.WriteLine("Invalid metric."); Console.ReadLine(); break; }
                                e.routes.Add(route);
                                e.routes.Remove(current_route);
                                refreshMenuOptions = true;
                            }catch {}
                        }
                        break;
                }
            }
        }

        public static string ConvertMenuItemToRoute(string menu_item)
        {
            string source = menu_item.Split('|')[0].Replace(" ", "");
            string via = menu_item.Split('|')[1].Replace("via", "").Replace(" ", "");
            string metric = menu_item.Split("(metric ")[1].Replace(" ", "").Replace(")", "");
            return $"{source}%{via}%{metric}";
        }

        public static void Load(string netplanPath)
        {
            ethernets = new List<Ethernet>();
            Console.WriteLine($"Loading netplan config from {netplanPath}");
            string[] lines = [];
            try
            {
                lines = File.ReadAllText(netplanPath).Split('\n');
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred trying to read that file. Try sudo?");
                netplanPath = "";
                Console.ReadLine();
                return;
            }
            int indent_count = lines[Utils.GetLineNumber(lines, "version")].Split("version:")[0].Length;
            string minimum_indent = "";
            for (int i = 0; i < indent_count * 3; i++) minimum_indent += " ";
            if (Utils.GetLineNumber(lines, "ethernets") > -1)
            {
                string workingLines = "";
                for (int i = Utils.GetLineNumber(lines, "ethernets") + 1; i < lines.Length; i++)
                {
                    // Check these two conditions. If both eval to true, then we have a non-blank line that is
                    // not indented enough, so we must have exited the ethernets section
                    if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) < 2) break;
                    else if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) == 2)
                    {
                        ethernets.Add(new Ethernet(workingLines));
                        workingLines = "";
                    }
                    else workingLines += lines[i] + "\n";
                }
                if (workingLines.Length > 0) ethernets.Add(new Ethernet(workingLines));
            }
        }

        public static void Save(Ethernet[] ethernets, string netplanPath, int indent = 2, bool previewOnly = true)
        {
            string tab = "";
            for (int i = 0; i < indent; i++) tab += " ";
            string finished_product = "network:\n";
            finished_product += $"{tab}version: 2\n";
            // finished_product += $"{tab}renderer: networkd\n";
            if (ethernets.Length > 0)
            {
                finished_product += $"{tab}ethernets:\n";
                foreach (Ethernet e in ethernets) finished_product += e.ToYaml() + "\n";
            }
            if (previewOnly) Console.WriteLine(finished_product);
            else
            {
                try
                {
                    File.WriteAllText(netplanPath, finished_product);
                    UnixFileMode permissions = UnixFileMode.UserRead | UnixFileMode.UserWrite | 
                    UnixFileMode.GroupRead | 
                    UnixFileMode.OtherRead;

                    File.SetUnixFileMode(netplanPath, permissions);
                }
                catch
                {
                    Console.WriteLine("An error occurred trying to write to file. Try sudo?");
                    try
                    {
                        File.WriteAllText("/tmp/nptui.bak", finished_product);
                        Console.WriteLine("Dumped file to /tmp/nptui.bak");
                    }
                    catch { }
                    Console.ReadLine();
                }
            }
        }
    }

    public class Ethernet
    {
        public string dhcp4;
        public bool ip4;
        public List<string> addresses = new List<string>();
        public List<string> nameservers = new List<string>();
        public List<string> routes = new List<string>();
        public string name;

        public Ethernet(string data, int indent = 2)
        {
            // Console.WriteLine($"I'm working with: {data}");
            string[] lines = data.Split("\n");
            name = lines[0].Split(':')[0].Replace(" ", "");
            if (data.ToLower().Contains("dhcp4: true") || data.ToLower().Contains("dhcp4: yes")) dhcp4 = "yes";
            else dhcp4 = "no";

            if (dhcp4 == "yes") addresses = [];
            else
            {
                if (Utils.GetLineNumber(lines, "addresses") > -1)
                {
                    int i = Utils.GetLineNumber(lines, "addresses") + 1;
                    while (i < lines.Length && lines[i].Replace(" ", "").StartsWith("-"))
                    {
                        addresses.Add(lines[i].Split(" ")[lines[i].Split(" ").Length - 1]);
                        i += 1;
                    }
                }
                if (Utils.GetLineNumber(lines, "nameservers") > -1)
                {
                    int i = Utils.GetLineNumber(lines, "nameservers") + 1;
                    while (!lines[i].Split(':')[0].EndsWith("addresses")) i += 1;
                    List<string> nameservers_temp = new List<string>(lines[i].Split(':')[1].Split('#')[0].Replace("[", "").Replace("]", "").Replace(" ", "").Split(","));
                    foreach (string nameserver in nameservers_temp) if (!nameservers.Contains(nameserver) && nameserver.Replace(" ", "") != "") nameservers.Add(nameserver);
                }
                if (Utils.GetLineNumber(lines, "routes") > -1)
                {
                    int i = Utils.GetLineNumber(lines, "routes") + 1;
                    while (lines[i].Replace(" ", "") == "") i += 1;
                    while (i < lines.Length && lines[i].Replace(" ", "").Contains("-to:"))
                    {
                        string route = lines[i].Split("to:")[1] + "%" + lines[i + 1].Split("via:")[1];
                        i += 2;
                        if (i < lines.Length && lines[i].Contains("metric:"))
                        {
                            route += $"%{lines[i].Split("metric:")[1]}";
                            i += 1;
                        }
                        else { route += "%-1"; }
                        routes.Add(route.Replace(" ", ""));
                        while (i < lines.Length && lines[i].Replace(" ", "") == "") i += 1;
                    }
                }
                if (Utils.GetLineNumber(lines, "gateway4") > -1)
                {
                    routes.Add($"default%{lines[Utils.GetLineNumber(lines, "gateway4")].Split("way4:")[1].Replace(" ", "")}%-1");
                }
            }
        }

        public string ToYaml(int indent = 2)
        {
            string tab = "";
            for (int i = 0; i < indent; i++) tab += " ";
            string output = @$"{tab}{tab}{name}:
{tab}{tab}{tab}dhcp4: {dhcp4}";
            if (addresses.Count() > 0 && dhcp4 == "no") {
                output += $"\n{tab}{tab}{tab}addresses: ";
                foreach (string s in addresses) output += $"\n{tab}{tab}{tab}{tab}- {s}";
            }
            if (dhcp4 == "no")
            {
                output += @$"
{tab}{tab}{tab}nameservers:
{tab}{tab}{tab}{tab}addresses: [";
                foreach (string s in nameservers) output += s + ",";
                if (nameservers.Count > 0) output = output.TrimEnd(',');
                output += "]";
            }
            if (routes.Count > 0)
            {
                output += @$"
{tab}{tab}{tab}routes:";
                foreach (string r in routes)
                {
                    output += $"\n{tab}{tab}{tab}{tab}- to: {r.Split("%")[0]}\n{tab}{tab}{tab}{tab}  via: {r.Split("%")[1]}";
                    if (r.Split("%")[2] != "-1") output += $"\n{tab}{tab}{tab}{tab}  metric: {r.Split("%")[2]}";
                }
            }
            return output;
        }
    }

    public static class Utils
    {
        public static int GetLineNumber(string[] lines, string search)
        {
            int i = 0;
            while (i < lines.Length)
            {
                if (lines[i].Split(':')[0].EndsWith(search)) return i;
                else i += 1;
            }
            return -1;
        }

        public static int GetIndentationLevel(string line, int indent = 2)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != ' ') return i;
                else i += indent;
            }
            return -1;
        }
    }
}

// network:
//   version: 2
//   ethernets:
//     enp12s0:
//       dhcp4: true
