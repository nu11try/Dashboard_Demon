using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        private Message message = new Message();
        private string request = "";
        private string response = "";
		private Logger logger = new Logger();

		Dictionary<string, string> elementXML = new Dictionary<string, string>();
        XmlDocument xDoc = new XmlDocument();

		private static ICollection<string> FindNext(string str, string resultPath)
		{
			var list = new LinkedList<string>();
			using (var reader = new StreamReader(resultPath, Encoding.Default))
			{
				while (!reader.EndOfStream)
				{
					var tmp = reader.ReadLine();
					if (tmp != null && tmp.Contains(str))
						list.AddLast(tmp);
				}
			}
			return list;
		}

		public XmlElement LoadFile(string resultPath)
        {
            XmlElement xRoot = null;
            if (StartTests.flager != 1)
            {
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
                    catch (Exception ex)
					{
						logger.WriteLog("Отказано в попытке прочитать файл по причине " + ex.Message, "ERROR");
						Console.WriteLine("Отказано");
					}
                }				
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
			ICollection<string> result = FindNext("<Result>Passed</Result>", resultPath);
			if (result.Count == 0) return "Failed";
			else return "Passed";

			/*string result = "";
            XmlElement xRoot = LoadFile(resultPath);
			if (xRoot == null)
			{
				Console.WriteLine("cannot_open");
				return "cannot_open";
			}
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
								Console.WriteLine("Result = " + result);
                            }
                        }
                    }
                }
            }
            return result;*/
        }
		public string ResultTest(string service, string nameTest, string resultPath, string data, string version, string stend)
		{
			// в конце - статус теста
			// каждый элемент - результат выполнения теста
			// в каждом элементе содержатся шаги и время
			// они идут через один (ШАГ ВРЕМЯ ШАГ...)
			// бывают момент, когда времени 2
			// 2-ое время - это потерянное время			
			/*XmlElement xRoot = LoadFile(resultPath);
			if (xRoot == null)
			{
				Console.WriteLine("cannot_open1");
				return ResultTest(service, nameTest, resultPath, data, "time_out", version, stend);
				//return "cannot_open";
			}
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
							if (dataChildren.Name == "StartTime")
							{
								dateofStart = dataChildren.InnerText;
							}
						}
					}
				}
			}*/
			List<Step> listSteps = new List<Step>();
			string result = TypeResultTest(resultPath);
			logger.WriteLog(nameTest + " = " + result);
			ICollection<string> Iduration = FindNext("<Duration>", resultPath);
			ICollection<string> Idate = FindNext("<StartTime>", resultPath);

			string[] Aduration = new string[Iduration.Count];
			string[] Adate = new string[Idate.Count];

			Iduration.CopyTo(Aduration, 0);
			Idate.CopyTo(Adate, 0);

			string duration = Aduration[Aduration.Length - 1].Split('<')[1].Split('>')[1];
			string dateofStart = Adate[Adate.Length - 1].Split('<')[1].Split('>')[1];

			Steps steps1 = new Steps();
			Message mess = new Message();
			/*steps1.innerSteps = new List<List<string>>();
			steps1.name = new List<string>();*/
			/*for (int i = 0; i < listSteps.Count; i++)
			{
				steps1.name.Add(listSteps[i].name);
				steps1.innerSteps.Add(listSteps[i].innerSteps);
				mess.Add(listSteps[i].time);
			}*/

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

			message.Add(DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss"));
			message.Add(Convert.ToDateTime(dateofStart).ToString("dd MMMM yyyy | HH:mm:ss"));
			message.Add(Convert.ToDateTime(dateofStart).AddSeconds(Int32.Parse(duration)).ToString("dd MMMM yyyy | HH:mm:ss"));
			request = JsonConvert.SerializeObject(message);
			response = database.SendMsg("AddStatisticDemon", service, request);

			return result;
		}
		public string ResultTest(string service, string nameTest, string resultPath, string data, string options, string version, string stend)
		{
			if (StartTests.flager != 1)
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
				else if (options == "error")
				{
					message.Add("ERROR");
					message.Add("ERROR");
					message.Add("ERROR");
					message.Add("ERROR");
				}
				else if (options == "Failed")
				{
					message.Add("Failed");
					message.Add("Failed");
					message.Add("Failed");
					message.Add("Failed");
				}
				else if (options == "no_verson")
				{
					message.Add("no_verson");
					message.Add("no_verson");
					message.Add("no_verson");
					message.Add("no_verson");
				}
				else if (options == "no_file")
				{
					message.Add("MISSING RESULT FILE");
					message.Add("MISSING RESULT FILE");
					message.Add("MISSING RESULT FILE");
					message.Add("MISSING RESULT FILE");
				}
				message.Add(data);
				message.Add(version);
				message.Add(stend);

				message.Add("-");
				message.Add("-");
				message.Add("-");

				request = JsonConvert.SerializeObject(message);
				Console.WriteLine("dependon = " + request + "\n");
				response = database.SendMsg("AddStatisticDemon", service, request);
			}
			return "Failed";
		}
		public void ClearScreenshot(string path)
		{
			try
			{
				Directory.Delete(path.Replace("\\Res1\\Report\\run_results.xml", "\\ScreenShots\\"), true);
			}
			catch { }
		}
		public void MakeScreenshot(string path)
		{
			try
			{
				path = path.Replace("\\Res1\\Report\\run_results.xml", "\\ScreenShots\\");
				Console.WriteLine("Путь к снимку " + path);
				string ext = "png";
				if (!Directory.Exists(path)) Directory.CreateDirectory(path);
				Size screenSz = Screen.PrimaryScreen.Bounds.Size;
				Bitmap screenshot = new Bitmap(screenSz.Width, screenSz.Height);
				Graphics gr = Graphics.FromImage(screenshot);
				gr.CopyFromScreen(Point.Empty, Point.Empty, screenSz);
				string filepath = path + "error." + ext;
				List<PropertyInfo> props = typeof(ImageFormat).GetProperties(BindingFlags.Static | BindingFlags.Public).ToList();
				var imgformat = (ImageFormat)props.Find(prop => prop.Name.ToLower() == ext).GetValue(null, null);
				screenshot.Save(filepath, imgformat);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Проблема с созданием скриншота! " + ex.Message);
			}
		}
	}
}
