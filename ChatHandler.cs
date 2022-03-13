using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace test
{
    public class ChatReceiver
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
                StartChat();
            }
            catch
            {
                Dispose();
            }
        }
        private void StartChat()
        {
            string brs = "CHATINFO";
            try
            {
                remoteSocket.BeginSend(Encoding.ASCII.GetBytes(brs), 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnChatSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnChatSent(IAsyncResult ar)
        {
            try
            {
                remoteSocket.EndSend(ar);
                remoteSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnChatReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnChatReceive(IAsyncResult ar)
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
            string recv = Encoding.UTF8.GetString(buffer, 0, Ret);
            Console.WriteLine(recv);
            if (Configuration.chats.Count > 0)
            {
                ChatHandler handler = new ChatHandler(recv);
                handler.Start();
            }
            try
            {
                remoteSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnChatReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
    public class ChatHandler
    {
        private string chatMsg;
        public ChatHandler(string chatMsg)
        {
            this.chatMsg = chatMsg;
        }
        public void Start()
        {   
            if(Configuration.isServer)
                Console.WriteLine(chatMsg);
            byte[] brs = Encoding.UTF8.GetBytes(chatMsg);
            HashSet<Socket> deadSockets = new HashSet<Socket>();
            foreach (Socket clientSocket in Configuration.chats)
            {
                try
                {
                    clientSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnChatSent), clientSocket);
                }
                catch
                {
                    deadSockets.Add(clientSocket);
                }
            }
            foreach (Socket dead in deadSockets)
            {
                Configuration.chats.Remove(dead);
            }
        }
        private void OnChatSent(IAsyncResult ar)
        {
            Socket clientSocket = (Socket)ar.AsyncState;
            try
            {
                clientSocket.EndSend(ar);
            }
            catch { }
        }
    }
    public class ChatRehandler
    {
        private string chatMsg;
        private Socket remoteSocket;
        public ChatRehandler(string chatMsg)
        {
            this.chatMsg = chatMsg;
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
                StartSendChat();
            }
            catch
            {
                Dispose();
            }
        }
        private void StartSendChat()
        {
            byte[] brs = Configuration.AddBytes(Encoding.ASCII.GetBytes("CHATINFO"), Encoding.UTF8.GetBytes(chatMsg));
            try
            {
                remoteSocket.BeginSend(brs, 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnChatSent), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnChatSent(IAsyncResult ar)
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
