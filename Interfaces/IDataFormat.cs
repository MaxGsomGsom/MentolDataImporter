using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    interface IDataFormat
    {
        List<string> ReadFile(string fileName);
    }
}
