using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    /// <summary>
    /// Interface for modules that parse raw array of lines from file and create array of separate cells
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Parses raw array of strings from file and create array of separate cells
        /// </summary>
        /// <param name="data">Array of raw strings</param>
        /// <param name="logger">Logger object</param>
        /// <returns></returns>
        List<string[]> Parse(List<string> data, ILogger logger);

        /// <summary>
        /// Returns name of current module
        /// </summary>
        string GetModuleName { get; }
    }
}
