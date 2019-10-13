using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Demon
{
    public class FileSystem
    {
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
    }
}
