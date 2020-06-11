using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Demon
{
    public class ClientObject
    {
        public TcpClient client;
        Controller controller = new Controller();

        private Logger logger = new Logger();
        string nameText = "";
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
                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, 4);
                int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);
                int bytesLeft = dataLength;
                byte[] data = new byte[dataLength];
                int bufferSize = 1024;
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
                nameText = "\\" + DateTime.Now.ToString("ddMMyyyyhhmmssfff");
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, data);
                string param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + nameText).Replace("\n", " ");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
                string buf = controller.transformation(param);

                nameText = "\\" + DateTime.Now.ToString("ddMMyyyyhhmmfffss");
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, Encoding.UTF8.GetBytes(buf));
                data = File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText);
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
                byte[] dataLengthResponse = BitConverter.GetBytes(data.Length);
                stream.Write(dataLengthResponse, 0, 4);
                int bytesSent = 0;
                bytesLeft = data.Length;
                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    stream.Write(data, bytesSent, curDataSize);
                    bytesSent += curDataSize;
                    bytesLeft -= curDataSize;
                }

                File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                logger.WriteLog("Не удалось прочитать/отправить транспортное сообщение по причине " + ex.Message, "ERROR");
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
