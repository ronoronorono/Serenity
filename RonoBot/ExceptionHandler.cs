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
            string excTime = DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;

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
            error += date + "\n";
            error += "AS:";
            error += time + "\n\n";

            error += "******************SOURCE******************";
            error += "\n\n" + e.Source + "\n\n";

            error += "******************STACK TRACE******************";
            error += "\n\n" + e.StackTrace + "\n\n";

            error += "******************INNER EXCEPTION******************";
            error += "\n\n" + e.InnerException + "\n\n";

            error += "******************MESSAGE******************";
            error += "\n\n" + e.Message + "\n\n";

            string filename = "exception" + time + ".txt";

            File.WriteAllText(@"C:\Users\NetWork\Desktop\DiscordBot\RonoBot\RonoBot\errors\"+filename, error);

            ConsoleWarn("EXCEPTION OCCURRED, DETAILS IN C:/Users/NetWork/Desktop/DiscordBot/RonoBot/RonoBot/errors/" + filename);
        }

        public Exception E { get => e; }
        public string Date { get => date;}
        public string Time { get => time;}
    }
}
