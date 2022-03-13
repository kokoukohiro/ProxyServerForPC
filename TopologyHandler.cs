using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace test
{
    class IPAsker
    {
        private Socket remoteSocket;
        public void Dispose()
        {
            try
            {
                remoteSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            if (remoteSocket != null)
                remoteSocket.Close();
            //Clean up
            remoteSocket = null;
        }
        public void Start()
        {
            try
            {
                IPAddress serverHost = IPAddress.Parse(Configuration.serverHost);
                remoteSocket = new Socket(serverHost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                remoteSocket.BeginConnect(new IPEndPoint(serverHost, Configuration.serverPort), new AsyncCallback(this.OnConnected), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndConnect(ar);
                SendAskIP();
            }
            catch
            {
                Dispose();
            }
        }
        private void SendAskIP()
        {
            string brs = "ASKFORIP";
            try
            {
                remoteSocket.BeginSend(Encoding.ASCII.GetBytes(brs), 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnAskSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnAskSent(IAsyncResult ar)
        {
            byte[] buf = new byte[1024];
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(this.OnAskReceive), buf);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnAskReceive(IAsyncResult ar)
        {
            int Ret;
            byte[] buf = (byte[])ar.AsyncState;
            try
            {
                Ret = remoteSocket.EndReceive(ar);
            }
            catch
            {
                Ret = -1;
            }
            if (Ret <= 0)
            { //Connection is dead :(
                Dispose();
                return;
            }
            String recv = Encoding.ASCII.GetString(buf, 0, Ret);
            IPAddress externalIP;
            if (IPAddress.TryParse(recv, out externalIP))
                Configuration.local.externalIP = externalIP.ToString();
            TopologyHandler handler = new TopologyHandler(false);
            handler.Start();
        }
    }
    public class TopologyHandler
    {
        private Socket remoteSocket;
        private bool show;
        public TopologyHandler(bool show)
        {
            this.show = show;
        }
        public void Dispose()
        {
            try
            {
                remoteSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            if (remoteSocket != null)
                remoteSocket.Close();
            //Clean up
            remoteSocket = null;
        }
        public void Start()
        {
            try
            {
                IPAddress serverHost = IPAddress.Parse(Configuration.serverHost);
                remoteSocket = new Socket(serverHost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                remoteSocket.BeginConnect(new IPEndPoint(serverHost, Configuration.serverPort), new AsyncCallback(this.OnConnected), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndConnect(ar);
                StartSendClients();
            }
            catch
            {
                Dispose();
            }
        }
        private void StartSendClients()
        {
            byte[] brs = Configuration.AddBytes(Encoding.ASCII.GetBytes("TOPOLOGY"), Configuration.ObjectToByteArray(Configuration.local));
            try
            {
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnTopoSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnTopoSent(IAsyncResult ar)
        {
            byte[] buf = new byte[4096];
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(this.OnTopoReceive), buf);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnTopoReceive(IAsyncResult ar)
        {
            int Ret;
            byte[] buf = (byte[])ar.AsyncState;
            try
            {
                Ret = remoteSocket.EndReceive(ar);
            }
            catch
            {
                Ret = -1;
            }
            if (Ret <= 0)
            { //Connection is dead :(
                Dispose();
                return;
            }
            Array.Resize(ref buf, Ret);
            Client client = (Client)Configuration.ByteArrayToObject(buf);
            Configuration.topology = client;
            if (show)
                ShowTopo();
        }
        private void GetTopo(Client client, int num)
        {
            foreach (Client c in client.clients)
            {
                Console.Write(new string(' ', 2 * num));
                Console.WriteLine("└ " + c.externalIP);
                if (c.clients.Count > 0)
                {
                    GetTopo(c, num + 1);
                }
            }
        }
        public void ShowTopo()
        {
            Console.WriteLine(Configuration.topology.externalIP);
            GetTopo(Configuration.topology, 1);
        }
    }
    public class TopologyRehandler
    {
        private Socket remoteSocket;
        private Socket clientSocket;
        public TopologyRehandler(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
        }
        public void Dispose()
        {
            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                remoteSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            if (clientSocket != null)
                clientSocket.Close();
            if (remoteSocket != null)
                remoteSocket.Close();
            //Clean up
            clientSocket = null;
            remoteSocket = null;
        }
        public void Start()
        {
            try
            {
                IPAddress serverHost = IPAddress.Parse(Configuration.serverHost);
                remoteSocket = new Socket(serverHost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                remoteSocket.BeginConnect(new IPEndPoint(serverHost, Configuration.serverPort), new AsyncCallback(this.OnConnected), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndConnect(ar);
                StartSendClients();
            }
            catch
            {
                Dispose();
            }
        }
        private void StartSendClients()
        {
            byte[] brs = Configuration.AddBytes(Encoding.ASCII.GetBytes("TOPOLOGY"), Configuration.ObjectToByteArray(Configuration.local));
            try
            {
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnTopoSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnTopoSent(IAsyncResult ar)
        {
            byte[] buf = new byte[4096];
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(this.OnTopoReceive), buf);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnTopoReceive(IAsyncResult ar)
        {
            int Ret;
            byte[] buf = (byte[])ar.AsyncState;
            try
            {
                Ret = remoteSocket.EndReceive(ar);
            }
            catch
            {
                Ret = -1;
            }
            if (Ret <= 0)
            { //Connection is dead :(
                Dispose();
                return;
            }
            Array.Resize(ref buf, Ret);
            Client client = (Client)Configuration.ByteArrayToObject(buf);
            Configuration.topology = client;
            StartSendTopo();
        }
        private void StartSendTopo()
        {
            byte[] brs = Configuration.ObjectToByteArray(Configuration.topology);
            try
            {
                clientSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnTopoResent), clientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnTopoResent(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }
    }
}
