using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    /// <summary>
    /// Interface for modules that read file of specific format and create array of raw strings
    /// </summary>
    public interface IDataFormat
    {

        /// <summary>
        /// Reads file of specific format and creates array of raw strings
        /// </summary>
        /// <param name="fileName">Name of file to read</param>
        /// <param name="logger">Logger object</param>
        /// <returns></returns>
        List<string> ReadFile(string fileName, ILogger logger);

        /// <summary>
        /// Returns name of current module
        /// </summary>
        string GetModuleName { get; }
    }
}
