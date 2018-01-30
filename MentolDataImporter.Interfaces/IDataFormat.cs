using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    /// <summary>
    /// Interface for modules that read file of specific format and create list of raw strings
    /// </summary>
    public interface IDataFormat
    {

        /// <summary>
        /// Reads file of specific format and creates list of raw strings
        /// </summary>
        /// <param name="fileName">Name of file to read</param>
        /// <param name="logger">Logger object</param>
        /// <param name="encoding">Text encoding for some types of files</param>
        /// <returns>List of raw strings in encoding of input file</returns>
        List<string> ReadFile(string fileName, ILogger logger, Encoding encoding = null);

        /// <summary>
        /// Current module name. Used for logging
        /// </summary>
        string GetModuleName { get; }
    }
}
