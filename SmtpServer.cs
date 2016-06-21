using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace MySmtp
{
    public class SmtpServer: TcpListener
    {
        private TcpClient client;
        private NetworkStream stream;
        private System.IO.StreamReader reader;
        private System.IO.StreamWriter writer;
        private Thread thread = null;


        public SmtpServer(IPAddress localaddr, int port)
            : base(localaddr, port)
        {
        }

        new public void Start()
        {
            base.Start();
            client = AcceptTcpClient();
            client.ReceiveTimeout = 5000;
            stream = client.GetStream();
            reader = new System.IO.StreamReader(stream);
            writer = new System.IO.StreamWriter(stream);
            writer.NewLine = "\r\n";
            writer.AutoFlush = true;
            thread = new System.Threading.Thread(new ThreadStart(RunThread));
            thread.Start();
        }

        protected void RunThread()
        {
            string line = null;
            writer.WriteLine("220 localhost -- Smtp server");
            try
            {
                while (reader != null)
                {
                    line = reader.ReadLine();
                    switch (line)
                    {
                        case "DATA":
                            writer.WriteLine("354 Start input, end data with <CRLF>.<CRLF>");
                            SmtpMessage myMessage = new SmtpMessage();
                            line = reader.ReadLine();

                            while (line != null && line != ".")
                            {
                                myMessage.Process(line);
                                line = reader.ReadLine();
                            }
                            myMessage.Update();
                            myMessage.Display();
                            writer.WriteLine("250 OK");
                            break;
                        case "QUIT":
                            writer.WriteLine("250 OK");
                            reader = null;
                            break;

                        default:
                            writer.WriteLine("250 OK");
                            break;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                client.Close();
                Stop();
            }
        }



        public bool IsThreadAlive
        {
            get { return thread.IsAlive; }
        }
    }
}
