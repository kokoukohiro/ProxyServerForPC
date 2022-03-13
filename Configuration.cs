using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Security.Cryptography;

namespace test
{
    [Serializable]
    public class Client
    {
        public string externalIP;
        public HashSet<Client> clients = new HashSet<Client>();
        public Client() { }
        public Client(string externalIP)
        {
            this.externalIP = externalIP;
        }
        public override int GetHashCode()
        {
            return this.externalIP.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            try
            {
                Client client = obj as Client;
                return this.externalIP.Equals(client.externalIP);
            }
            catch
            {
                return true;
            }
        }
    }
    public static class Rules
    {
        public static bool useRule = false;
        public static HashSet<string> directs = new HashSet<string>();
        public static HashSet<string> proxys = new HashSet<string>();
    }
    public static class Configuration
    {
        public static string serverHost;
        public static int serverPort;
        public static int localPort;
        public static bool isServer;
        public static bool showPackage = false;
        public static string testHost;
        public static string tempHost;
        public static long[] delay = new long[] { -2, -2 };
        public static Client local = new Client();
        public static Client topology = new Client();
        public static HashSet<Socket> chats = new HashSet<Socket>();
        public static HashSet<Socket> latencys = new HashSet<Socket>();

        // Convert an object to a byte array
        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            memStream.Position = 0;
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }
        public static byte[] AddBytes(byte[] bytes,byte[] newBytes)
        {
            Array.Resize(ref bytes, bytes.Length + newBytes.Length);
            Array.Copy(newBytes, 0, bytes, bytes.Length - newBytes.Length, newBytes.Length);
            return bytes;
        }

        public static string Encrypt(string text)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(@"pf69DL6GrWFyZcMK");
                rijndael.Key = Encoding.UTF8.GetBytes(@"9Fix4L4HB4PKeKWY");

                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }
        public static string Decrypt(string cipher)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(@"pf69DL6GrWFyZcMK");
                rijndael.Key = Encoding.UTF8.GetBytes(@"9Fix4L4HB4PKeKWY");

                ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(System.Convert.FromBase64String(cipher)))
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadToEnd();
                        }
                    }
                }
                return plain;
            }
        }
    }
}
