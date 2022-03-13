using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Specialized;

namespace test
{
    internal delegate void HttpCompleteDelegate(Socket clientSocket,Socket destinationSocket);

    internal sealed class HttpHandler
    {
        private Socket clientSocket;
        private Socket destinationSocket;
        private byte[] buffer = new byte[4096];
        private StringDictionary HeaderFields = null;
        private string HttpVersion = "";
        private string HttpRequestType = "";
        private string RequestedPath = null;
        private string m_HttpPost = null;
        private HttpCompleteDelegate Signaler;
        public HttpHandler(Socket clientSocket, HttpCompleteDelegate Callback)
        {
            if (Callback == null)
                throw new ArgumentNullException();
            this.clientSocket = clientSocket;
            this.Signaler = Callback;
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
                destinationSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            if (clientSocket != null)
                clientSocket.Close();
            else
                Signaler(null,null);
            if (destinationSocket != null)
                destinationSocket.Close();
            //Clean up
            clientSocket = null;
            destinationSocket = null;
        }
        public bool IsValidQuery(string Query)
        {
            int index = Query.IndexOf("\r\n\r\n");
            if (index == -1)
                return false;
            HeaderFields = ParseQuery(Query);
            if (HttpRequestType.ToUpper().Equals("POST"))
            {
                try
                {
                    int length = int.Parse((string)HeaderFields["Content-Length"]);
                    return Query.Length >= index + 6 + length;
                }
                catch
                {
                    SendBadRequest();
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        public void ProcessQuery(string Query)
        {
            HeaderFields = ParseQuery(Query);
            if (HeaderFields == null || !HeaderFields.ContainsKey("Host"))
            {
                SendBadRequest();
                return;
            }
            int Port;
            string Host;
            int Ret;
            if (HttpRequestType.ToUpper().Equals("CONNECT"))
            { //HTTPS
                Ret = RequestedPath.IndexOf(":");
                if (Ret >= 0)
                {
                    Host = RequestedPath.Substring(0, Ret);
                    if (RequestedPath.Length > Ret + 1)
                        Port = int.Parse(RequestedPath.Substring(Ret + 1));
                    else
                        Port = 443;
                }
                else
                {
                    Host = RequestedPath;
                    Port = 443;
                }
            }
            else
            { //Normal HTTP
                Ret = ((string)HeaderFields["Host"]).IndexOf(":");
                if (Ret > 0)
                {
                    Host = ((string)HeaderFields["Host"]).Substring(0, Ret);
                    Port = int.Parse(((string)HeaderFields["Host"]).Substring(Ret + 1));
                }
                else
                {
                    Host = (string)HeaderFields["Host"];
                    Port = 80;
                }
                if (HttpRequestType.ToUpper().Equals("POST"))
                {
                    int index = Query.IndexOf("\r\n\r\n");
                    m_HttpPost = Query.Substring(index + 4);
                }
            }
            try
            {
                IPEndPoint destinationEndPoint = new IPEndPoint(Dns.GetHostEntry(Host).AddressList[0], Port);
                destinationSocket = new Socket(destinationEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (HeaderFields.ContainsKey("Proxy-Connection") && HeaderFields["Proxy-Connection"].ToLower().Equals("keep-alive"))
                    destinationSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                destinationSocket.BeginConnect(destinationEndPoint, new AsyncCallback(this.OnConnected), destinationSocket);
            }
            catch
            {
                SendBadRequest();
                return;
            }
        }
        private StringDictionary ParseQuery(string Query)
        {
            StringDictionary retdict = new StringDictionary();
            string[] Lines = Query.Replace("\r\n", "\n").Split('\n');
            int Cnt, Ret;
            //Extract requested URL
            if (Lines.Length > 0)
            {
                //Parse the Http Request Type
                Ret = Lines[0].IndexOf(' ');
                if (Ret > 0)
                {
                    HttpRequestType = Lines[0].Substring(0, Ret);
                    Lines[0] = Lines[0].Substring(Ret).Trim();
                }
                //Parse the Http Version and the Requested Path
                Ret = Lines[0].LastIndexOf(' ');
                if (Ret > 0)
                {
                    HttpVersion = Lines[0].Substring(Ret).Trim();
                    RequestedPath = Lines[0].Substring(0, Ret);
                }
                else
                {
                    RequestedPath = Lines[0];
                }
                //Remove http:// if present
                if (RequestedPath.Length >= 7 && RequestedPath.Substring(0, 7).ToLower().Equals("http://"))
                {
                    Ret = RequestedPath.IndexOf('/', 7);
                    if (Ret == -1)
                        RequestedPath = "/";
                    else
                        RequestedPath = RequestedPath.Substring(Ret);
                }
            }
            for (Cnt = 1; Cnt < Lines.Length; Cnt++)
            {
                Ret = Lines[Cnt].IndexOf(":");
                if (Ret > 0 && Ret < Lines[Cnt].Length - 1)
                {
                    try
                    {
                        retdict.Add(Lines[Cnt].Substring(0, Ret), Lines[Cnt].Substring(Ret + 1).Trim());
                    }
                    catch { }
                }
            }
            return retdict;
        }
        private void SendBadRequest()
        {
            string brs = "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>";
            try
            {
                clientSocket.BeginSend(Encoding.ASCII.GetBytes(brs), 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnErrorSent), clientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnErrorSent(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }
        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                destinationSocket.EndConnect(ar);
                string rq;
                if (HttpRequestType.ToUpper().Equals("CONNECT"))
                { //HTTPS
                    rq = HttpVersion + " 200 Connection established\r\nProxy-Agent: Personal Proxy Server\r\n\r\n";
                    if (clientSocket != null) 
                        clientSocket.BeginSend(Encoding.ASCII.GetBytes(rq), 0, rq.Length, SocketFlags.None, new AsyncCallback(this.OnOkSent), clientSocket);
                    else
                        Signaler(clientSocket, destinationSocket);
                }
                else
                { //Normal HTTP
                    rq = RebuildQuery();
                    destinationSocket.BeginSend(Encoding.ASCII.GetBytes(rq), 0, rq.Length, SocketFlags.None, new AsyncCallback(this.OnQuerySent), destinationSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }
        private string RebuildQuery()
        {
            string ret = HttpRequestType + " " + RequestedPath + " " + HttpVersion + "\r\n";
            if (HeaderFields != null)
            {
                foreach (string sc in HeaderFields.Keys)
                {
                    if (sc.Length < 6 || !sc.Substring(0, 6).Equals("proxy-"))
                        ret += System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sc) + ": " + (string)HeaderFields[sc] + "\r\n";
                }
                ret += "\r\n";
                if (m_HttpPost != null)
                    ret += m_HttpPost;
            }
            return ret;
        }
        private void OnQuerySent(IAsyncResult ar)
        {
            try
            {
                if (destinationSocket.EndSend(ar) == -1)
                {
                    Dispose();
                    return;
                }
                Signaler(clientSocket, destinationSocket);
            }
            catch
            {
                Dispose();
            }
        }
        private void OnOkSent(IAsyncResult ar)
        {
            try
            {
                if (clientSocket.EndSend(ar) == -1)
                {
                    Dispose();
                    return;
                }
                Signaler(clientSocket, destinationSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
}
