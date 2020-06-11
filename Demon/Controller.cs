using Newtonsoft.Json;
using System;

namespace Demon
{    
    class Controller
    {
        private StartTests startTests = new StartTests();
        private Logger logger = new Logger();
        private Message mess;

        public string transformation(string param)
        {
            try
            {
                mess = JsonConvert.DeserializeObject<Message>(param);
                if (mess.args[mess.args.Count - 1].Equals("START"))
                {
                    mess.args.RemoveAt(mess.args.Count - 1);
                    startTests.Stop(mess);
                    startTests.Init(mess);
                    logger.WriteLog("Выполнен запрос на запуск набора");
                }
                if (mess.args[mess.args.Count - 1].Equals("STOP"))
                {
                    mess.args.RemoveAt(mess.args.Count - 1);
                    startTests.Stop(mess);
                    logger.WriteLog("Выполнен запрос на остановку набора");
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog("Не удалось запустить/остановить (" + mess.args[mess.args.Count - 1] + ") по причине " + ex.Message, "ERROR");
            }
            return "true";
        }
    }
}
