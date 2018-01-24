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
        const string moduleName = "TxtDataFormat";

        public List<string> ReadFile(string fileName, ILogger logger)
        {
            List<string> result = null;
            try
            {
                result = File.ReadAllLines(fileName).ToList();
                logger.Info(moduleName, "Read " + result.Count + "lines from " + Path.GetFileName(fileName));
            }
            catch
            {
                logger.Error(moduleName, "Can't read file " + Path.GetFileName(fileName));
            }
            return result;
        }
    }
}
