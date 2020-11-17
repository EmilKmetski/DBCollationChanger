using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CollationChange
{
    class ErrorLogging
    {

        private string logFilePath = ((new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).FullName).ToString().Replace(@"exe", @"exe.log");

        public void AddErrorToLog(string errortext)
        {
            if (File.Exists(logFilePath) == false)
            {
                File.Create(logFilePath);
            }
            else
            {
                File.AppendAllText(logFilePath, "Error time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t Error text:" + errortext + "\n\r");
            }

        }

    }
}
