using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace Demon
{    
    class Controller
    {
        private DataBaseConnect database = new DataBaseConnect();
        private SQLiteCommand command;
        private string query = "";
        private static Message res = new Message();
        private StartTests startTests = new StartTests();

        public string transformation(string param)
        {            
            Message mess = JsonConvert.DeserializeObject<Message>(param);
            if (mess.args[mess.args.Count - 1].Equals("START"))
            {
                mess.args.RemoveAt(mess.args.Count - 1);
                startTests.Init(mess);
            }
            if (mess.args[mess.args.Count - 1].Equals("STOP"))
            {
                mess.args.RemoveAt(mess.args.Count - 1);
                startTests.Stop(mess);
            }
            return "true";
        }
    }
}
