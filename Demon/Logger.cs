using System;
using System.Diagnostics;
using System.IO;

namespace Demon
{
    class Logger
    {
        public void WriteLog(string msg, string flag = "LOG")
        {
            try
            {
                string nameFile = "\\log.txt";
                if (flag == "ERROR") nameFile = "\\error.txt";
                using (FileStream fstream = new FileStream(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + nameFile, FileMode.Append))
                {
                    // преобразуем строку в байты
                    byte[] array = System.Text.Encoding.Default.GetBytes(DateTime.Now + " [" + flag + "] -- " + msg + "\n");
                    // запись массива байтов в файл
                    fstream.Write(array, 0, array.Length);
                }
            }
            catch { }
        }
    }
}
