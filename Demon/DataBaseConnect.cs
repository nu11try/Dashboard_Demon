using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Demon
{
    class DataBaseConnect
    {
        private Request request = new Request();
        string bufJSON;

        const int port = 8888;
        //const string address = "172.31.197.89";
        const string address = "172.31.197.232";

        string nameText = "";

        public string SendMsg(string msg, string service)
        {
            request.Add(msg, service, "");
            bufJSON = JsonConvert.SerializeObject(request);
            nameText = DateTime.Now.ToString("ddMMyyyyhhmmssfff");
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + nameText, bufJSON);
            return ConnectServer(bufJSON, nameText);
        }

        public string SendMsg(string msg, string service, string param)
        {
            request.Add(msg, service, param);
            bufJSON = JsonConvert.SerializeObject(request);
            nameText = DateTime.Now.ToString("ddMMyyyyhhmmfffss");
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + nameText, bufJSON);
            return ConnectServer(bufJSON, nameText);
        }

        private string ConnectServer(string json, string nameText)
        {
            TcpClient client = null;
            StringBuilder builder = new StringBuilder();
            string response = "";
            try
            {
                client = new TcpClient(address, port);
                NetworkStream stream = client.GetStream();
                byte[] data = File.ReadAllBytes(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + nameText);

                File.Delete(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + nameText);

                int bufferSize = 1024;
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                stream.Write(dataLength, 0, 4);
                int bytesSent = 0;
                int bytesLeft = data.Length;
                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    stream.Write(data, bytesSent, curDataSize);
                    bytesSent += curDataSize;
                    bytesLeft -= curDataSize;
                }
                nameText = DateTime.Now.ToString("MMddyyyyhhmmssfff");

                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, data);
                string param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + nameText).Replace("\n", " ");
                File.Delete(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + nameText);


                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, 4);
                int dataLengthResponse = BitConverter.ToInt32(fileSizeBytes, 0);
                bytesLeft = dataLengthResponse;
                data = new byte[dataLengthResponse];
                int bytesRead = 0;
                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    if (client.Available < curDataSize)
                        curDataSize = client.Available; //This saved me
                    bytes = stream.Read(data, bytesRead, curDataSize);
                    bytesRead += curDataSize;
                    bytesLeft -= curDataSize;
                }

                nameText = DateTime.Now.ToString("ddyyyyhhMMmmssfff");

                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, data);
                param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + nameText).Replace("\n", " ");
                File.Delete(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + nameText);
                response = param;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(0);
            }
            request = new Request();
            bufJSON = "";
            return response;
        }
    }

    public class Request
    {
        public Request()
        {
            args = new List<string>();
        }

        public void Add(params string[] tmp)
        {
            for (int i = 0; i < tmp.Length; i++)
            {
                args.Add(tmp[i]);
            }
        }
        public List<string> args { get; set; }
    }
}
