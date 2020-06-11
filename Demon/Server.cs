using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demon
{
    public class Server
    {
        static int port = 8889;
        static string ip = "";

        static TcpListener listener;

        private static Logger logger = new Logger();
        private static FileSystem fs = new FileSystem();

        static class Data
        {
            public static string[] TestsForStart { get; set; }
        }

        static void Main(string[] args)
        {
            ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
            //ip = "172.17.42.32";
            try
            {
                listener = new TcpListener(IPAddress.Parse(ip), port);
                listener.Start();
                logger.WriteLog("Демон запущен", "LOAD");
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

                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\test\\"))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\test\\");
                    }
                }
                catch { }

                try
                {
                    Awake awake = new Awake();
                    AwakeDemon(awake);
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Таск на пробуждение отвалился по причине " + ex.Message, "ERROR");
                    Console.WriteLine("Таск на пробуждение отвалился по причине " + ex.Message);
                }

                while (true)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        ClientObject clientObject = new ClientObject(client);

                        // создаем новый поток для обслуживания нового клиента
                        //Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                        //clientThread.Start();
                        ConnectClient(clientObject);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Проблема в цикле принятия клиентов");
                        logger.WriteLog("Проблема в цикле принятия клиентов " + ex.Message, "ERROR");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка обработки клиента по причине " + ex.Message);
                logger.WriteLog("Произошла ошибка обработки клиента по причине " + ex.Message, "ERROR");

                //-----
                /*string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Process.Start(path);
                Process.GetCurrentProcess().Kill();*/
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }
        static async void ConnectClient(ClientObject clientObject)
        {
            await Task.Run(() => clientObject.Process());
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
