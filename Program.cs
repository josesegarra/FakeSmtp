using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MySmtp;
using System.Threading;

namespace MySmtp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int port=10025;
            if (args.Length > 0) port = Int32.Parse(args[0]);
            SmtpServer listener = null;
            do
            {
                Console.WriteLine("Listening on port "+port);
                listener = new SmtpServer(IPAddress.Any, port);
                listener.Start();
                while (listener.IsThreadAlive)
                {
                    Thread.Sleep(500);
                }
            } while (listener != null);
        }
    }
}


