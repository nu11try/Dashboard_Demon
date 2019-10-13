using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Demon
{
    class Server
    {
        //const int port = 8888;
        //const string ip = "172.31.197.232";
        //const string ip = "127.0.0.1";        
        static TcpListener listener;     

        static class Data
        {
            public static string[] TestsForStart { get; set; }
        }

        static void Main(string[] args)
        {
            FileSystem fileSystem = new FileSystem();
            string bufParamStart;
            
            try
            {
                bufParamStart = fileSystem.ReadFileConfig();
                listener = new TcpListener(IPAddress.Parse(bufParamStart.Split('%')[0]), Int32.Parse(bufParamStart.Split('%')[1]));
                listener.Start();
                Console.WriteLine("===================================");
                Console.WriteLine("Произведен запуск демона для Asylum!");
                Console.WriteLine("\n");
                Console.WriteLine("Демон готов принимать поручения");
                Console.WriteLine("==================================");

                while (true)
                {                   
                    TcpClient client = listener.AcceptTcpClient();
                    ClientObject clientObject = new ClientObject(client);

                    // создаем новый поток для обслуживания нового клиента
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }
    }
}
