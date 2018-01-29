using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentolDataImporter.Interfaces;
using System.IO;

namespace MentolDataImporter.DataFormats.Txt
{
    public class DataFormat : IDataFormat
    {
        public string GetModuleName { get; } = "TxtDataFormat";

        public List<string> ReadFile(string fileName, ILogger logger)
        {
            List<string> result = null;
            try
            {
                result = File.ReadAllLines(fileName).ToList();
            }
            catch
            {
                logger.Error(GetModuleName, "Can't read file");
            }
            return result;
        }
    }
}
