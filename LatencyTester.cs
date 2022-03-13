using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace test
{
    public class LatencyTester
    {
        private string recv;
        private Socket remoteSocket;
        private byte[] buffer = new byte[4096];
        private Stopwatch timer;
        public LatencyTester(string recv)
        {
            this.recv = recv;
        }
        public void Dispose()
        {
            try
            {
                timer.Stop();
                Configuration.delay[0] = -1;
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
            DirectTester tester = new DirectTester(recv);
            tester.Start();
            if(!Configuration.isServer)
                ProxyStart();
        }
        private void ProxyStart()
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
                byte[] brs = Encoding.ASCII.GetBytes(recv);
                timer = Stopwatch.StartNew();
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int Ret = remoteSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    Dispose();
                }
                else
                {
                    timer.Stop();
                    Configuration.delay[0] = timer.ElapsedMilliseconds;
                }
            }
            catch
            {
                Dispose();
            }
        }
        public void TestComplate()
        {
            if (Configuration.isServer)
            {
                while (Configuration.delay[1] == -2)
                {
                    Thread.Sleep(1000);
                    if (!Configuration.isServer)
                        return;
                }
            }
            else
            {
                while (Configuration.delay[0] == -2 || Configuration.delay[1] == -2)
                {
                    Thread.Sleep(1000);
                    if (Configuration.isServer)
                        return;
                }
            }
            if (Configuration.isServer)
            {
                ChatHandler handler = new ChatHandler("Latency test from " + Configuration.local.externalIP + ": direct:" + Configuration.delay[1]);
                handler.Start();
            }
            else if (!Rules.useRule)
            {
                ChatRehandler rehandler = new ChatRehandler("Latency test from " + Configuration.local.externalIP + ": proxy:" + Configuration.delay[0] + " direct:" + Configuration.delay[1]);
                rehandler.Start();
            }
            else
            {
                if (Configuration.delay[0] >= 0 && Configuration.delay[1] >= 0)
                {
                    if (Configuration.delay[1] < Configuration.delay[0] - 20)
                        Rules.directs.Add(Configuration.tempHost);
                    else
                        Rules.proxys.Add(Configuration.tempHost);
                }
                else if (Configuration.delay[1] == -1 && Configuration.delay[0] >= 0)
                    Rules.proxys.Add(Configuration.tempHost);
                else if (Configuration.delay[0] == -1 && Configuration.delay[1] >= 0)
                    Rules.directs.Add(Configuration.tempHost);
            }
            Configuration.delay[0] = -2;
            Configuration.delay[1] = -2;
            TestComplate();
        }
    }
    public class DirectTester
    {
        private Socket remoteSocket;
        private byte[] buffer = new byte[4096];
        private string recv;
        private Stopwatch timer;
        public DirectTester(string recv)
        {
            this.recv = recv;
        }
        public void Dispose()
        {
            try
            {
                timer.Stop();
                Configuration.delay[1] = -1;
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
            HttpHandler handler = new HttpHandler(null, new HttpCompleteDelegate(this.OnEndHttpProtocol));
            handler.ProcessQuery(recv);
        }
        private void OnEndHttpProtocol(Socket clientSocket, Socket destinationSocket)
        {
            this.remoteSocket = destinationSocket;
            byte[] brs = Encoding.ASCII.GetBytes(recv);
            timer = Stopwatch.StartNew();
            try
            {
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int Ret = remoteSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    Dispose();
                }
                else
                {
                    timer.Stop();
                    Configuration.delay[1] = timer.ElapsedMilliseconds;
                }
            }
            catch
            {
                Dispose();
            }
        }
    }
    public class LatencyReceiver
    {
        private Socket remoteSocket;
        private byte[] buffer = new byte[1024];
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
                StartLatency();
            }
            catch
            {
                Dispose();
            }
        }
        private void StartLatency()
        {
            string brs = "LATENCY_";
            try
            {
                remoteSocket.BeginSend(Encoding.ASCII.GetBytes(brs), 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnLatencySent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnLatencySent(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnLatencyReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnLatencyReceive(IAsyncResult ar)
        {
            int Ret;
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
            string recv = Encoding.ASCII.GetString(buffer, 0, Ret);
            recv = Configuration.Decrypt(recv);
            LatencyTester tester = new LatencyTester(recv);
            tester.Start();
            if (Configuration.latencys.Count > 0)
            {
                LatencyHandler handler = new LatencyHandler(recv);
                handler.Start();
            }
            try
            {
                remoteSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnLatencyReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
    public class LatencyHandler
    {
        private string delays;
        public LatencyHandler(string delays)
        {
            this.delays = Configuration.Encrypt(delays);
        }
        public void Start()
        {
            byte[] brs = Encoding.ASCII.GetBytes(delays);
            HashSet<Socket> deadSockets = new HashSet<Socket>();
            foreach (Socket clientSocket in Configuration.latencys)
            {
                try
                {
                    clientSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnLatencySent), clientSocket);
                }
                catch
                {
                    deadSockets.Add(clientSocket);
                }
            }
            foreach (Socket dead in deadSockets)
            {
                Configuration.latencys.Remove(dead);
            }
        }
        private void OnLatencySent(IAsyncResult ar)
        {
            Socket clientSocket = (Socket)ar.AsyncState;
            try
            {
                clientSocket.EndSend(ar);
            }
            catch { }
        }
    }
    public class LatencyRehandler
    {
        private string delays;
        private Socket remoteSocket;
        public LatencyRehandler(string delays)
        {
            this.delays = Configuration.Encrypt(delays);
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
                StartSendLatency();
            }
            catch
            {
                Dispose();
            }
        }
        private void StartSendLatency()
        {
            byte[] brs = Configuration.AddBytes(Encoding.ASCII.GetBytes("LATENCY_"), Encoding.ASCII.GetBytes(delays));
            try
            {
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnLatencySent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnLatencySent(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }
    }
}
