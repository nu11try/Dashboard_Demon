using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demon
{
    class Awake
    {
        Timer timer;
        TimerCallback tm;       

        public void Init()
        {
            tm = new TimerCallback(CheckTime);
            timer = new Timer(tm, "", 0, 30000);
        }
        public void CheckTime(object obj)
        {
            try
            {
                PingFun();
            }
            catch
            {
                Console.WriteLine("Не получилось пробудить!");
            }
        }
        private void PingFun()
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            PingReply reply = pingSender.Send(Dns.GetHostByName(Dns.GetHostName()).AddressList[0], timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Попингуемся");
            }
        }
    }
}
