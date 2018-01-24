using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter.Interfaces
{
    public interface IDataSource
    {
        List<string[]> Parse(List<string> data, ILogger logger, string fileName);
    }
}
