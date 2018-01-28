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
    class Logger : ILogger, IDisposable
    {
        string logFileName;
        StreamWriter writer;

        public Logger()
        {
            string logPath = ConfigurationManager.AppSettings["LogsPath"] ?? "Logs";
            if (!Path.IsPathRooted(logPath)) logPath = Path.Combine(Directory.GetCurrentDirectory(), logPath);
            Directory.CreateDirectory(logPath);
            logFileName = Path.Combine(logPath, DateTime.Now.ToString("dd.MM.yyyy_hh-mm-ss") + ".txt");
            writer = File.AppendText(logFileName);
        }

        public void Dispose()
        {
            writer?.Dispose();
        }

        public void Error(string moduleName, string text)
        {
            writer.WriteLineAsync(DateTime.Now.ToString("hh:mm:tt") + " - ERROR - " + moduleName + " - " + text);
        }

        public void Info(string moduleName, string text)
        {
            writer.WriteLineAsync(DateTime.Now.ToShortTimeString() + " - INFO - " + moduleName + " - " + text);
        }

        public void Warn(string moduleName, string text)
        {
            writer.WriteLineAsync(DateTime.Now.ToShortTimeString() + " - WARN - " + moduleName + " - " + text);
        }
    }
}
