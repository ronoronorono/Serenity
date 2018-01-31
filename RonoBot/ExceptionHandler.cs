using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RonoBot
{
    class ExceptionHandler
    {
        Exception e;
        string date;
        string time;

        public ExceptionHandler(Exception e)
        {
            DateTime dateTime = DateTime.UtcNow.Date;
            string excDay = dateTime.ToString("dd/MM/yyyy");
            string excTime = DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Millisecond;

            this.e = e;
            this.date = excDay;
            this.time = excTime;
        }

        public void ConsoleWarn(string txt)
        {
            Console.WriteLine(txt);
        }

        public void WriteToFile()
        {
            string error = "";
            
            error += "OCORRIDO EM:";
            error += date + Environment.NewLine;
            error += "AS:";
            error += time + Environment.NewLine+ Environment.NewLine;

            error += "******************SOURCE******************";
            error += Environment.NewLine + Environment.NewLine + e.Source + Environment.NewLine + Environment.NewLine;

            error += "******************STACK TRACE******************";
            error += Environment.NewLine + Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine;

            error += "******************MESSAGE******************";
            error += Environment.NewLine + Environment.NewLine + e.Message + Environment.NewLine + Environment.NewLine;

            string filename = "exception" + time.Replace(":","") + ".txt";
            try
            {
                File.WriteAllText(@"C:\Users\NetWork\Desktop\DiscordBot\RonoBot\RonoBot\errors\" + filename, error);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            ConsoleWarn("EXCEPTION OCCURRED, DETAILS IN C:/Users/NetWork/Desktop/DiscordBot/RonoBot/RonoBot/errors/" + filename);
        }

        public Exception E { get => e; }
        public string Date { get => date;}
        public string Time { get => time;}
    }
}
