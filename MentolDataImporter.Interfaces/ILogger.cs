using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    public interface ILogger
    {
        void Info(string moduleName, string text);
        void Warn(string moduleName, string text);
        void Error(string moduleName, string text);
    }
}
