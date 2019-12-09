using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Demon
{
    public class Step
    {
        public string name { get; set; }
        public List<string> innerSteps { get; set; }
        public string time { get; set; }
    }
    public class Steps
    {
        public List<string> name { get; set; }
        public List<List<string>> innerSteps { get; set; }
    }
    public class FileSystem
    {        
        private DataBaseConnect database = new DataBaseConnect();
        private SQLiteCommand command;
        private string query = "";
        private Message message = new Message();
        private string request = "";
        private string response = "";

        Dictionary<string, string> elementXML = new Dictionary<string, string>();
        XmlNode attr;
        XmlDocument xDoc = new XmlDocument();

        public XmlElement LoadFile(string resultPath)
        {
            XmlElement xRoot = null;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("Попытка загрузить файл");
                    xDoc.Load(resultPath);
                    xRoot = xDoc.DocumentElement;
                    Console.WriteLine("Файл загружен");
                    break;
                }
                catch { Console.WriteLine("Отказано"); }
            }
            return xRoot;
        }
        public string ReadFileConfig()
        {
            string textFromFile;
            using (FileStream fstream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\config.cfg"))
            {
                byte[] array = new byte[fstream.Length];
                fstream.Read(array, 0, array.Length);
                textFromFile = System.Text.Encoding.Default.GetString(array);          
            }
            return textFromFile;
        }
        public string TypeResultTest(string resultPath)
        {
            string result = "";
            XmlElement xRoot = LoadFile(resultPath);
            foreach (XmlNode xNode in xRoot)
            {
                foreach (XmlNode children in xNode.ChildNodes)
                {
                    if (children.Name == "Data")
                    {
                        foreach (XmlNode dataChildren in children.ChildNodes)
                        {
                            if (dataChildren.Name == "Result")
                            {
                                result = dataChildren.InnerText;
                            }
                        }
                    }
                }
            }
            return result;
        }
        public string ResultTest(string service, string nameTest, string resultPath, string data, string version, string stend)
        {
            // в конце - статус теста
            // каждый элемент - результат выполнения теста
            // в каждом элементе содержатся шаги и время
            // они идут через один (ШАГ ВРЕМЯ ШАГ...)
            // бывают момент, когда времени 2
            // 2-ое время - это потерянное время
            List<Step> listSteps = new List<Step>();
            string duration = "";
            string result = "";
            XmlElement xRoot = LoadFile(resultPath);
            int flag = 0;
            Step step = new Step();
            step.innerSteps = new List<string>();
            foreach (XmlNode xNode in xRoot)
            {
                foreach (XmlNode children in xNode.ChildNodes)
                {
                    if (children.Name == "ReportNode")
                    {
                        foreach (XmlNode reports in children.ChildNodes)
                        {
                            foreach (XmlNode steps in reports.ChildNodes)
                            {
                                if (steps.Name != "Data")
                                {
                                    foreach (XmlNode datas in steps.ChildNodes)
                                    {
                                        foreach (XmlNode dataCh in datas.ChildNodes)
                                        {
                                            if (dataCh.Name == "Name" && flag == 0 && dataCh.InnerText.StartsWith("Step"))
                                            {
                                                flag = 1;
                                                step = new Step();
                                                step.innerSteps = new List<string>();
                                                step.name = dataCh.InnerText;

                                            }
                                            else
                                            if (dataCh.Name == "Name" && flag == 1 && dataCh.InnerText.StartsWith("Step"))
                                            {
                                                listSteps.Add(step);
                                                flag = 0;

                                            }
                                            if (dataCh.Name == "Name" && !dataCh.InnerText.StartsWith("Step") && flag == 1)
                                            {
                                                step.innerSteps.Add(dataCh.InnerText);
                                                if (dataCh.InnerText.Contains("Stop Run"))
                                                {
                                                    flag = 0;
                                                    step.time = dataCh.InnerText;
                                                    listSteps.Add(step);
                                                }
                                            }
                                            if (dataCh.Name == "Description" && dataCh.InnerText.Contains("Total Duration:"))
                                            {
                                                string dur = dataCh.InnerText.Substring(dataCh.InnerText.LastIndexOf("Total Duration: "));
                                                step.time = dur.Split(' ')[2];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (children.Name == "Data")
                    {
                        foreach (XmlNode dataChildren in children.ChildNodes)
                        {
                            if (dataChildren.Name == "Duration")
                            {
                                duration = dataChildren.InnerText;
                            }
                            if (dataChildren.Name == "Result")
                            {
                                result = dataChildren.InnerText;
                            }
                        }
                    }
                }
            }
            Steps steps1 = new Steps();
            steps1.innerSteps = new List<List<string>>();
            steps1.name = new List<string>();
            Message mess = new Message();
            for (int i = 0; i < listSteps.Count; i++)
            {
                steps1.name.Add(listSteps[i].name);
                steps1.innerSteps.Add(listSteps[i].innerSteps);
                mess.Add(listSteps[i].time);
            }

            message = new Message();

            message.Add(nameTest);
            message.Add(nameTest);
            message.Add(result);
            message.Add(JsonConvert.SerializeObject(mess));
            message.Add(duration);
            message.Add("0");
            message.Add(JsonConvert.SerializeObject(steps1));
            message.Add(data);
            message.Add(version);
            message.Add(stend);

            request = JsonConvert.SerializeObject(message);
            response = database.SendMsg("AddStatisticDemon", service, request);

            return result;
        }
        public string ResultTest(string service, string nameTest, string resultPath, string data, string options, string version, string stend)
        {
            message = new Message();

            message.Add(nameTest);
            message.Add(nameTest);
            message.Add("Failed");

            if (options == "dependen_error")
            {
                message.Add("DEPENDEN ERROR");
                message.Add("DEPENDEN ERROR");
                message.Add("DEPENDEN ERROR");
                message.Add("DEPENDEN ERROR");
            }
            else if (options == "time_out")
            {
                message.Add("TIMEOUT");
                message.Add("TIMEOUT");
                message.Add("TIMEOUT");
                message.Add("TIMEOUT");
            }
            else if (options == "no_verson")
            {
                message.Add("no_verson");
                message.Add("no_verson");
                message.Add("no_verson");
                message.Add("no_verson");
            }
            message.Add(data);
            message.Add(version);
            message.Add(stend);

            request = JsonConvert.SerializeObject(message);
            Console.WriteLine("dependon = " + request + "\n");
            response = database.SendMsg("AddStatisticDemon", service, request);
            return "Failed";
        }
    }
}
