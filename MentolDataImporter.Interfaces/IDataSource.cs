using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    /// <summary>
    /// Interface for modules that parse raw list of lines from file,
    /// convert encoding and create list of lines separated on cells
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Parses raw array of strings from file and create list of separate cells
        /// </summary>
        /// <param name="data">Array of raw strings</param>
        /// <param name="logger">Logger object</param>
        /// <returns>List of strings divided on cells in utf16 encoding</returns>
        List<string[]> Parse(List<string> data, ILogger logger);

        /// <summary>
        /// Returns name of current module
        /// </summary>
        string GetModuleName { get; }
    }
}
