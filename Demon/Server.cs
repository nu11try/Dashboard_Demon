using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Demon
{
    public class Server
    {
        static int port = 8889;
        static string ip = "";

        static TcpListener listener;

        static class Data
        {
            public static string[] TestsForStart { get; set; }
        }

        static void Main(string[] args)
        {
            //ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();            
            ip = "172.17.42.32";
            try
            {
                listener = new TcpListener(IPAddress.Parse(ip), port);
                listener.Start();
                Console.WriteLine("===================================");
                Console.WriteLine("Произведен запуск демона для Asylum!");
                Console.WriteLine("IP машины - " + ip);
                try
                {
                    Console.WriteLine("Версия - " + ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString());
                }
                catch
                {
                    Console.WriteLine("Версия - unknown");
                }
                Console.WriteLine("Демон готов принимать поручения");
                Console.WriteLine("==================================");

                if (!Directory.Exists(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\test\\"))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\test\\");
                }

                try
                {
                    Awake awake = new Awake();
                    AwakeDemon(awake);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Таск на пробуждение отвалился по причине " + ex.Message);
                }

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ClientObject clientObject = new ClientObject(client);

                    // создаем новый поток для обслуживания нового клиента
                    //Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    //clientThread.Start();
                    ConnectClient(clientObject);
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
        static async void ConnectClient(ClientObject clientObject)
        {
            await Task.Run(()=>clientObject.Process());
        }
        static async void AwakeDemon(Awake awake)
        {
            await Task.Run(() => awake.Init());
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
