using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Demon
{
    public class ClientObject
    {
        public TcpClient client;
        StartTest startTest = new StartTest();
        public ClientObject(TcpClient tcpClient)
        {
            client = tcpClient;
        }

        public void Process()
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] data = new byte[5042]; // буфер для получаемых данных
                                              // получаем сообщение
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);

                string[] result = new string[] { };
                result = builder.ToString().Split('@');
                List<string> response = new List<string>();
                string msg = "";
                foreach (var el in result)
                {
                    Console.WriteLine(el);
                }
                if (result.Length > 0)
                {
                    switch (result[0])
                    {
                        case "startPackTests":
                            //msg = startTest.Start(result[1], result[2]);
                            break;
                    }
                }
                data = Encoding.Unicode.GetBytes(msg);
                stream.Write(data, 0, data.Length);
                builder.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }
    }
}
