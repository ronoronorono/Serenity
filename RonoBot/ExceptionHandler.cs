using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RonoBot
{
    class ExceptionHandler
    {
        public static void WriteToFile(Exception e)
        {
            DateTime dateTime = DateTime.UtcNow.Date;
            string excDay = dateTime.ToString("dd/MM/yyyy");
            string excTime = DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond;

            string error = "";
            
            error += "OCORRIDO EM: ";
            error += excDay + Environment.NewLine;
            error += "AS: ";
            error += excTime + Environment.NewLine+ Environment.NewLine;

            error += "******************SOURCE******************";
            error += Environment.NewLine + Environment.NewLine + e.Source + Environment.NewLine + Environment.NewLine;

            error += "******************STACK TRACE******************";
            error += Environment.NewLine + Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine;

            error += "******************MESSAGE******************";
            error += Environment.NewLine + Environment.NewLine + e.Message + Environment.NewLine + Environment.NewLine;

            string filename = "exception" + excTime.Replace(":","") + ".txt";
          
            File.WriteAllText(@".\errors\" + filename, error);

            Console.WriteLine(excTime+" EXCEPTION OCCURRED, DETAILS IN .../errors/" + filename + "\n");
        }
    }
}
