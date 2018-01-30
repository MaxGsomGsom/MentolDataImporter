using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentolDataImporter.Interfaces;
using System.IO;

namespace MentolDataImporter.DataFormats.Txt
{
    /// <summary>
    /// Reads any text file with given encoding
    /// </summary>
    public class DataFormat : IDataFormat
    {
        public string GetModuleName { get; } = "TxtDataFormat";

        public List<string> ReadFile(string fileName, ILogger logger, Encoding encoding = null)
        {
            List<string> result = null;
            try
            {
                if (encoding == null) result = File.ReadAllLines(fileName).ToList();
                else result = File.ReadAllLines(fileName, encoding).ToList();
            }
            catch (Exception ex)
            {
                logger.Error(GetModuleName, "Can't read file: " + ex.Message);
            }
            return result;
        }
    }
}
