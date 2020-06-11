using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using System.Data.Entity;

namespace Demon
{
    public class PackStart
    {
        public PackStart()
        {
            TestsInPack = new Tests();
            ResultTest = new Dictionary<string, string>();
            ResultFolders = new List<string>();
            FilesToStart = new List<string>();
            VersionStends = new List<string>();
        }

        public string Name = "";
        public string Service = "";
        public string IP = "";
        public string Restart = "";
        public string Browser = "";
        public string Time = "";
        public string Stend = "";
        public string PathToTests = "";

        public Tests TestsInPack;
        public Dictionary<string, string> ResultTest;
        public List<string> ResultFolders;
        public List<string> FilesToStart;
        public List<string> VersionStends;
    }
    public class Options
    {
        public string file = "";
        public PackStart pack;
    }
    public class TestPackNow : PackStart
    {
        public string Data = "";
        public string Version = "";
        public string Result = "";
    }

    public class StartTests
    {
        public Regex myReg;
        public Match match;

        private TestPackNow now = new TestPackNow();

        private DataBaseConnect database = new DataBaseConnect();

        private Message message;

        private Logger logger = new Logger();
        private FileSystem fs = new FileSystem();

        Process StartTest = new Process();
        TimerCallback tm;
        Timer timer;
        private Message Response = new Message();
        private List<PackStart> packs = new List<PackStart>();

        private bool FlagStarted;
        private int SeconsdEnd;

        private string request = "";
        private string response = "";

        private FreeRAM freeRAM = new FreeRAM();
        public static int flager = 0;

        public void Init(object RESPONSE)
        {            
            flager = 0;
            string data = DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss");

            Response = (Message)RESPONSE;

            if (Response.args.Count > 0)
            {
                try
                {
                    for (int i = 0; i < Response.args.Count; i += 9)
                    {
                        PackStart pack = new PackStart();
                        pack.Name = Response.args[i + 1];
                        pack.Service = Response.args[i];
                        pack.Browser = Response.args[i + 6];
                        pack.Restart = Response.args[i + 7];
                        pack.Stend = Response.args[i + 8];
                        pack.Time = Response.args[i + 4];
                        pack.IP = Response.args[i + 3].Split(' ')[2];
                        pack.PathToTests = JsonConvert.DeserializeObject<Message>(Response.args[i + 2]).args[0];
                        pack.TestsInPack = JsonConvert.DeserializeObject<Tests>(Response.args[i + 5]);

                        ConfigStartTest(pack, data);
                        packs.Add(pack);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка разбора набора " + ex.Message);
                }
                Console.WriteLine("Производится удаление папок");
                packs.ForEach(pack =>
                {
                    for (int i = 0; i < pack.TestsInPack.id.Count; i++)
                    {
                        if (pack.TestsInPack.restart[i].Equals("default"))
                            pack.TestsInPack.restart[i] = pack.Restart;
                        if (pack.TestsInPack.time[i].Equals("default"))
                            pack.TestsInPack.time[i] = pack.Time;

                        int indexFolder = 1;
                        while (true)
                        {
                            try
                            {
                                Console.WriteLine("\\\\pur-test01\\\\ATST" + pack.PathToTests.Replace("Z:", "") + "\\\\" + pack.TestsInPack.id[i] + "\\\\Res" + indexFolder);
                                Directory.Delete("\\\\pur-test01\\\\ATST" + pack.PathToTests.Replace("Z:", "") + "\\\\" + pack.TestsInPack.id[i] + "\\\\Res" + indexFolder, true);
                                indexFolder++;
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLog("Не удалось удалить папку \\\\pur-test01\\\\ATST" + pack.PathToTests.Replace("Z:", "") + "\\\\" + pack.TestsInPack.id[i] + "\\\\Res" + indexFolder
                                        + " по причине " + ex.Message, "ERROR");
                                break;
                            }
                        }
                    }
                });
                Console.WriteLine("Удаление папок завершено");
            }
            else return;

            packs.ForEach(pack =>
            {
                int indexElement = 0;
                for (int i = 0; i < pack.TestsInPack.id.Count; i++)
                {
                    if (flager == 1) return;
                    while (Int32.Parse(pack.TestsInPack.restart[indexElement]) >= 0)
                    {
                        CloseUFT();
                        if (flager == 1) return;
                        Message message = new Message();

                        message.Add(pack.IP, pack.TestsInPack.id[i]);
                        request = JsonConvert.SerializeObject(message);
                        response = database.SendMsg("updateTestsNow", pack.Service, request);
                        FlagStarted = true;
                        string ver = "";

                        myReg = new Regex(@"http:\/\/.*\/");
                        ver = GetVersionStend(myReg.Match(pack.Stend).Value);
                        message = new Message();
                        message.Add(pack.Service, ver, data);
                        pack.VersionStends.Add(ver);
                        Console.WriteLine("ver = " + ver);

                        if (ver == "no_version")
                        {
                            if (Int32.Parse(pack.TestsInPack.restart[indexElement]) == 0)
                            {
                                if (!pack.ResultTest.ContainsKey(pack.TestsInPack.id[indexElement]))
                                    pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "no_verson", pack.VersionStends[indexElement], pack.Stend));
                                else
                                    pack.ResultTest[pack.TestsInPack.id[indexElement]] = fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "no_verson", pack.VersionStends[indexElement], pack.Stend);
                            }
                            pack.TestsInPack.restart[indexElement] = (Int32.Parse(pack.TestsInPack.restart[indexElement]) - 1).ToString();
                            FlagStarted = true;
                            logger.WriteLog("Не получена версия стенда", "WARNING");
                            continue;
                        }
                        else if (!ver.Equals("no_version"))
                        {

                            string bufDependons = JsonConvert.DeserializeObject<Message>(pack.TestsInPack.dependon[indexElement]).args[0];
                            try
                            {
                                if (pack.ResultTest[bufDependons].Equals("Failed"))
                                {
                                    if (!pack.ResultTest.ContainsKey(pack.TestsInPack.id[indexElement]))
                                        pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "dependen_error", pack.VersionStends[indexElement], pack.Stend));
                                    else
                                        pack.ResultTest[pack.TestsInPack.id[indexElement]] = fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "dependen_error", pack.VersionStends[indexElement], pack.Stend);

                                    FlagStarted = true;
                                    break;
                                }
                            }
                            catch { }

                            fs.ClearScreenshot(pack.ResultFolders[indexElement]);

                            StartScript(pack.FilesToStart[indexElement], pack);
                            pack.TestsInPack.restart[indexElement] = (Int32.Parse(pack.TestsInPack.restart[indexElement]) - 1).ToString();
                            FlagStarted = true;                            

                            try
                            {                                
                                string bufResult = fs.TypeResultTest(pack.ResultFolders[indexElement]);

                                logger.WriteLog("Результат теста " + pack.ResultFolders[indexElement] + " - " + fs.TypeResultTest(pack.ResultFolders[indexElement]), "RESULT");

                                if (bufResult.Equals("Passed") || bufResult.Equals("Warning") || bufResult.Equals("Failed"))
                                {
                                    if (!pack.ResultTest.ContainsKey(pack.TestsInPack.id[indexElement]))
                                        pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, pack.VersionStends[indexElement], pack.Stend));
                                    else
                                        pack.ResultTest[pack.TestsInPack.id[indexElement]] = fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, pack.VersionStends[indexElement], pack.Stend);
                                    FlagStarted = true;
                                    if (bufResult.Equals("Passed") || bufResult.Equals("Warning")) break;
                                    else continue;
                                }
                                else if (bufResult.Equals("cannot_open"))
                                {
                                    fs.MakeScreenshot(pack.ResultFolders[indexElement]);
                                    if (!pack.ResultTest.ContainsKey(pack.TestsInPack.id[indexElement]))
                                        pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "time_out", pack.VersionStends[indexElement], pack.Stend));
                                    else
                                        pack.ResultTest[pack.TestsInPack.id[indexElement]] = fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "time_out", pack.VersionStends[indexElement], pack.Stend);
                                    FlagStarted = true;
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLog("Ошибка при запуске тестов " + ex.Message, "ERROR");

                                string opt;

                                // ЭТОТ БЛОК НУЖЕН, ЕСЛИ ФАЙЛ XML НЕ БЫЛ СФОРМИРОВАН, НЕ УДАЛЯТЬ
                                if (File.Exists(pack.ResultFolders[indexElement])) opt = "Failed";
                                else opt = "no_file";

                                fs.MakeScreenshot(pack.ResultFolders[indexElement]);
                                if (!pack.ResultTest.ContainsKey(pack.TestsInPack.id[indexElement]))
                                    pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, opt, pack.VersionStends[indexElement], pack.Stend));
                                else
                                    pack.ResultTest[pack.TestsInPack.id[indexElement]] = fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, opt, pack.VersionStends[indexElement], pack.Stend);
                                FlagStarted = true;
                                continue;
                                //-------------------------                                
                            }
                        }
                    }
                    Console.WriteLine("Тест " + pack.FilesToStart[indexElement] + " выполнен!");
                    logger.WriteLog("Выполнен тест " + pack.FilesToStart[indexElement], "END");
                    FlagStarted = true;

                    indexElement++;
                    //File.Delete(pack.FilesToStart[indexElement]);
                }
                message = new Message();

                try
                {
                    message.Add(pack.IP, "not");
                    request = JsonConvert.SerializeObject(message);
                    logger.WriteLog("Обновление теста " + request);
                    response = database.SendMsg("updateTestsNow", pack.Service, request);
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Ошибка в обновлении тестов по причине " + ex.Message, "ERROR");
                }

                try
                {
                    message = new Message();
                    message.Add(pack.Service);
                    request = JsonConvert.SerializeObject(message);
                    logger.WriteLog("Удаление автостарта " + request);
                    response = database.SendMsg("DeleteAutostart", pack.Service, request);
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Ошибка в удаления автостарта по причине " + ex.Message, "ERROR");
                }

                try
                {
                    message = new Message();
                    message.Add(pack.Name, pack.Service);
                    request = JsonConvert.SerializeObject(message);
                    logger.WriteLog("Обновление статуса автостарта " + request);
                    response = database.SendMsg("UpdateStatusAutostart", pack.Service, request);
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Ошибка в обновлении статуса автостарта по причине " + ex.Message, "ERROR");
                }

                try
                {
                    message = new Message();
                    message.Add(pack.Name);
                    request = JsonConvert.SerializeObject(message);
                    logger.WriteLog("Обновление статуса набора " + request);
                    response = database.SendMsg("UpdateStatusPack", pack.Service, request);
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Ошибка в обновлении статуса набора по причине " + ex.Message, "ERROR");
                }

                FlagStarted = true;
                Finish(pack);

                try
                {
                    Directory.Delete(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\test\\");
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Невозможно удалить папку с vbs тестов по причине " + ex.Message, "ERROR");
                }
            });
        }
        public void Stop(object RESPONSE)
        {
            CloseUFT();

            Message message = new Message();
            DateTime time = DateTime.Now;

            int sec = time.DayOfYear * 24 * 60 * 60 + time.Hour * 60 * 60 + time.Minute * 60 + time.Second;
            message.Add(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString(), "not", "" + sec);

            database.SendMsg("updateTestsNow", "-", JsonConvert.SerializeObject(message));

            flager = 1;
            FlagStarted = false;            
        }
        static public void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            StreamReader reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();
            content = Regex.Replace(content, searchText, "\"" + replaceText + "\"");
            StreamWriter writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }
        public void ConfigStartTest(PackStart pack, string data)
        {
            for (int i = 0; i < pack.TestsInPack.id.Count; i++)
            {
                try
                {
                    if (pack.TestsInPack.duplicate[i] == "not")
                    {
                        File.Copy(AppDomain.CurrentDomain.BaseDirectory + "\\startTests.vbs",
                            AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs", true);

                        ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs",
                            "AddressHost", pack.Stend);
                        Console.WriteLine(pack.Browser.ToUpper());
                        if (pack.TestsInPack.browser[i].Equals("default") || pack.TestsInPack.browser[i].Equals("По умолчанию"))
                            ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs",
                                "BrowserName", pack.Browser.ToUpper());
                        else ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs",
                                "BrowserName", pack.TestsInPack.browser[i].ToUpper());

                        using (FileStream fstream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs", FileMode.Append))
                        {
                            byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "Z:" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.id[i] + "\", \"" + "Z:" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.id[i] + "\\Res1\\" + "\")");
                            fstream.Write(array, 0, array.Length);

                            pack.FilesToStart.Add(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs");
                            pack.ResultFolders.Add("Z:" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\") + "\\" + pack.TestsInPack.id[i] + "\\Res1\\Report\\run_results.xml");
                        }
                    }
                    else
                    {
                        File.Copy(AppDomain.CurrentDomain.BaseDirectory + "\\startTests.vbs",
                            AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs", true);
                        ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs",
                            "AddressHost", pack.Stend);

                        if (pack.TestsInPack.browser[i].Equals("default") || pack.TestsInPack.browser[i].Equals("По умолчанию"))
                            ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs",
                                "BrowserName", pack.Browser.ToUpper());
                        else ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs",
                                "BrowserName", pack.TestsInPack.browser[i].ToUpper());

                        using (FileStream fstream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs", FileMode.Append))
                        {
                            byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "Z:" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.duplicate[i] + "\", \"" + "Z:" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.duplicate[i] + "\\Res" + i + "\\" + "\")");
                            fstream.Write(array, 0, array.Length);

                            pack.FilesToStart.Add(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs");
                            pack.ResultFolders.Add("Z:\\" + pack.PathToTests.Replace("Z:" + "\\", "\\").Replace("\\" + "\\", "\\") + "\\" + pack.TestsInPack.duplicate[i] + "\\Res" + i + "\\Report\\run_results.xml");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    logger.WriteLog("Упал запуск тестов по причине " + ex.Message, "ERROR");
                }
            }
        }
        public void Finish(PackStart pack)
        {
            CloseUFT();
            logger.WriteLog("[СТАТУС НАБОРА ОБНОВЛЕН] " + pack.Name, "START");
            Console.WriteLine("Статус набора " + pack.Name + " обновлен!");
        }
        public void StartScript(string file, PackStart pack)
        {
            SeconsdEnd = 0;
            try
            {
                StartTest = new Process();
                StartTest.StartInfo.FileName = file;
                StartTest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                StartTest.StartInfo.UseShellExecute = true;
                StartTest.StartInfo.LoadUserProfile = true;
                StartTest.Start();
                
                tm = new TimerCallback(TimeOut);
                Options options = new Options();
                options.file = file;
                options.pack = pack;
                timer = new Timer(tm, options, 5000, 1000);
                
                StartTest.WaitForExit();
            }
            catch (Exception ex)
            {
                logger.WriteLog("Функция StartScript(string file, PackStart pack) выдала ошибку по причине " + ex.Message, "ERROR");
            }
        }
        public void TimeOut(object obj)
        {
            try
            {                
                if (flager == 1) FlagStarted = false;
                else
                {
                    Options options = (Options)obj;
                    string fileStarted = options.file;
                    PackStart pack = options.pack;
                    Console.WriteLine("Секунд прошло = " + SeconsdEnd);
                    if (SeconsdEnd >= Int32.Parse(pack.TestsInPack.time[pack.FilesToStart.IndexOf(fileStarted.ToString())]) && FlagStarted) FlagStarted = false;
                    else if (!FlagStarted)
                    {
                        try { SeconsdEnd = 0; } catch { }
                        FlagStarted = false;
                    }
                    else SeconsdEnd++;
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog("Функция TimeOut(object obj) выдала ошибку по причине " + ex.Message, "ERROR");
            }
        }
        public void CloseUFT()
        {
            CloseProc();
            try { timer.Dispose(); }
            catch (Exception ex) { logger.WriteLog("Не удалось произвести timer.Dispose() CloseUFT() по причине " + ex.Message, "ERROR"); }
            try { StartTest.Refresh(); }
            catch (Exception ex) { logger.WriteLog("Не удалось произвести StartTest.Refresh() CloseUFT() по причине " + ex.Message, "ERROR"); }
            try { StartTest.Dispose(); }
            catch (Exception ex) { logger.WriteLog("Не удалось произвести StartTest.Dispose() CloseUFT() по причине " + ex.Message, "ERROR"); }
            try { StartTest.Kill(); }
            catch (Exception ex) { logger.WriteLog("Не удалось произвести StartTest.Kill() CloseUFT() по причине " + ex.Message, "ERROR"); }            
            try { StartTest.Close(); }
            catch (Exception ex) { logger.WriteLog("Не удалось произвести StartTest.Close() CloseUFT() по причине " + ex.Message, "ERROR"); }
            try { SeconsdEnd = 0; }
            catch (Exception ex) { logger.WriteLog("Не удалось произвести SeconsdEnd = 0 по причине " + ex.Message, "ERROR"); }
            FlagStarted = false;
        }
        public void CloseProc()
        {
            try { foreach (Process proc in Process.GetProcessesByName("iexplore")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс iexplore по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("firefox")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс firefox по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("JinnClient")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс JinnClient по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("java")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс java по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("plugin-container")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс plugin-container по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("phantomjs")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс phantomjs по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("chrome")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс chrome по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("Mediator64")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс Mediator64 по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("UFT")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс UFT по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("QtpAutomationAgent")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс QtpAutomationAgent по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("wscript")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс wscript по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("JinnSignExtensionProvider")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс JinnSignExtensionProvider по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("ICheck")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс ICheck по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("EXCEL")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс EXCEL по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }

            try { foreach (Process proc in Process.GetProcessesByName("HP.UFT.Chrome.NativeMessagingHost")) proc.Kill(); }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось остановить процесс HP.UFT.Chrome.NativeMessagingHost по причине " + ex.Message, "ERROR");
                Console.WriteLine(ex.Message);
            }
        }
        public void DeleteResDirectories(String nameTest, String dir)
        {
            try
            {
                String[] tmp = dir.Split('\\');
                String dirs = tmp[0] + "\\" + tmp[2] + "\\" + tmp[3] + "\\" + nameTest;
                Console.WriteLine(dirs);
                string[] ress = Directory.GetDirectories(dirs);
                foreach (string res in ress)
                {
                    tmp = res.Split('\\');
                    if (tmp[tmp.Length - 1].StartsWith("Res"))
                    {
                        logger.WriteLog("Происходит удаление папки " + res);
                        DirectoryInfo dirInfo = new DirectoryInfo(res);
                        dirInfo.Delete(true);
                        Console.WriteLine(res);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось найти папку на удаление или произвести удаление по причине " + ex.Message, "ERROR");
            }
        }
        public string GetVersionStend(string stend)
        {
            string result = "";
            try
            {
                WebRequest req = WebRequest.Create(stend + "sufdversion");
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader sr = new System.IO.StreamReader(stream);
                string html = sr.ReadToEnd();
                sr.Close();
                myReg = new Regex(@"\d.*");
                match = myReg.Match(html);
                result += match.Value.Split('&')[0];
                myReg = new Regex(@"<b>.*");
                match = myReg.Match(match.Value.Split('&')[1]);
                result += " " + match.Value.Substring(3);
                logger.WriteLog("Версия стенда " + stend + "sufdversion - " + result);
            }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось получить версию стенда по причине " + ex.Message, "ERROR");
                result = "no_version";
            }
            return result;
        }
    }
}