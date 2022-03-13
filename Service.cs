using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace test
{
    public class Service
    {
        private Socket remoteSocket;
        private Socket clientSocket;
        private byte[] buffer = new byte[4096];
        private byte[] remoteBuffer = new byte[4096];
        public Service(Socket clientSocket,Socket remoteSocket)
        {
            this.clientSocket = clientSocket;
            this.remoteSocket = remoteSocket;
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
        public void StartRelay()
        {
            try
            {
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), clientSocket);
                remoteSocket.BeginReceive(remoteBuffer, 0, remoteBuffer.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), remoteSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnClientReceive(IAsyncResult ar)
        {
            try
            {
                int Ret = clientSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    Dispose();
                    return;
                }
                remoteSocket.BeginSend(buffer, 0, Ret, SocketFlags.None, new AsyncCallback(this.OnRemoteSent), remoteSocket);
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
                int Ret = remoteSocket.EndSend(ar);
                if (Ret > 0)
                {
                    clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), clientSocket);
                    return;
                }
            }
            catch { }
            Dispose();
        }
        private void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int Ret = remoteSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    Dispose();
                    return;
                }
                clientSocket.BeginSend(remoteBuffer, 0, Ret, SocketFlags.None, new AsyncCallback(this.OnClientSent), clientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnClientSent(IAsyncResult ar)
        {
            try
            {
                int Ret = clientSocket.EndSend(ar);
                if (Ret > 0)
                {
                    remoteSocket.BeginReceive(remoteBuffer, 0, remoteBuffer.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), remoteSocket);
                    return;
                }
            }
            catch { }
            Dispose();
        }
    }
}
