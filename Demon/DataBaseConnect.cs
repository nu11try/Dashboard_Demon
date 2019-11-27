using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Demon
{
    class DataBaseConnect
    {
        private Request request = new Request();
        string bufJSON;

        const int port = 8888;
        const string address = "172.31.197.232";

        public string SendMsg(string msg, string service)
        {
            request.Add(msg, service, "");
            bufJSON = JsonConvert.SerializeObject(request);
            return ConnectServer(bufJSON);
        }

        public string SendMsg(string msg, string service, string param)
        {
            request.Add(msg, service, param);
            bufJSON = JsonConvert.SerializeObject(request);
            return ConnectServer(bufJSON);
        }

        private string ConnectServer(string json)
        {
            TcpClient client = null;
            StringBuilder builder = new StringBuilder();
            string response = "";
            try
            {
                client = new TcpClient(address, port);
                NetworkStream stream = client.GetStream();

                // преобразуем сообщение в массив байтов
                byte[] data = new byte[] { };
                data = Encoding.Unicode.GetBytes(json);

                // отправка сообщения
                stream.Write(data, 0, data.Length);

                // получаем ответ
                data = new byte[9999999]; // буфер для получаемых данных

                int bytes = 0;

                bytes = stream.Read(data, 0, data.Length);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                response = builder.ToString();

                builder.Clear();
                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
