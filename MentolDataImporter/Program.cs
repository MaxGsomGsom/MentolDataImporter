using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentolDataImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            DataImporter importer = new DataImporter();
            importer.ReadSourcesAndFormats();
            importer.RunFilesProcessing();

            //TODO: extensions, encoding
        }
    }
}
