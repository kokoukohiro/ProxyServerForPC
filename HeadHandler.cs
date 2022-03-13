using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    class HeadHandler
    {
        private Socket clientSocket;
        private Socket remoteSocket;
        private byte[] buffer = new byte[4096];
        private string HttpQuery = "";
        public HeadHandler(Socket clientSocket)
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
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveQuery), clientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnReceiveQuery(IAsyncResult ar)
        {
            int ret;
            try
            {
                ret = clientSocket.EndReceive(ar);
            }
            catch
            {
                ret = -1;
            }
            if (ret <= 0)
            { //Connection is dead :(
                Dispose();
                return;
            }
            string recv = Encoding.ASCII.GetString(buffer, 0, ret);
            string head;
            try
            {
                head = recv.Substring(0, 8);
            }
            catch
            {
                head = recv;
            }
            switch (head)
            {
                case "TOPOLOGY":
                    EditTopo(buffer,ret);
                    break;
                case "ASKFORIP":
                    AnswerIP();
                    break;
                case "CHATINFO":
                    if(recv.Length==8)
                        Configuration.chats.Add(clientSocket);
                    if (recv.Length > 8)
                        HandleChat(buffer,ret);
                    break;
                case "LATENCY_":
                    if (recv.Length == 8)
                        Configuration.latencys.Add(clientSocket);
                    if (recv.Length > 8)
                        HandleLatency(buffer, ret);
                    break;
                default:
                    try
                    {                      
                        recv = Configuration.Decrypt(recv);
                    }
                    catch { }
                    HttpHandler handler = new HttpHandler(clientSocket, new HttpCompleteDelegate(this.OnEndHttpProtocol));
                    HttpQuery += recv;
                    if (!handler.IsValidQuery(HttpQuery))
                    {
                        try
                        {
                            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveQuery), clientSocket);
                        }
                        catch
                        {
                            Dispose();
                        }
                        return;
                    }
                    if (Configuration.showPackage)
                        Console.Write(recv);
                    if (Configuration.testHost != null && recv.IndexOf(Configuration.testHost) >= 0)
                    {
                        Configuration.testHost = null;
                        if (Configuration.isServer)
                        {
                            LatencyHandler latency = new LatencyHandler(recv);
                            latency.Start();
                            LatencyTester tester = new LatencyTester(recv);
                            tester.Start();
                        }
                        else
                        {
                            LatencyRehandler rehandler = new LatencyRehandler(recv);
                            rehandler.Start();
                        }
                    }
                    if (!Configuration.isServer && Rules.useRule && recv.IndexOf("Host") >= 0) 
                    {
                        string host = recv.Substring(recv.IndexOf("Host") + 6);
                        host = host.Substring(0, host.IndexOf("\r\n"));
                        try
                        {
                            host = host.Substring(0, host.IndexOf(":"));
                        }
                        catch { }
                        if (Rules.directs.Contains(host))
                        {
                            handler.ProcessQuery(recv);
                            return;
                        }
                        else if (!Rules.proxys.Contains(host))
                        {
                            Configuration.tempHost = host;
                            LatencyTester tester = new LatencyTester(recv);
                            Task.Run(() =>
                            {
                                tester.Start();
                            });
                        }
                    }
                    if (Configuration.isServer)
                        handler.ProcessQuery(HttpQuery);
                    else
                        StartConnect(recv);
                    break;
            }
        }
        private void OnEndHttpProtocol(Socket clientSocket, Socket destinationSocket)
        {
            Service service = new Service(clientSocket, destinationSocket);
            service.StartRelay();
        }
        private void StartConnect(string recv)
        {
            try
            {
                IPAddress serverHost = IPAddress.Parse(Configuration.serverHost);
                remoteSocket = new Socket(serverHost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                remoteSocket.BeginConnect(new IPEndPoint(serverHost, Configuration.serverPort), new AsyncCallback(this.OnConnected), recv);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnConnected(IAsyncResult ar)
        {
            string recv = (string)ar.AsyncState;
            byte[] brs = Encoding.ASCII.GetBytes(Configuration.Encrypt(recv));
            try
            {
                remoteSocket.EndConnect(ar);
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnSent), remoteSocket);                
            }
            catch
            {
                Dispose();
            }
        }
        private void OnSent(IAsyncResult ar)
        {
            try
            {
                int Ret = remoteSocket.EndSend(ar);
                if (Ret > 0)
                {
                    Service service = new Service(clientSocket, remoteSocket);
                    service.StartRelay();
                }
            }
            catch
            {
                Dispose();
            }            
        }
        private void AnswerIP()
        {
            string brs = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
            try
            {
                clientSocket.BeginSend(Encoding.ASCII.GetBytes(brs), 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnAnswerSent), clientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnAnswerSent(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }
        private void EditTopo(byte[] buf,int ret)
        {
            Array.Resize(ref buf, ret);
            buf = CutHead(buf, 8);
            Client client = (Client)Configuration.ByteArrayToObject(buf);
            Configuration.local = CutTopo(Configuration.local,client);
            ReplaceTopo(client);
            if (Configuration.isServer)
            {
                Configuration.topology = Configuration.local;
                StartSendTopo();
            }
            else
            {
                TopologyRehandler rehandler = new TopologyRehandler(clientSocket);
                rehandler.Start();
            }
        }
        private void StartSendTopo()
        {
            byte[] brs = Configuration.ObjectToByteArray(Configuration.topology);
            try
            {
                clientSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnTopoSent), clientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnTopoSent(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }
        private byte[] CutHead(byte[] bytes, int n)
        {
            byte[] newBytes = new byte[bytes.Length - n];
            Array.Copy(bytes, n, newBytes, 0, newBytes.Length);
            return newBytes;
        }
        private void ReplaceTopo(Client client)
        {
            Client toReplace = null;
            foreach (Client c in Configuration.local.clients)
            {
                if (c.Equals(client))
                    toReplace = c;
            }
            if (toReplace != null)
                Configuration.local.clients.Remove(toReplace);
            Configuration.local.clients.Add(client);
        }
        private Client CutTopo(Client client, Client newClient)
        {
            Client toCut = client;
            foreach (Client c in client.clients)
            {
                if (c.externalIP.Equals(newClient.externalIP))
                {
                    toCut.clients.Remove(c);
                    return toCut;
                }
                if (c.clients.Count > 0)
                    CutTopo(c, newClient);
            }
            return client;
        }
        private void HandleChat(byte[] buf,int ret)
        {
            Array.Resize(ref buf, ret);
            buf = CutHead(buf, 8);
            string recv = Encoding.UTF8.GetString(buf, 0, ret-8);
            if (Configuration.isServer)
            {
                ChatHandler handler = new ChatHandler(recv);
                handler.Start();
            }
            else
            {
                ChatRehandler rehandler = new ChatRehandler(recv);
                rehandler.Start();
            }
        }
        private void HandleLatency(byte[] buf, int ret)
        {
            Array.Resize(ref buf, ret);
            buf = CutHead(buf, 8);
            string recv = Encoding.ASCII.GetString(buf, 0, ret - 8);
            recv = Configuration.Decrypt(recv);
            if (Configuration.isServer)
            {
                LatencyHandler latency = new LatencyHandler(recv);
                latency.Start();
                LatencyTester tester = new LatencyTester(recv);
                tester.Start();
            }
            else
            {
                LatencyRehandler rehandler = new LatencyRehandler(recv);
                rehandler.Start();
            }
        }
    }
}
