using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Demon
{
    public class Server
    {
        //const int port = 8888;
        //const string ip = "172.31.197.232";
        //const string ip = "127.0.0.1";                   

        static TcpListener listener;
        static DataBaseConnect database = new DataBaseConnect();
        static SQLiteCommand command;
        static string query = "";

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
    public class Message
    {
        public Message() { args = new List<string>(); }
        public void Add(params string[] tmp)
        {
            for (int i = 0; i < tmp.Length; i++) args.Add(tmp[i]);
        }
        public List<string> args { get; set; }
    }
    public class Tests
    {
        public Tests()
        {
            id = new List<string>();
            start = new List<string>();
            time = new List<string>();
            dependon = new List<string>();
            restart = new List<string>();
            browser = new List<string>();
            duplicate = new List<string>();
        }
        public List<string> id { get; set; }
        public List<string> start { get; set; }
        public List<string> time { get; set; }
        public List<string> dependon { get; set; }
        public List<string> restart { get; set; }
        public List<string> browser { get; set; }
        public List<string> duplicate { get; set; }
    }
}
