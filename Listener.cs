using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace test
{
    public class Listener
    {
        private Socket listenSocket;
        public void Start()
        {
            try
            {
                // Create a TCP/IP socket.
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                localEndPoint = new IPEndPoint(IPAddress.Any, Configuration.localPort);

                // Bind the socket to the local endpoint and listen for incoming connections.
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(1024);

                // Start an asynchronous socket to listen for connections.
                listenSocket.BeginAccept(new AsyncCallback(OnAccept), listenSocket);
            }
            catch (SocketException)
            {
                listenSocket.Close();
                throw;
            }
        }
        private void Stop()
        {
            if (listenSocket != null)
            {
                listenSocket.Close();
                listenSocket = null;
            }
        }
        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket acceptSocket = listenSocket.EndAccept(ar);  
                HeadHandler handler = new HeadHandler(acceptSocket);
                handler.Start();
            }
            catch
            {
            }
            try
            {
                //Restart Listening
                listenSocket.BeginAccept(new AsyncCallback(this.OnAccept), listenSocket);
            }
            catch
            {
                Stop();
            }
        }
    }
}
