using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using NATUPNPLib;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace test
{
    class PortMapper
    {
        private string externalIP;
        private delegate void WebAccessDelegate();
        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;
        private Int32 dwFlag = new Int32();
        [System.Runtime.InteropServices.DllImport("winInet.dll")]
        private static extern bool InternetGetConnectedState(ref int dwFlag, int dwReserved);

        public void GetExternalIP()
        {
            if (externalIP == null)
                webAccess("https://ipv4.icanhazip.com/");
            if (externalIP == null)
                webAccess("http://ipinfo.io/ip");
            if (externalIP == null)
                webAccess("http://bot.whatismyipaddress.com/");
            if (externalIP == null)
                webAccess("https://api.ipify.org/");
            if (externalIP == null)
                webAccess("https://icanhazip.com/");
            if (externalIP == null)
                webAccess("http://checkip.amazonaws.com/");
            if (externalIP == null)
                webAccess("https://wtfismyip.com/text");
            if (externalIP != null)
            {
                Configuration.local.externalIP = externalIP;
                if (Configuration.topology.externalIP == null)
                    Configuration.topology.externalIP = externalIP;
            }
        }
        private void webAccess(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Proxy = new WebProxy();
                WebResponse response = request.GetResponse();
                StreamReader stream = new StreamReader(response.GetResponseStream());
                externalIP = (stream.ReadToEnd()).
                    Replace("<html><head><title>Current IP Check</title></head><body>Current IP Address: ", string.Empty).
                    Replace("</body></html>", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
                stream.Close();
            }
            catch { }
        }
        private void StartPortMap()
        {
            UPnPNAT upnpnat = new UPnPNAT();
            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;
            if (mappings == null)
            {
                Console.WriteLine("Can not map the port,maybe UPnP disabled or no router found");
                return;
            }
            string port = Configuration.localPort.ToString();
            RemoveFrmNAT(port);
            if(AddFrmNAT(port))
                Console.WriteLine("Map to " + Configuration.local.externalIP + ":" + Configuration.localPort + " success");
        }
        private void RemoveFrmNAT(string port)
        {
            UPnPNAT upnpnat = new UPnPNAT();
            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;
            try
            {
                mappings.Remove(int.Parse(port), "TCP");
                //mappings.Remove(int.Parse(port), "UDP");
            }
            catch
            { }
        }
        private bool AddFrmNAT(string port)
        {
            UPnPNAT upnpnat = new UPnPNAT();
            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;
            try
            {
                mappings.Add(int.Parse(port), "TCP", int.Parse(port), GetLocalIP().ToString(), true, "personal proxy");
                //mappings.Add(int.Parse(port), "UDP", int.Parse(port), GetLocalIP(), true, "personal proxy");
                return true;
            }
            catch
            {
                Console.WriteLine("Can not map the port,the use of the UPnP feature in router,please map it manually");
                return false;
            }
        }
        public static IPAddress GetLocalIP()
        {
            try
            {
                IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ipAddress in he.AddressList)
                {
                    if (ipAddress.AddressFamily.ToString() == "InterNetwork")
                        return ipAddress;
                }
                return he.AddressList[0];
            }
            catch
            {
                return IPAddress.Any;
            }
        }
        public int TraceRoute()
        {
            // following are the defaults for the "traceroute" command in unix.
            const int timeout = 10000;
            const int maxTTL = 30;
            const int bufferSize = 32;
            int ttl;
            byte[] buffer = new byte[bufferSize];
            new Random().NextBytes(buffer);
            Ping pinger = new Ping();
            for (ttl = 1; ttl <= maxTTL; ttl++)
            {
                if (ttl > 2)
                    break;
                PingOptions options = new PingOptions(ttl, true);
                PingReply reply = pinger.Send(Configuration.local.externalIP, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    // Success means the tracert has completed
                    break;
                }
                if (reply.Status == IPStatus.TtlExpired)
                {
                    // TtlExpired means we've found an address, but there are more addresses
                    continue;
                }
                if (reply.Status == IPStatus.TimedOut)
                {
                    // TimedOut means this ttl is no good, we should continue searching
                    continue;
                }
            }
            return ttl;
        }
        public bool Start()
        {
            if (!Check())
                return false;
            if (Configuration.local.externalIP == null)
                GetExternalIP();
            bool isModem = (dwFlag & INTERNET_CONNECTION_MODEM) != 0;
            bool isRouter = (dwFlag & INTERNET_CONNECTION_LAN) != 0;
            int ttl = TraceRoute();
            //It is WAN
            if (ttl == 1 && isModem)
            {
                Console.WriteLine("Map to " + Configuration.local.externalIP + ":" + Configuration.localPort + "success");
                return true;
            }
            //There is only a router
            else if (ttl == 2 && isRouter)
            {
                StartPortMap();
                return true;
            }
            //else it can not use
            else
            {
                Console.WriteLine("Can not map the port,please map it manually");
                return true;
            }
        }
        public bool Check()
        {           
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                Console.WriteLine("Please connnect to the Internet");
                return false;
            }
            return true;
        }
    }
}
