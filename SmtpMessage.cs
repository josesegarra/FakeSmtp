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
        string mimeVersion = "";
        string date = "";
        string cType = "";
        string cEncoding= "";
        string title = "";
        string text = "";
        string nestLevel = "";
        List<SmtpMessage> msgParts = new List<SmtpMessage>();


        internal void Process(string line)
        {
            if (line.StartsWith(SUBJECT)) subject = line.Substring(SUBJECT.Length);
            else if (line.StartsWith(FROM)) from = line.Substring(FROM.Length);
            else if (line.StartsWith(TO)) to = line.Substring(TO.Length);
            else if (line.StartsWith(MIME_VERSION)) mimeVersion = line.Substring(MIME_VERSION.Length);
            else if (line.StartsWith(DATE)) date = line.Substring(DATE.Length);
            else if (line.StartsWith(CONTENT_TYPE)) cType = line.Substring(CONTENT_TYPE.Length);
            else if (line.StartsWith(CONTENT_TRANSFER_ENCODING)) cEncoding = line.Substring(CONTENT_TRANSFER_ENCODING.Length);
            else data.AppendLine(line);
        }
        internal SmtpMessage()
        {
        }
        
        private SmtpMessage(string s, string pType, string pEncoding,string nested)
        {
            cType = pType;
            cEncoding = pEncoding;
            nestLevel = nested + "      ";
            string[] lines = DecodeMessage(s);
            for (int i = 0; i < lines.Length; i++) Process(lines[i]);
            Update();
        }

        void PrintNested(string s)
        {
            string[] l = s.Split('\n');
            for (int i = 0; i < l.Length; i++) Console.WriteLine(nestLevel + "    " + l[i]);
        }

        internal void Display()
        {
            if (nestLevel == "") Console.WriteLine("==============================================================================================================");
            if (from!="")       Console.WriteLine(nestLevel + "From:     " + from);
            if (to.Trim()!= "")        Console.WriteLine(nestLevel + "To:       " + to);
            if (subject != "")  Console.WriteLine(nestLevel + "Subject:  " + subject);
            if (cType != "")    Console.WriteLine(nestLevel + "Type:     " + cType);
            if (cEncoding != "") Console.WriteLine(nestLevel + "Encoding: " + cEncoding);
            if (title!= "")     Console.WriteLine(nestLevel + "Title:    " + title);
            if (msgParts.Count==0 && title == "" && text != "") PrintNested(text);
            foreach (SmtpMessage m in msgParts) m.Display();
            Console.WriteLine("");
        }

        internal void Update()
        {
            text= data.ToString();
            data = null;
            string[] lines = text.Split('\n');
            bool multi = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var t = lines[i].Trim();
                if (t.StartsWith("boundary="))
                {
                    multi = true;
                    GetParts("--" + t.Substring(9).Trim(), lines);
                }
            }
            if (!multi) DecodeMessage(text);
        }

        void GetParts(string bStart,string[] lines)
        {
            StringBuilder sb = new StringBuilder();
            List<string> blocks = new List<string>();
            string bClose = bStart + "--";

            bool inb = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string m=lines[i].Trim();
                if (inb)
                {
                    if (m == bClose)
                    {
                        inb = false;
                        string s=sb.ToString().Trim();
                        if (s!="") blocks.Add(s);sb.Length = 0;
                    }
                    else if (m != bStart)
                    {
                        sb.Append(lines[i]);
                    }
                    else
                    {
                        string s = sb.ToString().Trim();
                        if (s != "") blocks.Add(s); sb.Length = 0;
                    }
                }

                if (!inb)
                {
                    if (m == bStart)
                    {
                        inb = true;
                    }
                }
            }
            foreach (string s in blocks) msgParts.Add(new SmtpMessage(s,cType,cEncoding,nestLevel)); 
        }

        private string DecodeBase64(string encodedString)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encodedString);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception e)
            {
                if (encodedString.Length>20) encodedString=encodedString.Substring(0,20)+"...";
                return "Error decoding base64: " + e.Message+"  "+encodedString;
            }
        }

        string[] DecodeMessage(string s)
        {
            if (cEncoding== "base64") s= DecodeBase64(s);
            // Fix ORIGINALLY
            int i = s.IndexOf("*** ORIGINALLY SENT TO:");
            if (i > 0)
            {
                string r = s.Substring(i + 23);
                while (r.IndexOf("*") > 0) r = r.Replace("*", "").Trim();
                to = "[" + r + "] -> " + to;
                s = s.Substring(0, i - 1);
            }
            // If HTML
            if (cType.IndexOf("text/html") > -1)
            {
                text = s;
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(s);
                    XmlNodeList l = xmlDoc.GetElementsByTagName("title");
                    if (l.Count > 0) title = l[0].InnerText;
                }
                catch (Exception)     // If not an XML message
                {
                }
            }
            return s.Split('\n');
        }

        /*

        

        void GetMessageParts(string s)
        {
            string[] lines = s.Split('\n');
            for (int i = 0; i < lines.Length;i++)
            { 
                var t=lines[i].Trim();
                if (t.StartsWith("boundary=")) GetPartsForBoundary(t.Substring(9), lines);
            }
        }
    
        void AddText(StringBuilder sb)
        {
            string s = sb.ToString().Trim();
            if (s != "") parts.Add(s);
            sb.Length= 0;
        }
        
        void GetPartsForBoundary(string bname,string[] lines)
        {
            StringBuilder sb = new StringBuilder();
            string bend=bname+"--";
            bool inb = false;
            for (int i = 0; i < lines.Length;i++)
            {
                if (inb)
                {
                    if (lines[i].Trim() == bend)
                    {
                        inb = false;
                        AddText(sb);
                    }
                    else if (lines[i].Trim() != bname) sb.Append(lines[i]);
                    else AddText(sb);
                }
                if (!inb && lines[i].Trim() == bname) inb = true;
            }
        }


        void CloseMessage()
        {
            string message = data.ToString();
            data = null;
            if (contentType=="text/calendar") title = "CANNOT DISPLAY text/calendar content";
            GetMessageParts(message);
            List<string> l = new List<string>();
            if (parts.Count>0)  foreach (string s in parts) l.Add(ProcessPart(s)); else l.Add(ProcessPart(message));
            parts=l;
        }

        string ProcessPart(string message)
        {
            if (contentTransferEncoding == "base64") message = DecodeBase64(message);
            else return "Unsupported Transfer encoding: " + contentTransferEncoding;

            int i = message.IndexOf("*** ORIGINALLY SENT TO:");
            if (i > 0)
            {
                string r = message.Substring(i + 23);
                while (r.IndexOf("*") > 0) r = r.Replace("*", "").Trim();
                to = "[" + r + "] -> " + to;
                message = message.Substring(0, i - 1);
            }

            if (contentType.IndexOf("text/html")<0) return message;
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(message);
                XmlNodeList l = xmlDoc.GetElementsByTagName("title");
                if (l.Count > 0) title = l[0].InnerText;
                return "see title !!";
            }
            catch (Exception e)     // If not an XML message
            {
                return e.Message;
            }
        }
         * */
    }
}
