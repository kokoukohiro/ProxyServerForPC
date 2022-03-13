using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Start();
            program.Deal();
            Systemproxy.Unset();
        }
        private void Start()
        {
            Console.WriteLine("Server or client?");
            string command;
            command = Console.ReadLine().ToLower();
            while (!command.Equals("exit"))
            {
                switch (command)
                {
                    case "server":
                        ServerStart();
                        return;
                    case "client":
                        ClientStart();
                        return;
                    default:
                        Console.WriteLine("Command not understood.");
                        break;
                }
                command = Console.ReadLine().ToLower();
            }
        }
        private void Deal()
        {
            string command;
            command = Console.ReadLine().ToLower();
            while (!command.Equals("exit"))
            {
                switch (command)
                {
                    case "showtopo":
                        ShowTopo();
                        break;
                    case "portmap":
                        StartPortmapper();
                        break;
                    case "package":
                        ShowPackage();
                        break;
                    case "chat":
                        StartChat();
                        break;
                    case "latency":
                        ShowLatency();
                        break;
                    case "switch":
                        StartSwitch();
                        break;
                    case "userule":
                        StartRule();
                        break;
                    default:
                        Console.WriteLine("Command not understood.");
                        break;
                }
                command = Console.ReadLine().ToLower();
            }
        }
        private int SetPort()
        {
            string command;
            int port = 0;
            while (true)
            {
                Console.WriteLine("Please enter a port:");
                command = Console.ReadLine();
                try
                {
                    port = int.Parse(command);
                    return port;
                }
                catch { }
            }
        }
        private string SetHost()
        {
            string command;
            IPAddress host = null;
            while (true)
            {
                Console.WriteLine("Please enter a host:");
                command = Console.ReadLine();
                try
                {
                    host = IPAddress.Parse(command);
                    return host.ToString();
                }
                catch { }
            }
        }
        private void ServerStart()
        {
            Configuration.isServer = true;
            Configuration.localPort = SetPort();
            Listener listener = new Listener();
            listener.Start();
            Task.Run(() =>
            {
                StartConfig();
            });
        }
        private void ClientStart()
        {
            Configuration.isServer = false;
            Configuration.localPort = SetPort();
            Configuration.serverHost = SetHost();
            Configuration.serverPort = SetPort();
            Systemproxy.Set("127.0.0.1", Configuration.localPort.ToString());
            Listener listener = new Listener();
            listener.Start();
            Task.Run(() =>
            {
                StartConfig();
            });
        }
        private void StartConfig()
        {
            PortMapper mapper = new PortMapper();
            if (Configuration.isServer && mapper.Start())
            {
                (new LatencyTester(null)).TestComplate();
            }
            if (!Configuration.isServer && mapper.Check())
            {
                IPAsker ask = new IPAsker();
                ask.Start();
                ChatReceiver chat = new ChatReceiver();
                chat.Start();
                LatencyReceiver latency = new LatencyReceiver();
                latency.Start();
                (new LatencyTester(null)).TestComplate();
            }
        }
        private void ShowTopo()
        {
            TopologyHandler handler = new TopologyHandler(true);
            if (!Configuration.isServer)
                handler.Start();
            else
                handler.ShowTopo();
        }
        private void StartPortmapper()
        {
            if (Configuration.isServer)
            {
                Console.WriteLine("Command not understood.");
                return;
            }
            Console.WriteLine("Mapping the port...");
            PortMapper mapper = new PortMapper();
            mapper.Start();
        }
        private void ShowPackage()
        {
            if (Configuration.showPackage)
                Configuration.showPackage = false;
            else
                Configuration.showPackage = true;
        }
        private void StartChat()
        {
            Console.Write("Please enter a message:");
            string chatMsg = Console.ReadLine();
            chatMsg = "Message from " + Configuration.local.externalIP + ":" + chatMsg;
            if (Configuration.isServer)
            {
                ChatHandler handler = new ChatHandler(chatMsg);
                handler.Start();
            }
            else
            {
                ChatRehandler rehandler = new ChatRehandler(chatMsg);
                rehandler.Start();
            }
        }
        private void ShowLatency()
        {
            if (Rules.useRule)
            {
                Console.WriteLine("Can not test the latency while using proxy rules.");
                return;
            }
            Console.Write("Please enter a host and access it:");
            Configuration.testHost = Console.ReadLine();
        }
        private void StartSwitch()
        {
            if (Configuration.isServer)
            {
                Configuration.isServer = false;
                Configuration.serverHost = SetHost();
                Configuration.serverPort = SetPort();
                Configuration.local.clients = new HashSet<Client>();
                Configuration.chats = new HashSet<Socket>();
                Configuration.latencys = new HashSet<Socket>();
                Systemproxy.Set("127.0.0.1", Configuration.localPort.ToString());
                Thread.Sleep(1000);
                Task.Run(() =>
                {
                    StartConfig();
                });
                Console.WriteLine("Switched to a client");
            }
            else
            {
                Configuration.isServer = true;
                Configuration.topology = Configuration.local;
                Systemproxy.Unset();
                Thread.Sleep(1000);
                Task.Run(() =>
                {
                    StartConfig();
                });
                Console.WriteLine("Switched to a server");
            }
        }
        private void StartRule()
        {
            if (Rules.useRule)
                Rules.useRule = false;
            else
                Rules.useRule = true;
        }
    }
}
