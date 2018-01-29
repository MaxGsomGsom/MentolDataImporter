using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    /// <summary>
    /// Logger interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Used for logging any informational messages
        /// </summary>
        /// <param name="moduleName">Name of current module</param>
        /// <param name="text">Message text</param>
        void Info(string moduleName, string text);

        /// <summary>
        /// Used for logging critical errors that prevent further app executing
        /// </summary>
        /// <param name="moduleName">Name of current module</param>
        /// <param name="text">Message text</param>
        void Critical(string moduleName, string text);

        /// <summary>
        /// Used for logging errors in modules that allow further app executing
        /// </summary>
        /// <param name="moduleName">Name of current module</param>
        /// <param name="text">Message text</param>
        void Error(string moduleName, string text);
    }
}
