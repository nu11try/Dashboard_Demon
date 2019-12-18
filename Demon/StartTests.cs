﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Data.SQLite;
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

    public class StartTests
    {
        public Regex myReg;
        public Match match;

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
                packs.ForEach(pack =>
                {
                    for (int i = 0; i < pack.TestsInPack.id.Count; i++)
                    {
                        if (pack.TestsInPack.restart[i].Equals("default"))
                            pack.TestsInPack.restart[i] = pack.Restart;
                        if (pack.TestsInPack.time[i].Equals("default"))
                            pack.TestsInPack.time[i] = pack.Time;
                    }
                });
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
                        if (flager == 1) return;
                        Message message = new Message();
                        DateTime time1 = DateTime.Now;
                        int sec1 = time1.DayOfYear * 24 * 60 * 60 + time1.Hour * 60 * 60 + time1.Minute * 60 + time1.Second;
                        message.Add(pack.IP, pack.TestsInPack.id[i], ""+ sec1);
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
                            if (Int32.Parse(pack.TestsInPack.restart[indexElement]) < 0)
                                pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "no_verson", pack.VersionStends[indexElement], pack.Stend));

                            pack.TestsInPack.restart[indexElement] = (Int32.Parse(pack.TestsInPack.restart[indexElement]) - 1).ToString();
                            FlagStarted = true;
                            continue;
                        }

                        if (!ver.Equals("no_version"))
                        {
                            string bufDependons = JsonConvert.DeserializeObject<Message>(pack.TestsInPack.dependon[indexElement]).args[0];
                            try
                            {
                                if (pack.ResultTest[bufDependons].Equals("Failed"))
                                {
                                    pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "dependen_error", pack.VersionStends[indexElement], pack.Stend));
                                    FlagStarted = true;
                                    break;
                                }
                            }
                            catch { }

                            StartScript(pack.FilesToStart[indexElement], pack);
                            pack.TestsInPack.restart[indexElement] = (Int32.Parse(pack.TestsInPack.restart[indexElement]) - 1).ToString();
                            FlagStarted = true;

                            try
                            {
                                if (fs.TypeResultTest(pack.ResultFolders[indexElement]).Equals("Passed") || fs.TypeResultTest(pack.ResultFolders[indexElement]).Equals("Warning"))
                                {
                                    pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, pack.VersionStends[indexElement], pack.Stend));
                                    FlagStarted = true;
                                    break;
                                }
                                if (fs.TypeResultTest(pack.ResultFolders[indexElement]).Equals("Failed"))
                                {
                                    if (Int32.Parse(pack.TestsInPack.restart[indexElement]) < 0)
                                        pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, pack.VersionStends[indexElement], pack.Stend));
                                    FlagStarted = true;
                                    continue;
                                }
                            }
                            catch
                            {
                                pack.ResultTest.Add(pack.TestsInPack.id[indexElement], fs.ResultTest(pack.Service, pack.TestsInPack.id[indexElement], pack.ResultFolders[indexElement], data, "time_out", pack.VersionStends[indexElement], pack.Stend));
                                FlagStarted = true;
                                continue;
                            }
                        }
                    }

                    Console.WriteLine("Тест " + pack.FilesToStart[indexElement] + " выполнен!");
                    logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + pack.FilesToStart[indexElement], "START");
                    FlagStarted = true;

                    indexElement++;
                    //File.Delete(pack.FilesToStart[indexElement]);
                }
                message = new Message();
                DateTime time = DateTime.Now;
                int sec = time.DayOfYear * 24 * 60 * 60 + time.Hour * 60 * 60 + time.Minute * 60 + time.Second;
                message.Add(pack.IP, "not", "" + sec);
                request = JsonConvert.SerializeObject(message);
                response = database.SendMsg("updateTestsNow", pack.Service, request);

                message = new Message();
                message.Add(pack.Service);
                request = JsonConvert.SerializeObject(message);
                response = database.SendMsg("DeleteAutostart", pack.Service, request);

                message = new Message();
                message.Add(pack.Name, pack.Service);
                request = JsonConvert.SerializeObject(message);
                response = database.SendMsg("UpdateStatusAutostart", pack.Service, request);

                message = new Message();
                message.Add(pack.Name);
                request = JsonConvert.SerializeObject(message);
                Console.WriteLine("Update status pack = " + request);
                response = database.SendMsg("UpdateStatusPack", pack.Service, request);

                FlagStarted = true;
                Finish(pack);
            });
        }
        public void Stop(object RESPONSE)
        {
            Message message = new Message();
            DateTime time = DateTime.Now;
            int sec = time.DayOfYear * 24 * 60 * 60 + time.Hour * 60 * 60 + time.Minute * 60 + time.Second;
            message.Add(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString(), "not", "" + sec);
            request = JsonConvert.SerializeObject(message);
            response = database.SendMsg("updateTestsNow", "-", request);
            flager = 1;
            Response = (Message)RESPONSE;
            CloseProc();
            CloseUFT();
            FlagStarted = false;
            try { timer.Dispose(); } catch { }
            try { StartTest.Kill(); } catch { }
            try { StartTest.Close(); } catch { }
            try { SeconsdEnd = 0; } catch { }
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
                            byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.id[i] + "\", \"" + "Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.id[i] + "\\Res1\\" + "\")");
                            fstream.Write(array, 0, array.Length);

                            pack.FilesToStart.Add(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs");
                            pack.ResultFolders.Add("Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\") + "\\" + pack.TestsInPack.id[i] + "\\Res1\\Report\\run_results.xml");
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
                            byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.duplicate[i] + "\", \"" + "Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + pack.TestsInPack.duplicate[i] + "\\Res" + i + "\\" + "\")");
                            fstream.Write(array, 0, array.Length);

                            pack.FilesToStart.Add(AppDomain.CurrentDomain.BaseDirectory + "test\\" + pack.TestsInPack.id[i] + ".vbs");
                            pack.ResultFolders.Add("Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\") + "\\" + pack.TestsInPack.duplicate[i] + "\\Res" + i + "\\Report\\run_results.xml");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + ex.Message, "ERROR");

                }
            }
        }
        public void Finish(PackStart pack)
        {
            CloseProc();
            CloseUFT();

            logger.WriteLog("[СТАТУС НАБОРА ОБНОВЛЕН] " + pack.Name, "START");
            Console.WriteLine("Статус набора " + pack.Name + " обновлен!");
            freeRAM.Free();
        }
        public void StartScript(string file, PackStart pack)
        {
            CloseProc();
            CloseUFT();

            SeconsdEnd = 0;

            StartTest.StartInfo.FileName = file;
            StartTest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartTest.StartInfo.UseShellExecute = true;
            StartTest.StartInfo.LoadUserProfile = true;
            StartTest.Start();

            Console.WriteLine("Ждем поток");
            tm = new TimerCallback(TimeOut);
            Options options = new Options();
            options.file = file;
            options.pack = pack;
            timer = new Timer(tm, options, 1000, 1000);

            StartTest.WaitForExit();
        }
        public void TimeOut(object obj)
        {
            if (flager == 1)
            {
                CloseProc();
                CloseUFT();
                FlagStarted = false;
            }
            else
            {
                Options options = (Options)obj;
                string fileStarted = options.file;
                PackStart pack = options.pack;
                Console.WriteLine("Секунд прошло = " + SeconsdEnd);
                if (SeconsdEnd >= Int32.Parse(pack.TestsInPack.time[pack.FilesToStart.IndexOf(fileStarted.ToString())]) && FlagStarted)
                {
                    CloseProc();
                    CloseUFT();
                    FlagStarted = false;
                }
                else if (!FlagStarted)
                {
                    try { SeconsdEnd = 0; } catch { }
                    FlagStarted = false;
                }
                else SeconsdEnd++;
            }
        }
        public void CloseUFT()
        {
            CloseProc();
            try { timer.Dispose(); } catch { }
            try { StartTest.Kill(); } catch { }
            try { StartTest.Close(); } catch { }
            try { SeconsdEnd = 0; } catch { }
        }
        public void CloseProc()
        {
            try { foreach (Process proc in Process.GetProcessesByName("iexplore")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("firefox")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("JinnClient")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("java")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("plugin-container")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("phantomjs")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("chrome")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("Mediator64")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("UFT")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("QtpAutomationAgent")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("wscript")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        public void DeleteResDirectories(String nameTest, String dir)
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
                    DirectoryInfo dirInfo = new DirectoryInfo(res);
                    dirInfo.Delete(true);
                    Console.WriteLine(res);
                }
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

            }
            catch
            {
                result = "no_version";
            }
            return result;
        }
    }
}