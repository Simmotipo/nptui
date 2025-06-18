using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace NPTUI
{
    class NPTUI
    {
        public static string nptui_version = "v4.0";
        public static string nptui_date = "18-06-25";
        public static List<Ethernet> ethernets = new List<Ethernet>();
        public static List<Bond> bonds = new List<Bond>();
        public static List<Vlan> vlans = new List<Vlan>();
        public static string netplanPath = "";
        public static void Main(string[] args)
        {
            Console.Clear();
            netplanPath = "";
            if (args.Length > 0) netplanPath = args[0];
            else netplanPath = "/etc/netplan/25-nptui.yaml";
            if (netplanPath != "")
            {
                if (!Utils.CycleBackups(netplanPath)) { Console.WriteLine("Could not create backup configs, quitting. Try sudo next time"); Environment.Exit(0); }
                if (File.Exists(netplanPath))
                {
                    try
                    {
                        if (netplanPath.StartsWith('/')) if (!Load(netplanPath))
                            {
                                Console.WriteLine("An error occurred creating the file. Try sudo?");
                                Environment.Exit(0);
                            }
                        if (!Save(netplanPath, previewOnly: false))
                        {
                            Console.WriteLine("An error occurred creating the file. Try sudo?");
                            Environment.Exit(0);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occurred creating the file. Try sudo?");
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Console.WriteLine($"No such file {netplanPath}. Would you like to create one? [Y/n]");
                    if (netplanPath == "/etc/netplan/25-nptui.yaml" || Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        if (!Utils.CycleBackups(netplanPath)) { Console.WriteLine("Could not create backup configs, quitting. Try sudo next time"); Environment.Exit(0); }
                        try
                        {
                            File.WriteAllText(netplanPath, "network:\n  version: 2");
                            UnixFileMode permissions = UnixFileMode.UserRead | UnixFileMode.UserWrite;

                            File.SetUnixFileMode(netplanPath, permissions);
                            Load(netplanPath);
                        }
                        catch
                        {
                            Console.WriteLine("An error occurred creating the file. Try sudo?");
                            Environment.Exit(0);
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
            string[] menu_options = ["Edit Interfaces", "Edit Bonds", "Edit Vlans", "Edit Hostname", "About NPTUI", "Run Netplan Apply", "Exit"];
            string hostname = File.ReadAllText("/etc/hostname").Replace("\n", "");
            while (true)
            {
                Console.SetCursorPosition(0, 0);
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
                        switch (selected_item)
                        {
                            case 0:
                                if (netplanPath.Replace(" ", "").Replace("/", "") == "")
                                {
                                    Console.WriteLine("Cannot work on config without first loading a file path.\nTo create a new file for it, choose 'Load File' and enter a new absolute file path: you will then be prompted to created it.\nPress ENTER to continue.");
                                    Console.ReadLine();
                                }
                                else InterfacesMenu();
                                break;
                            case 1:
                                if (netplanPath.Replace(" ", "").Replace("/", "") == "")
                                {
                                    Console.WriteLine("Cannot work on config without first loading a file path.\nTo create a new file for it, choose 'Load File' and enter a new absolute file path: you will then be prompted to created it.\nPress ENTER to continue.");
                                    Console.ReadLine();
                                }
                                else BondsMenu();
                                break;
                            case 2:
                                if (netplanPath.Replace(" ", "").Replace("/", "") == "")
                                {
                                    Console.WriteLine("Cannot work on config without first loading a file path.\nTo create a new file for it, choose 'Load File' and enter a new absolute file path: you will then be prompted to created it.\nPress ENTER to continue.");
                                    Console.ReadLine();
                                }
                                else VlansMenu();
                                break;
                            case 3:
                                Console.Clear();
                                Console.Write($"Enter new hostname [{hostname}]: ");
                                string new_hostname = Console.ReadLine();
                                if (new_hostname.Replace(" ", "") != "") { hostname = new_hostname; File.WriteAllText("/etc/hostname", hostname); }
                                break;
                            case 4:
                                Console.Clear();
                                Console.WriteLine($"NetPlan Terminal User Interface (NPTUI)\n{nptui_version} | {nptui_date} | By Simmo <3\n\"{Utils.GetRandomSplash()}\"\nhttps://github.com/simmotipo/nptui");
                                Console.WriteLine("Press ENTER to continue");
                                Console.ReadLine();
                                break;
                            case 5:
                                Console.Clear();
                                Console.WriteLine($"\n   ==== Preview ({netplanPath}) ".PadRight(64, '='));
                                Save(netplanPath, previewOnly: true);
                                Console.WriteLine("\n   ".PadRight(64, '=') + "\n");
                                Console.WriteLine("You are about to apply the above saved netplan configuration. Are you sure you want to apply this? Remember, if you are accessing this server via SSH, applying a config could cause you to get locked out.");
                                bool waitingForConfirmation = true;
                                bool commitToApply = false;
                                while (waitingForConfirmation)
                                {
                                    Console.Write("Are you sure you want to apply this netplan config? [Y/n] ");
                                    string response = Console.ReadLine().Replace(" ", "");
                                    if (response.ToLower() == "n") { commitToApply = false; waitingForConfirmation = false; }
                                    else if (response == "Y") { commitToApply = true; waitingForConfirmation = false; }
                                }
                                if (commitToApply)
                                {
                                    try
                                    {
                                        // Create a new ProcessStartInfo object
                                        ProcessStartInfo startInfo = new ProcessStartInfo
                                        {
                                            FileName = "/usr/bin/sudo",
                                            Arguments = "netplan apply",
                                            UseShellExecute = false,
                                        };

                                        // Start the process
                                        using (Process process = Process.Start(startInfo))
                                        {
                                            // Optionally wait for the process to exit
                                            process.WaitForExit();
                                            Console.WriteLine($"'netplan apply' command executed. Exit Code: {process.ExitCode}. Press ENTER to continue.");
                                            Console.ReadLine();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"An error occurred: {ex.Message}");
                                        Console.ReadLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Netplan apply NOT executed.\nPress ENTER to continue");
                                }
                                break;
                            case 6:
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
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    foreach (Ethernet ethernet in ethernets) menuOptionsList.Add(ethernet.name);
                    menuOptionsList.Add("");
                    //menuOptionsList.Add("+ Add Interface");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
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
                    foreach (Ethernet ethernet in ethernets)
                    {
                        if (ethernet.name == menu_options[i])
                        {
                            if (ethernet.activationmode == "off") Console.ForegroundColor = ConsoleColor.DarkRed;


                            bool isBonded = false;
                            foreach (Bond bond in bonds)
                            {
                                if (bond.interfaceMacs.Contains(ethernet.macaddress)) { isBonded = true; break; }
                            }
                            if (!isBonded) // We're not in a bond, but may have vlan subinterfaces and so have the same restriction...
                            {
                                foreach (Vlan vlan in vlans)
                                {
                                    if (vlan.link == ethernet.macaddress) { isBonded = true; break; }
                                }
                            }
                            if (isBonded) Console.ForegroundColor = ConsoleColor.DarkBlue;
                            break;
                        }
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
                        else if (menu_options[selected_item] != "")
                        {
                            foreach (Ethernet e in ethernets)
                            {
                                if (e.name == menu_options[selected_item])
                                {
                                    bool isBonded = false;
                                    foreach (Bond bond in bonds)
                                    {
                                        if (bond.interfaceMacs.Contains(e.macaddress)) { isBonded = true; break; }
                                    }
                                    if (!isBonded) // We're not in a bond, but may have vlan subinterfaces and so have the same restriction...
                                    {
                                        foreach (Vlan vlan in vlans)
                                        {
                                            if (vlan.link == e.macaddress) { isBonded = true; break; }
                                        }
                                    }
                                    EditInterface(e, isBonded);
                                    break;
                                }
                            }
                            refreshMenuOptions = true;
                        }
                        break;
                }
            }
        }

        public static void BondsMenu()
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    foreach (Bond bond in bonds) menuOptionsList.Add(bond.name);
                    menuOptionsList.Add("");
                    menuOptionsList.Add("+ Create Bond");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
                Console.SetCursorPosition(0,0);
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
                    foreach (Bond bond in bonds)
                    {
                        if (bond.name == menu_options[i])
                        {
                            string b_mac = "00:00:00:00:00:00";
                            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                            {
                                if (ni.Name == bond.name) b_mac = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                            }
                            foreach (Vlan vlan in vlans)
                            {
                                if (vlan.link == b_mac || vlan.link == bond.macaddress || vlan.link == bond.name) { Console.ForegroundColor = ConsoleColor.DarkBlue; break; }
                            }
                            break;
                        }
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
                        else if (menu_options[selected_item].Contains("Create Bond"))
                        {
                            Console.Clear();
                            Console.Write($"Enter bond name: ");
                            string new_name = Console.ReadLine().Replace(" ", "");

                            bool can_add = true;
                            foreach (Bond bond in bonds) if (bond.name == new_name) { can_add = false; break; } // I know this is inefficient; I'll come back to this.
                            if (can_add && new_name.Replace(" ", "") != "") bonds.Add(new Bond($"    {new_name}\n      dhcp4: yes\n      interfaces: []\n      parameters:\n        mode: active-backup", ethernets.ToArray()));
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item] != "")
                        {
                            foreach (Bond b in bonds)
                            {
                                bool isBonded = false;
                                if (b.name == menu_options[selected_item])
                                {
                                    string b_mac = "00:00:00:00:00:00";
                                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                                    {
                                        if (ni.Name == b.name) b_mac = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                                    }
                                    foreach (Vlan vlan in vlans)
                                    {
                                        if (vlan.link == b_mac || vlan.link == b.name) { isBonded = true; break; }
                                    }
                                    EditBond(b, isBonded);
                                    break;
                                }
                            }
                            refreshMenuOptions = true;
                        }
                        break;
                }
            }
        }

        public static void VlansMenu()
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    foreach (Vlan vlan in vlans) menuOptionsList.Add(vlan.name);
                    menuOptionsList.Add("");
                    menuOptionsList.Add("+ Create Vlan");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
                Console.SetCursorPosition(0,0);
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
                        else if (menu_options[selected_item].Contains("Create Vlan"))
                        {
                            refreshMenuOptions = true;
                            try
                            {
                                Console.Clear();
                                Console.Write($"Enter vlan name: ");
                                string new_name = Console.ReadLine().Replace(" ", "");

                                Console.Write($"Enter vlan id: ");
                                string new_id = Convert.ToString(Convert.ToInt32(Console.ReadLine()));

                                string new_link = InterfaceSelectMenu(includeBonded: false, includeVlaned: true, includeBonds: true);
                                if (new_link == "") break;

                                bool can_add = true;
                                foreach (Bond bond in bonds) if (bond.name == new_name) { can_add = false; break; } // I know this is inefficient; I'll come back to this.
                                if (can_add && new_name.Replace(" ", "") != "") { vlans.Add(new Vlan($"    {new_name}\n      id: {new_id}\n      link: {new_link}\n      dhcp4: yes", ethernets.ToArray(), bonds.ToArray())); Save(netplanPath, previewOnly: false);}
                            }
                            catch
                            {
                                Console.WriteLine("Failed to create a VLAN. Ensure you provide a valid name and numeric id.");
                                Console.ReadLine();
                            }
                        }
                        else if (menu_options[selected_item] != "")
                        {
                            foreach (Vlan v in vlans)
                            {
                                if (v.name == menu_options[selected_item])
                                {
                                    EditVlan(v);
                                    refreshMenuOptions = true;
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }


        public static void EditInterface(Ethernet e, bool isBonded = false) // isBonded will disable IP config (this should be used if the interface is EITHER bonded, or has a vlan subinterface.)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add($"Name                  | {e.name}".PadRight(64));
                    menuOptionsList.Add($"Interface Status      | {e.activationmode}".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Config ".PadRight(64, '-'));
                    menuOptionsList.Add($"DHCP                  | {e.dhcp4}".PadRight(64));
                    if (e.dhcp4 == "no")
                    {
                        for (int i = 0; i < e.addresses.Count(); i++) menuOptionsList.Add($"Address {i + 1}             | {e.addresses[i]}".PadRight(64));
                        menuOptionsList.Add($"+ Add Address         ".PadRight(64));
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
                    }
                    int route_count = 0;
                    foreach (string route in e.routes) if (!route.Contains("default")) route_count += 1;
                    menuOptionsList.Add("".PadRight(64));
                    for (int i = 0; i < e.nameservers.Count(); i++) menuOptionsList.Add($"Nameserver {i + 1}          | {e.nameservers[i]}".PadRight(64));
                    menuOptionsList.Add($"+ Add Nameserver      ".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Routing ".PadRight(64, '-'));
                    menuOptionsList.Add($"IPv4 Routes          | {route_count} custom route(s)".PadRight(64));
                    menuOptionsList.Add("".PadRight(64));
                    menuOptionsList.Add("< Back To Menu".PadRight(64));
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
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
                    if (isBonded)
                    {
                        if (menu_options[i].Contains("DHCP") || menu_options[i].Contains("Nameserver") || menu_options[i].Contains("Routes") || menu_options[i].Contains("Status") || menu_options[i].Contains("Address") || menu_options[i].Contains("Gateway")) Console.ForegroundColor = ConsoleColor.DarkRed;
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
                        if (selected_item == menu_options.Length - 1) { Save(netplanPath, previewOnly: false); return; }
                        else if (menu_options[selected_item].Contains("DHCP"))
                        {
                            if (isBonded) break;
                            if (e.dhcp4 == "yes") e.dhcp4 = "no";
                            else e.dhcp4 = "yes";
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Interface Status"))
                        {
                            if (isBonded) break;
                            if (e.activationmode == "on") e.activationmode = "off";
                            else e.activationmode = "on";
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Name "))
                        {
                            Console.Write($"Provide new name [{e.name}] ");
                            string resp = Console.ReadLine();
                            if (resp != "") { e.name = resp.Replace(" ", ""); refreshMenuOptions = true; }
                        }
                        else if (menu_options[selected_item].Contains("IPv4 Routes"))
                        {
                            if (isBonded) break;
                            EditRoutes(e.routes);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Add Address"))
                        {
                            if (isBonded) break;
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
                            if (isBonded) break;
                            Console.Write("Provide new IP [x.x.x.x] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4) { e.nameservers.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Address "))
                        {
                            if (isBonded) break;
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
                            if (isBonded) break;
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
                            if (isBonded) break;
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

                                    Console.Write($"Provide a gateway metric? Enter -1 for no metric [{current_metric}] ");
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

        public static void EditBond(Bond b, bool isBonded = false) // isBonded will disable IP config (this should be used if the interface is EITHER bonded, or has a vlan subinterface.)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add($"Name                  | {b.name}".PadRight(64));
                    menuOptionsList.Add($"Mode                  | {b.mode}".PadRight(64));
                    menuOptionsList.Add($"Mii Monitor Interval  | {b.miiMonitorInterval}ms".PadRight(64));
                    for (int i = 0; i < b.interfaceMacs.Count(); i++)
                    {
                        string interfaceName = "undefined";
                        foreach (Ethernet ethernet in ethernets) {
                            if (ethernet.macaddress == b.interfaceMacs[i]) {interfaceName = ethernet.name; break; }
                        }
                        menuOptionsList.Add($"Interface {i + 1}           | {interfaceName}".PadRight(64));
                    }
                    menuOptionsList.Add($"+ Add Interface      ".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Config ".PadRight(64, '-'));
                    menuOptionsList.Add($"DHCP                  | {b.dhcp4}".PadRight(64));
                    if (b.dhcp4 == "no")
                    {
                        for (int i = 0; i < b.addresses.Count(); i++) menuOptionsList.Add($"Address {i + 1}             | {b.addresses[i]}".PadRight(64));
                        menuOptionsList.Add($"+ Add Address         ".PadRight(64));
                        bool found_gateway = false;
                        foreach (string route in b.routes)
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
                    }
                    int route_count = 0;
                    foreach (string route in b.routes) if (!route.Contains("default")) route_count += 1;
                    menuOptionsList.Add("".PadRight(64));
                    for (int i = 0; i < b.nameservers.Count(); i++) menuOptionsList.Add($"Nameserver {i + 1}          | {b.nameservers[i]}".PadRight(64));
                    menuOptionsList.Add($"+ Add Nameserver      ".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Routing ".PadRight(64, '-'));
                    menuOptionsList.Add($"IPv4 Routes          | {route_count} custom route(s)".PadRight(64));
                    menuOptionsList.Add("".PadRight(64));
                    menuOptionsList.Add("- Delete Bond".PadRight(64));
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
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
                    if (isBonded)
                    {
                        if (menu_options[i].Contains("DHCP") || menu_options[i].Contains("Nameserver") || menu_options[i].Contains("Routes") || menu_options[i].Contains("Status") || menu_options[i].Contains("Address") || menu_options[i].Contains("Delete") || menu_options[i].Contains("Gateway")) Console.ForegroundColor = ConsoleColor.DarkRed;
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
                            b.addresses.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                            refreshMenuOptions = true;
                        }
                        if (menu_options[selected_item].Contains("Interface "))
                        {
                            string interface_name = menu_options[selected_item].Split("| ")[1].Split(' ')[0];
                            string interface_mac = "";
                            foreach (Ethernet ethernet in ethernets) if (ethernet.name == interface_name) { interface_mac = ethernet.macaddress; break; }
                            if (b.interfaceMacs.Contains(interface_mac)) b.interfaceMacs.Remove(interface_mac);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Nameserver "))
                        {
                            b.nameservers.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Gateway "))
                        {
                            foreach (string route in b.routes)
                            {
                                if (route.Contains("default")) { b.routes.Remove(route); break; }
                            }
                            refreshMenuOptions = true;
                        }
                        break;
                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) { Save(netplanPath, previewOnly: false); return; }
                        else if (menu_options[selected_item].Contains("Mode"))
                        {
                            int current_index = 0;
                            while (b.validModes[current_index] != b.mode) current_index++;
                            current_index += 1;
                            if (current_index >= b.validModes.Length) current_index = 0;
                            b.mode = b.validModes[current_index];
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Name "))
                        {
                            Console.Write($"Provide new name [{b.name}] ");
                            string resp = Console.ReadLine();
                            if (resp != "") { b.name = resp.Replace(" ", ""); refreshMenuOptions = true; }
                        }
                        else if (menu_options[selected_item].Contains("IPv4 Routes"))
                        {
                            if (isBonded) break;
                            EditRoutes(b.routes); // I think we can reuse this function without changes?
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Mii"))
                        {
                            Console.Write($"Enter new value in milliseconds [{b.miiMonitorInterval}] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Replace(" ", "") != "") { b.miiMonitorInterval = Convert.ToString(Convert.ToInt32(resp.Replace(" ", ""))); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid value; please provide a valid number. Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("InvInvalid value; please provide a valid number. Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Add Interface"))
                        {
                            string ethernetNameToAdd = InterfaceSelectMenu(includeBonded:false, includeVlaned: false, includeBonds: false);
                            if (ethernetNameToAdd != "")
                            {
                                foreach (Ethernet ethernet in ethernets) if (ethernet.name == ethernetNameToAdd) { b.interfaceMacs.Add(ethernet.macaddress); refreshMenuOptions = true; break; }
                            }
                        }
                        else if (menu_options[selected_item].Contains("Add Address"))
                        {
                            if (isBonded) break;
                            Console.Write("Provide new IP [x.x.x.x/xx] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0) { b.addresses.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Add Nameserver"))
                        {
                            if (isBonded) break;
                            Console.Write("Provide new IP [x.x.x.x] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4) { b.nameservers.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Address "))
                        {
                            if (isBonded) break;
                            Console.Write($"Enter new address [{menu_options[selected_item].Split("| ")[1].Split(' ')[0]}] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Replace(" ", "") != "")
                                {
                                    if (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0)
                                    {
                                        b.addresses.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                                        b.addresses.Add(resp);
                                        refreshMenuOptions = true;
                                    }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                                }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Nameserver "))
                        {
                            if (isBonded) break;
                            Console.Write($"Enter new nameserver [{menu_options[selected_item].Split("| ")[1].Split(' ')[0]}] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Replace(" ", "") != "")
                                {
                                    if (resp.Split('.').Length == 4)
                                    {
                                        b.nameservers.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                                        b.nameservers.Add(resp);
                                        refreshMenuOptions = true;
                                    }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                                }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("DHCP"))
                        {
                            if (isBonded) break;
                            if (b.dhcp4 == "yes") b.dhcp4 = "no";
                            else b.dhcp4 = "yes";
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Gateway "))
                        {
                            if (isBonded) break;
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
                                        foreach (string route in b.routes)
                                        {
                                            if (route.Contains("default"))
                                            {
                                                current_metric = route.Split("%")[2];
                                                break;
                                            }
                                        }
                                    }

                                    Console.Write($"Provide a gateway metric? Enter -1 for no metric [{current_metric}] ");
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
                                        b.routes.Add($"default%{resp}%{metric_resp}");
                                    }
                                    else
                                    {
                                        foreach (string route in b.routes)
                                        {
                                            if (route.Contains("default"))
                                            {
                                                b.routes.Remove(route);
                                                break;
                                            }
                                        }
                                        b.routes.Add($"default%{resp}%{metric_resp}");
                                    }
                                    refreshMenuOptions = true;
                                }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }

                        }
                        else if (menu_options[selected_item].Contains("Delete Bond"))
                        {
                            if (isBonded) break; // only allow people to delete this bond if the vlans on it are deleted first.
                            Console.Clear();
                            Console.Write($"Are you sure you wish to remove this bond? [Y/n]: ");
                            if (Console.ReadLine().ToLower() == "y")
                            {
                                bonds.Remove(b);
                                Save(netplanPath, previewOnly: false);
                                return;
                            }
                        }
                        break;
                }
            }
        }

        public static void EditVlan(Vlan v) // isBonded will disable IP config (this should be used if the interface is EITHER bonded, or has a vlan subinterface.)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add($"Name                  | {v.name}".PadRight(64));
                    menuOptionsList.Add($"Id                    | {v.id}".PadRight(64));
                    menuOptionsList.Add($"Link                  | {Utils.AttemptVlanLinkNameRecall(v.link, ethernets.ToArray(), bonds.ToArray())}".PadRight(64));      // This will need updating

                    menuOptionsList.Add($"---- IPv4 Config ".PadRight(64, '-'));
                    menuOptionsList.Add($"DHCP                  | {v.dhcp4}".PadRight(64));
                    if (v.dhcp4 == "no")
                    {
                        for (int i = 0; i < v.addresses.Count(); i++) menuOptionsList.Add($"Address {i + 1}             | {v.addresses[i]}".PadRight(64));
                        menuOptionsList.Add($"+ Add Address         ".PadRight(64));
                        bool found_gateway = false;
                        foreach (string route in v.routes)
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
                    }
                    int route_count = 0;
                    foreach (string route in v.routes) if (!route.Contains("default")) route_count += 1;
                    menuOptionsList.Add("".PadRight(64));
                    for (int i = 0; i < v.nameservers.Count(); i++) menuOptionsList.Add($"Nameserver {i + 1}          | {v.nameservers[i]}".PadRight(64));
                    menuOptionsList.Add($"+ Add Nameserver      ".PadRight(64));
                    menuOptionsList.Add($"---- IPv4 Routing ".PadRight(64, '-'));
                    menuOptionsList.Add($"IPv4 Routes          | {route_count} custom route(s)".PadRight(64));
                    menuOptionsList.Add("".PadRight(64));
                    menuOptionsList.Add("- Delete Vlan".PadRight(64));
                    menuOptionsList.Add("< Back To Menu".PadRight(64));
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
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
                            v.addresses.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Nameserver "))
                        {
                            v.nameservers.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Gateway "))
                        {
                            foreach (string route in v.routes)
                            {
                                if (route.Contains("default")) { v.routes.Remove(route); break; }
                            }
                            refreshMenuOptions = true;
                        }
                        break;
                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) { Save(netplanPath, previewOnly: false); return; }
                        else if (menu_options[selected_item].Contains("Name "))
                        {
                            Console.Write($"Provide new name [{v.name}] ");
                            string resp = Console.ReadLine();
                            if (resp != "") { v.name = resp.Replace(" ", ""); refreshMenuOptions = true; }
                        }
                        else if (menu_options[selected_item].Contains("IPv4 Routes"))
                        {
                            EditRoutes(v.routes); // I think we can reuse this function without changes?
                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Id"))
                        {
                            Console.Write($"Enter new ID: [{v.id}] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Replace(" ", "") != "") { v.id = Convert.ToString(Convert.ToInt32(resp.Replace(" ", ""))); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid value; please provide a valid number. Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid value; please provide a valid number. Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Add Address"))
                        {
                            Console.Write("Provide new IP [x.x.x.x/xx] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0) { v.addresses.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("Link "))
                        {
                            string new_link = InterfaceSelectMenu(includeBonded: false, includeVlaned: true, includeBonds: true); // This will be the name, now we should try to get the MAC
                            v.link = Utils.AttemptVlanLinkMacRecall(new_link, ethernets.ToArray(), bonds.ToArray());

                            refreshMenuOptions = true;
                        }
                        else if (menu_options[selected_item].Contains("Add Nameserver"))
                        {
                            Console.Write("Provide new IP [x.x.x.x] ");
                            string resp = Console.ReadLine();
                            try
                            {
                                if (resp.Split('.').Length == 4) { v.nameservers.Add(resp); refreshMenuOptions = true; }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
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
                                        v.addresses.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                                        v.addresses.Add(resp);
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
                                        v.nameservers.Remove(menu_options[selected_item].Split("| ")[1].Split(' ')[0]);
                                        v.nameservers.Add(resp);
                                        refreshMenuOptions = true;
                                    }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                                }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                        }
                        else if (menu_options[selected_item].Contains("DHCP"))
                        {
                            if (v.dhcp4 == "yes") v.dhcp4 = "no";
                            else v.dhcp4 = "yes";
                            refreshMenuOptions = true;
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
                                        foreach (string route in v.routes)
                                        {
                                            if (route.Contains("default"))
                                            {
                                                current_metric = route.Split("%")[2];
                                                break;
                                            }
                                        }
                                    }

                                    Console.Write($"Provide a gateway metric? Enter -1 for no metric [{current_metric}] ");
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
                                        v.routes.Add($"default%{resp}%{metric_resp}");
                                    }
                                    else
                                    {
                                        foreach (string route in v.routes)
                                        {
                                            if (route.Contains("default"))
                                            {
                                                v.routes.Remove(route);
                                                break;
                                            }
                                        }
                                        v.routes.Add($"default%{resp}%{metric_resp}");
                                    }
                                    refreshMenuOptions = true;
                                }
                                else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); }

                        }
                        else if (menu_options[selected_item].Contains("Delete Vlan"))
                        {
                            Console.Clear();
                            Console.Write($"Are you sure you wish to remove this vlan? [Y/n]: ");
                            if (Console.ReadLine().ToLower() == "y")
                            {
                                vlans.Remove(v);
                                Save(netplanPath, previewOnly: false);
                                return;
                            }
                        }
                        break;
                }
            }
        }



        public static string InterfaceSelectMenu(bool includeBonded = true, bool includeVlaned = true, bool includeBonds = false)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add("--- Interfaces");
                    foreach (Ethernet ethernet in ethernets)
                    {
                        if (includeBonded && includeVlaned) { menuOptionsList.Add(ethernet.name); continue; }
                        bool can_add = true;
                        if (!includeBonded)
                        {
                            foreach (Bond bond in bonds) if (bond.interfaceMacs.Contains(ethernet.macaddress)) { can_add = false; break; } // I know this is inefficient; I'll come back to this.
                        }
                        if (can_add && !includeVlaned)
                        {
                            foreach (Vlan vlan in vlans) if (vlan.link == ethernet.name || vlan.link == ethernet.macaddress) { can_add = false; break; }
                        }
                        if (can_add) menuOptionsList.Add(ethernet.name);
                    }
                    menuOptionsList.Add("");
                    if (includeBonds)
                    {
                        menuOptionsList.Add("--- Bonds");
                        foreach (Bond bond in bonds)
                        {
                            menuOptionsList.Add(bond.name);
                        }
                        menuOptionsList.Add("");
                    }
                    menuOptionsList.Add("< Back");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
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
                        if (selected_item == menu_options.Length - 1) return "";
                        else if (menu_options[selected_item] != "")
                        {
                            return menu_options[selected_item];
                        }
                        break;
                }
            }
        }


        public static List<string> EditRoutes(List<string> routes)
        {
            int selected_item = 0;
            bool refreshMenuOptions = true;
            string[] menu_options = [];
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (refreshMenuOptions)
                {
                    List<string> menuOptionsList = new List<string>();
                    menuOptionsList.Add("Destination      | via Next Hop         | Metric (-1 = no metric)");
                    menuOptionsList.Add("".PadRight(48, '-'));
                    foreach (string route in routes) menuOptionsList.Add($"{route.Split("%")[0].PadRight(16)} | via {route.Split("%")[1].PadRight(16)} | (metric {route.Split("%")[2]})".PadRight(48));
                    menuOptionsList.Add("");
                    menuOptionsList.Add("+ Add Route");
                    menuOptionsList.Add("< Back To Menu");
                    menu_options = menuOptionsList.ToArray();
                    refreshMenuOptions = false;
                    Console.Clear();
                }
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
                            routes.Remove(ConvertMenuItemToRoute(menu_options[selected_item]));
                            refreshMenuOptions = true;
                        }
                        catch { }
                        break;
                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (selected_item == menu_options.Length - 1) return routes;
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
                            routes.Add(route);
                            refreshMenuOptions = true;
                        }
                        else
                        {
                            try
                            {
                                string current_route = ConvertMenuItemToRoute(menu_options[selected_item]);
                                Console.Write($"Enter Destination [{current_route.Split('%')[0]}] ");
                                string resp = Console.ReadLine().Replace(" ", "");
                                if (resp == "") resp = current_route.Split('%')[0];
                                string route = "";
                                try
                                {
                                    if (resp == "default" || (resp.Split('.').Length == 4 && resp.Contains('/') && Convert.ToInt32(resp.Split('/')[1]) < 33 && Convert.ToInt32(resp.Split('/')[1]) > 0)) { route += $"{resp}%"; }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); break; }
                                }
                                catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x/xx? Press ENTER to continue"); Console.ReadLine(); break; }

                                Console.Write($"Enter Next Hop [{current_route.Split('%')[1]}] ");
                                resp = Console.ReadLine().Replace(" ", "");
                                if (resp == "") resp = current_route.Split('%')[1];
                                try
                                {
                                    if (resp.Split('.').Length == 4 && !resp.Contains('/')) { route += $"{resp}%"; }
                                    else { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); break; }
                                }
                                catch { Console.WriteLine("Invalid IP address. Did you definitely use the format x.x.x.x? Press ENTER to continue"); Console.ReadLine(); break; }

                                Console.Write($"Provide a Metric? Leave blank or -1 for no metric [{current_route.Split('%')[2]}] ");
                                resp = Console.ReadLine().Replace(" ", "");
                                if (resp == "") resp = current_route.Split('%')[2];
                                try
                                {
                                    if (Convert.ToInt32(resp) > -2) { route += $"{resp}"; }
                                    else { Console.WriteLine("Invalid metric. Press ENTER to continue"); Console.ReadLine(); break; }
                                }
                                catch { Console.WriteLine("Invalid metric."); Console.ReadLine(); break; }
                                routes.Add(route);
                                routes.Remove(current_route);
                                refreshMenuOptions = true;
                            }
                            catch { }
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

        public static bool Load(string path)
        {
            ethernets = new List<Ethernet>();
            Console.WriteLine($"Loading netplan config from {path}");
            string[] lines = [];
            try
            {
                lines = File.ReadAllText(path).Split('\n');
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred trying to read that file. Try sudo?");
                netplanPath = "";
                Console.ReadLine();
                return false;
            }
            int indent_count = lines[Utils.GetLineNumber(lines, "version")].Split("version:")[0].Length;
            string minimum_indent = "";
            for (int i = 0; i < indent_count * 3; i++) minimum_indent += " ";
            if (Utils.GetLineNumber(lines, "ethernets") > -1)
            {
                string workingLines = "";
                bool first_pass = true;
                for (int i = Utils.GetLineNumber(lines, "ethernets") + 1; i < lines.Length; i++)
                {
                    // Check these two conditions. If both eval to true, then we have a non-blank line that is
                    // not indented enough, so we must have exited the ethernets section
                    if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) < 2) break;
                    else if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) == 2)
                    {
                        if (first_pass) { workingLines += lines[i] + "\n"; first_pass = false; }
                        else
                        {
                            ethernets.Add(new Ethernet(workingLines));
                            workingLines = "";
                            workingLines += lines[i] + "\n";
                        }
                    }
                    else workingLines += lines[i] + "\n";
                }
                if (workingLines.Length > 0) ethernets.Add(new Ethernet(workingLines));
            }
            if (Utils.GetLineNumber(lines, "bonds") > -1)
            {
                string workingLines = "";
                bool first_pass = true;
                for (int i = Utils.GetLineNumber(lines, "bonds") + 1; i < lines.Length; i++)
                {
                    // Check these two conditions. If both eval to true, then we have a non-blank line that is
                    // not indented enough, so we must have exited the ethernets section
                    if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) < 2) break;
                    else if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) == 2)
                    {
                        if (first_pass) { workingLines += lines[i] + "\n"; first_pass = false; }
                        else
                        {
                            bonds.Add(new Bond(workingLines, ethernets.ToArray()));
                            workingLines = "";
                            workingLines += lines[i] + "\n";
                        }
                    }
                    else workingLines += lines[i] + "\n";
                }
                if (workingLines.Length > 0) bonds.Add(new Bond(workingLines, ethernets.ToArray()));
            }
            if (Utils.GetLineNumber(lines, "vlans") > -1)
            {
                string workingLines = "";
                bool first_pass = true;
                for (int i = Utils.GetLineNumber(lines, "vlans") + 1; i < lines.Length; i++)
                {
                    // Check these two conditions. If both eval to true, then we have a non-blank line that is
                    // not indented enough, so we must have exited the ethernets section
                    if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) < 2) break;
                    else if (lines[i] != "" && Utils.GetIndentationLevel(lines[i], indent_count) == 2)
                    {
                        if (first_pass) { workingLines += lines[i] + "\n"; first_pass = false; }
                        else
                        {
                            vlans.Add(new Vlan(workingLines, ethernets.ToArray(), bonds.ToArray()));
                            workingLines = "";
                            workingLines += lines[i] + "\n";
                        }
                    }
                    else workingLines += lines[i] + "\n";
                }
                if (workingLines.Length > 0) vlans.Add(new Vlan(workingLines, ethernets.ToArray(), bonds.ToArray()));
            }

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                bool can_add = true;
                string activationmode = "off";
                if (ni.OperationalStatus == OperationalStatus.Up) activationmode = "on";
                string macaddress = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                foreach (Ethernet ethernet in ethernets) if (ethernet.name == ni.Name || ethernet.macaddress == macaddress) { can_add = false; break; } // I know this is inefficient; I'll come back to this.
                if (can_add)
                {
                    ethernets.Add(new Ethernet($"    {ni.Name}\n      dhcp4: true\n      activation-mode: {activationmode}\n      set-name: {ni.Name}\n      match:\n        macaddress: \"{macaddress}\""));
                }
            }
            List<Ethernet> ethernetsToDelete = new List<Ethernet>();
            foreach (Ethernet ethernet in ethernets)
            {
                foreach (Bond bond in bonds)
                {
                    if (bond.name == ethernet.name)
                    {
                        bond.macaddress = ethernet.macaddress; //Load this in while we're here;
                        ethernetsToDelete.Add(ethernet); break;
                    }
                }
                foreach (Vlan vlan in vlans) // Not sure if this check is necessary, but I'm adding it in for now.
                {
                    if (vlan.name == ethernet.name) { ethernetsToDelete.Add(ethernet); break; }
                }
                if (!ethernetsToDelete.Contains(ethernet) && (ethernet.name.ToLower().Contains("bond") || ethernet.name.ToLower().Contains("vlan"))) ethernetsToDelete.Add(ethernet); // If it has 'bond' or 'vlan' in the name, but doesn't exist as bond config, it's prolly stale- purge it.
            }
            foreach (Ethernet ethernet in ethernetsToDelete) ethernets.Remove(ethernet);
            foreach (Ethernet ethernet in ethernets)
            {
                if (ethernet.name == "lo") { ethernets.Remove(ethernet); break; }
            }
            return true;
        }

        public static bool Save(string netplanPath, int indent = 2, bool previewOnly = true)
        {
            string tab = "";
            for (int i = 0; i < indent; i++) tab += " ";
            string finished_product = "network:\n";
            finished_product += $"{tab}version: 2\n";
            finished_product += $"{tab}renderer: networkd\n"; // Set networkd is the renderer. If you're using NetworkManager, this means you should already be able to use nmtui for that, and honestly, that's better.
                                                                // finished_product += $"{tab}renderer: networkd\n";
            if (ethernets.Count > 0)
            {
                finished_product += $"{tab}ethernets:\n";
                foreach (Ethernet e in ethernets) if (e.name != "lo") finished_product += e.ToYaml(bonds: bonds.ToArray()) + "\n";
            }
            if (bonds.Count > 0)
            {
                Console.WriteLine($"There are {bonds.Count} bonds");
                finished_product += $"{tab}bonds:\n";
                foreach (Bond b in bonds) finished_product += b.ToYaml(ethernets: ethernets.ToArray()) + "\n";
            }
            if (vlans.Count > 0)
            {
                finished_product += $"{tab}vlans:\n";
                foreach (Vlan v in vlans) finished_product += v.ToYaml(ethernets: ethernets.ToArray(), bonds: bonds.ToArray()) + "\n";
            }
            if (previewOnly) { Console.WriteLine(finished_product); return true; }
            else
            {
                try
                {
                    File.WriteAllText(netplanPath, finished_product);
                    UnixFileMode permissions = UnixFileMode.UserRead | UnixFileMode.UserWrite;

                    File.SetUnixFileMode(netplanPath, permissions);
                    return true;
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
                    Console.WriteLine("At least something went wrong");
                    Console.ReadLine();
                    return false;
                }
            }
        }
    }

    public class Ethernet
    {
        public string dhcp4;
        public List<string> addresses = new List<string>();
        public List<string> nameservers = new List<string>();
        public List<string> routes = new List<string>();
        public string name;

        public string activationmode;

        public string macaddress;

        public Ethernet(string data, int indent = 2)
        {
            Console.WriteLine($"I'm working with: {data}");
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
                if (Utils.GetLineNumber(lines, "gateway4") > -1)
                {
                    routes.Add($"default%{lines[Utils.GetLineNumber(lines, "gateway4")].Split("way4:")[1].Replace(" ", "")}%-1");
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
            if (Utils.GetLineNumber(lines, "macaddress") > -1)
            {
                macaddress = lines[Utils.GetLineNumber(lines, "macaddress")].Split("macaddress:")[1].Replace(" ", "").Replace("\"", "");
            }
            else
            {
                bool found_interface = false;
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.Name == name)
                    {
                        macaddress = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                        found_interface = true;
                        break;
                    }
                }
                if (!found_interface) macaddress = "00:00:00:00:00:00";
            }
            if (Utils.GetLineNumber(lines, "activation-mode") > -1)
            {
                activationmode = lines[Utils.GetLineNumber(lines, "activation-mode")].Split(":")[1].Replace(" ", "");
            }
            else
            {
                activationmode = "on"; // Presume on
            }

        }

        public string ToYaml(Bond[] bonds, int indent = 2)
        {
            bool isBonded = false;
            foreach (Bond bond in bonds)
            {
                if (bond.interfaceMacs.Contains(macaddress)) {isBonded = true; break;}
            }
            if (isBonded) { // Purge these values so we don't generate invalid netplan config as part of a bond.
                dhcp4 = "no";
                nameservers = new List<string>();
                addresses = new List<string>();
                routes = new List<string>();
                activationmode = "on";
            }
            string tab = "";
            for (int i = 0; i < indent; i++) tab += " ";
            string output = $"{tab}{tab}{name}:\n{tab}{tab}{tab}dhcp4: {dhcp4}";
            if (addresses.Count() > 0 && dhcp4 == "no")
            {
                output += $"\n{tab}{tab}{tab}addresses: ";
                foreach (string s in addresses) output += $"\n{tab}{tab}{tab}{tab}- {s}";
            }
            if (nameservers.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}nameservers:\n{tab}{tab}{tab}{tab}addresses: [";
                foreach (string s in nameservers) output += s + ",";
                if (nameservers.Count > 0) output = output.TrimEnd(',');
                output += "]";
            }
            if (routes.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}routes:";
                foreach (string r in routes)
                {
                    output += $"\n{tab}{tab}{tab}{tab}- to: {r.Split("%")[0]}\n{tab}{tab}{tab}{tab}  via: {r.Split("%")[1]}";
                    if (r.Split("%")[2] != "-1") output += $"\n{tab}{tab}{tab}{tab}  metric: {r.Split("%")[2]}";
                }
            }
            output += $"\n{tab}{tab}{tab}match:\n{tab}{tab}{tab}{tab}macaddress: \"{macaddress}\"\n{tab}{tab}{tab}set-name: {name}";
            if (activationmode == "off") output += $"\n{tab}{tab}{tab}activation-mode: {activationmode}";
            return output;
        }
    }

    public class Bond
    {
        public string dhcp4;
        public List<string> addresses = new List<string>();
        public List<string> nameservers = new List<string>();
        public List<string> routes = new List<string>();
        public string name;

        public string mode;

        // We'll use this to make cycling through modes easier.
        public string[] validModes = ["active-backup", "balance-alb", "balance-tlb", "balance-rr", "balance-xor", "broadcast", "802.3ad"];

        public string miiMonitorInterval;

        public string macaddress;

        public List<string> interfaceMacs = new List<string>();

        public Bond(string data, Ethernet[] ethernets, int indent = 2)
        {
            Console.WriteLine($"I'm working with: {data}");
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
                if (Utils.GetLineNumber(lines, "gateway4") > -1)
                {
                    routes.Add($"default%{lines[Utils.GetLineNumber(lines, "gateway4")].Split("way4:")[1].Replace(" ", "")}%-1");
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

            if (Utils.GetLineNumber(lines, "macaddress") > -1)
            {
                macaddress = lines[Utils.GetLineNumber(lines, "macaddress")].Split("macaddress:")[1].Replace(" ", "").Replace("\"", "");
            }
            else
            {
                bool found_interface = false;
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.Name == name)
                    {
                        macaddress = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                        found_interface = true;
                        break;
                    }
                }
                if (!found_interface) macaddress = name;
            }
            if (Utils.GetLineNumber(lines, "mode") > -1)
            {
                mode = lines[Utils.GetLineNumber(lines, "mode")].Split(":")[1].Replace(" ", "");
            }
            else
            {
                mode = "active-passive"; // default to active-passive
            }
            if (Utils.GetLineNumber(lines, "mii-monitor-interval") > -1)
            {
                miiMonitorInterval = lines[Utils.GetLineNumber(lines, "mii-monitor-interval")].Split(":")[1].Replace(" ", "");
            }
            else
            {
                miiMonitorInterval = "100"; // default to active-passive
            }
            
            if (Utils.GetLineNumber(lines, "interfaces") > -1)
            {
                int i = Utils.GetLineNumber(lines, "interfaces");
                List<string> interfaces_temp = new List<string>(lines[i].Split(':')[1].Split('#')[0].Replace("[", "").Replace("]", "").Replace(" ", "").Split(","));
                foreach (string interfaceName in interfaces_temp)
                {
                    foreach (Ethernet eth in ethernets)
                    {
                        if (eth.name == interfaceName && !interfaceMacs.Contains(eth.macaddress)) interfaceMacs.Add(eth.macaddress);
                    }
                }
            }
        }

        public string ToYaml(Ethernet[] ethernets, int indent = 2)
        {
            string tab = "";
            for (int i = 0; i < indent; i++) tab += " ";
            string output = $"{tab}{tab}{name}:\n{tab}{tab}{tab}dhcp4: {dhcp4}";
            if (addresses.Count() > 0 && dhcp4 == "no")
            {
                output += $"\n{tab}{tab}{tab}addresses: ";
                foreach (string s in addresses) output += $"\n{tab}{tab}{tab}{tab}- {s}";
            }
            if (nameservers.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}nameservers:\n{tab}{tab}{tab}{tab}addresses: [";
                foreach (string s in nameservers) output += s + ",";
                if (nameservers.Count > 0) output = output.TrimEnd(',');
                output += "]";
            }
            if (interfaceMacs.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}interfaces: [";
                foreach (string s in interfaceMacs)
                {
                    foreach (Ethernet ethernet in ethernets)
                    {
                        if (ethernet.macaddress == s) output += ethernet.name + ",";
                    }
                }
                output = output.TrimEnd(',') + "]";
            }
            else output += $"\n{tab}{tab}{tab}interfaces: []";
            if (routes.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}routes:";
                foreach (string r in routes)
                {
                    output += $"\n{tab}{tab}{tab}{tab}- to: {r.Split("%")[0]}\n{tab}{tab}{tab}{tab}  via: {r.Split("%")[1]}";
                    if (r.Split("%")[2] != "-1") output += $"\n{tab}{tab}{tab}{tab}  metric: {r.Split("%")[2]}";
                }
            }
            output += $"\n{tab}{tab}{tab}parameters:\n{tab}{tab}{tab}{tab}mode: {mode}\n{tab}{tab}{tab}{tab}mii-monitor-interval: {miiMonitorInterval}";
            return output;
        }
    }

    public class Vlan
    {
        public string dhcp4;
        public List<string> addresses = new List<string>();
        public List<string> nameservers = new List<string>();
        public List<string> routes = new List<string>();
        public string name;

        public string id;
        public string link;

        public Vlan(string data, Ethernet[] ethernets, Bond[] bonds, int indent = 2)
        {
            Console.WriteLine($"I'm working with: {data}");
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
                if (Utils.GetLineNumber(lines, "gateway4") > -1)
                {
                    routes.Add($"default%{lines[Utils.GetLineNumber(lines, "gateway4")].Split("way4:")[1].Replace(" ", "")}%-1");
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
            if (Utils.GetLineNumber(lines, "id") > -1)
            {
                id = lines[Utils.GetLineNumber(lines, "id")].Split(":")[1].Replace(" ", "");
            }
            else
            {
                id = "10"; // default to 10 - this isn't optional so we have to go with something....
            }
            link = "00:00:00:00:00:00"; // default to over localhost, else netplan apply may not work if we can't reach it from here (though, this isn't valid. Put a better check in here later, simmo.):
            if (Utils.GetLineNumber(lines, "link") > -1)
            {
                link = lines[Utils.GetLineNumber(lines, "link")].Split(":")[1].Replace(" ", "");
                link = Utils.AttemptVlanLinkMacRecall(link, ethernets, bonds);
                // If we don't find a match, this means we're likely bound to a bond that doesn't exist yet (not been netplan applied). We'll just use the bond name for now.
            }
        }

        public string ToYaml(Ethernet[] ethernets, Bond[] bonds, int indent = 2)
        {
            string tab = "";
            for (int i = 0; i < indent; i++) tab += " ";
            string output = $"{tab}{tab}{name}:\n{tab}{tab}{tab}dhcp4: {dhcp4}";
            if (addresses.Count() > 0 && dhcp4 == "no")
            {
                output += $"\n{tab}{tab}{tab}addresses: ";
                foreach (string s in addresses) output += $"\n{tab}{tab}{tab}{tab}- {s}";
            }
            if (nameservers.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}nameservers:\n{tab}{tab}{tab}{tab}addresses: [";
                foreach (string s in nameservers) output += s + ",";
                if (nameservers.Count > 0) output = output.TrimEnd(',');
                output += "]";
            }
            if (routes.Count > 0)
            {
                output += $"\n{tab}{tab}{tab}routes:";
                foreach (string r in routes)
                {
                    output += $"\n{tab}{tab}{tab}{tab}- to: {r.Split("%")[0]}\n{tab}{tab}{tab}{tab}  via: {r.Split("%")[1]}";
                    if (r.Split("%")[2] != "-1") output += $"\n{tab}{tab}{tab}{tab}  metric: {r.Split("%")[2]}";
                }
            }
            output += $"\n{tab}{tab}{tab}id:{id}\n{tab}{tab}{tab}link: {Utils.AttemptVlanLinkNameRecall(link, ethernets, bonds)}";
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
            int level = 0;
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] != ' ') return level;
                else { i += indent; level++; }
            }
            return -1;
        }

        private static readonly string[] AboutSplashes = [
            "Enough with the YAML.",
            "Made by people who have had to read character-by-character a netplan YAML file to a not-yaml-or-netplan-literate engineer over the phone in a datacenter, for people who have had to read character-by-character a netplan YAML file to a not-yaml-or-netplan-literate engineer over the phone in a datacenter.",
            "At least it's not network-scripts.",
            "Adding support for RPF filters one day (maybe).",
            "Netplan? I hardly know an.",
            "Why are we here? Just to YAML?",
            "Imagine showing netplan to a caveman.",
            "I could have learnt netplan and yaml. Instead I wrote 1000 lines of C#.",
            "If someone could explain why I have to log into KDE plasma twice (the first hangs for 60 seconds then fails) on my Ubuntu 24.04 PC, that'd be great. Thanks.",
            "[insert imaginative and funny phrase here.]",
            "Configure? I hardly know 'er."
        ];
        private static readonly Random random = new Random();
        public static string GetRandomSplash()
        {
            // Generate a random index within the bounds of the array
            // The upper bound of Next() is exclusive, so Length gives valid indices [0, Length-1]
            int index = random.Next(AboutSplashes.Length);

            // Return the item at the random index
            return AboutSplashes[index];
        }

        public static bool CycleBackups(string path)
        {
            try
            {
                string pathKey = path.Split('/')[path.Split('/').Length - 1];
                for (int i = 4; i >= 0; i--)
                {
                    if (File.Exists($"/etc/netplan/{pathKey}.bak-{i}"))
                    {
                        if (File.Exists($"/etc/netplan/{pathKey}.bak-{i + 1}")) File.Delete($"/etc/netplan/{pathKey}.bak-{i + 1}");
                        File.Move($"/etc/netplan/{pathKey}.bak-{i}", $"/etc/netplan/{pathKey}.bak-{i + 1}");
                    }
                }
                if (File.Exists($"/etc/netplan/{pathKey}"))
                {
                    File.Copy($"/etc/netplan/{pathKey}", $"/etc/netplan/{pathKey}.bak-{0}");
                }
                return true;
            }
            catch { return false; }
        }

        public static string AttemptVlanLinkNameRecall(string link, Ethernet[] ethernets, Bond[] bonds)
        {
            foreach (Ethernet ethernet in ethernets) // First look for a mac match amongst the interfaces
            {
                if (ethernet.macaddress == link) { return ethernet.name; }
            }
            foreach (Bond bond in bonds) // If we don't find a mac match amongst the interfaces, we should check the bonds
            {
                if (bond.macaddress == link) { return bond.name; }
            }
            // If we haven't found a mac match amongst either the bonds or interfaces, it probably means we're looking at a bond that hasn't been applied/created yet, and link is already the name.
            // We've not matched an existing interface; this means we've probably been added to a bond which has not yet been netplan applied.
            // In that case, the link should be the link name. We should be able to presume this for now.
            return link;
        }

        public static string AttemptVlanLinkMacRecall(string link, Ethernet[] ethernets, Bond[] bonds)
        {
            Console.WriteLine($"Trying to find the mac address of {link}");
            foreach (Ethernet ethernet in ethernets) // First look for a mac match amongst the interfaces
            {
                if (ethernet.name == link) { Console.WriteLine($"Found ethernet {ethernet.macaddress}"); return ethernet.macaddress; }
            }
            foreach (Bond bond in bonds) // If we don't find a mac match amongst the interfaces, we should check the bonds
            {
                if (bond.name == link || bond.macaddress == link) { Console.WriteLine($"Found bond {bond.macaddress}"); return bond.macaddress; }
            }
            // If we haven't found a mac match amongst either the bonds or interfaces, it probably means we're looking at a bond that hasn't been applied/created yet, and link is already the name.
            // We've not matched an existing interface; this means we've probably been added to a bond which has not yet been netplan applied.
            // In that case, the link should be the link name. We should be able to presume this for now.

            return link;
        }
    }
}