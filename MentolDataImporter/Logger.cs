using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentolDataImporter.Interfaces;
using System.Configuration;
using System.IO;

namespace MentolDataImporter
{
    class Logger : ILogger
    {
        string logFileName;
        StreamWriter writer;
        bool errorOccurred = false;

        public Logger()
        {
            string logPath = ConfigurationManager.AppSettings["LogsPath"].Trim() ?? "Logs";
            if (!Path.IsPathRooted(logPath)) logPath = Path.Combine(Directory.GetCurrentDirectory(), logPath);
            Directory.CreateDirectory(logPath);
            logFileName = Path.Combine(logPath, DateTime.Now.ToString("dd.MM.yyyy_hh-mm-ss") + ".txt");
            writer = File.AppendText(logFileName);
        }

        public void FlushLog()
        {
            writer?.Flush();
        }

        public void Error(string moduleName, string text)
        {
            writer?.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " - ERROR - " + moduleName + " - " + text);
            if (!errorOccurred)
            {
                ErrorOccurredWarning(this, new EventArgs());
                errorOccurred = true;
            }
        }

        public void Info(string moduleName, string text)
        {
            writer?.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " - INFO - " + moduleName + " - " + text);
        }

        public void Critical(string moduleName, string text)
        {
            writer?.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " - CRITICAL - " + moduleName + " - " + text);
        }

        public event EventHandler ErrorOccurredWarning;
    }
}
