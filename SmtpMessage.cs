using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MySmtp
{
    class SmtpMessage
    {
        const string SUBJECT = "Subject: ";
        const string FROM = "From: ";
        const string TO = "To: ";
        const string MIME_VERSION = "MIME-Version: ";
        const string DATE = "Date: ";
        const string CONTENT_TYPE = "Content-Type: ";
        const string CONTENT_TRANSFER_ENCODING = "Content-Transfer-Encoding: ";

        StringBuilder data = new StringBuilder();
        String subject = "";
        string from = "";
        string to = "";
        string message = "";
        string mimeVersion = "";
        string date = "";
        string contentType = "";
        string contentTransferEncoding = "";
        string title = "";

        internal void Process(string line)
        {
            if (line.StartsWith(SUBJECT)) subject = line.Substring(SUBJECT.Length);
            else if (line.StartsWith(FROM)) from = line.Substring(FROM.Length);
            else if (line.StartsWith(TO)) to = line.Substring(TO.Length);
            else if (line.StartsWith(MIME_VERSION)) mimeVersion = line.Substring(MIME_VERSION.Length);
            else if (line.StartsWith(DATE)) date = line.Substring(DATE.Length);
            else if (line.StartsWith(CONTENT_TYPE)) contentType = line.Substring(CONTENT_TYPE.Length);
            else if (line.StartsWith(CONTENT_TRANSFER_ENCODING)) contentTransferEncoding = line.Substring(CONTENT_TRANSFER_ENCODING.Length);
            else data.AppendLine(line);
        }

        internal void Display()
        {
            if (data != null) CloseMessage();
            Console.Error.WriteLine("===================================================================================================================================");
            Console.Error.WriteLine("From:    " + from);
            Console.Error.WriteLine("To:      " + to);
            Console.Error.WriteLine("Subject: " + subject);
            Console.Error.WriteLine("Type:    " + contentType + "           Encoding: " + contentTransferEncoding);
            Console.Error.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------");
            Console.Error.WriteLine(title != "" ? "Title: "+title : "Full content:\n"+message);
            Console.Error.WriteLine("");
        
        }
        

        private string DecodeBase64(string encodedString)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encodedString);
                return Encoding.UTF8.GetString(data);
            }
            catch(Exception e)
            {
                Console.WriteLine("********** ERROR: " + e.Message);
                if (encodedString.Length > 50) Console.WriteLine(encodedString.Substring(0, 45) + "....."); else Console.WriteLine(encodedString);
                return encodedString;
            }
        }




        void CloseMessage()
        {
            message = data.ToString();
            data = null;
            if (contentType=="text/calendar")
            {
                title = "CANNOT DISPLAY text/calendar content";
            }


            if (contentTransferEncoding == "base64") message = DecodeBase64(message); else throw new Exception("Unsupported Transfer encoding: " + contentTransferEncoding);
            int i = message.IndexOf("*** ORIGINALLY SENT TO:");
            if (i > 0)
            {
                string r = message.Substring(i + 23);
                while (r.IndexOf("*") > 0) r = r.Replace("*", "").Trim();
                to = "[" + r + "] -> " + to;
                message = message.Substring(0, i - 1);
            }

            if (contentType.IndexOf("text/html")<0) return;
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(message);
                XmlNodeList l = xmlDoc.GetElementsByTagName("title");
                if (l.Count > 0) title = l[0].InnerText;
            }
            catch (Exception e)     // If not an XML message
            {
                Console.WriteLine(e.Message);
                return;
            }
        }
    }
}
