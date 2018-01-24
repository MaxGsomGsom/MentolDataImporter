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
            logFileName = ConfigurationManager.AppSettings["LogFileName"] ?? ".\\Log.txt";
            writer = File.AppendText(logFileName);
        }

        public void Dispose()
        {
            writer?.Flush();
            writer?.Dispose();
        }

        public void Error(string moduleName, string text)
        {
            writer.WriteLineAsync(DateTime.Now.ToShortTimeString() + " - ERROR - " + moduleName + " - " + text);
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
