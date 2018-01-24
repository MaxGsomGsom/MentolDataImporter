using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    interface IDataSource
    {
        List<string[]> Parse(List<string> data);
    }
}
