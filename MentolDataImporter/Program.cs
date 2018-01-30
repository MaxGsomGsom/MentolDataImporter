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

            importer.Logger.OnErrorOccurredWarning += (obj, e) => 
            { Console.WriteLine("One or more errors occurred. See log for details"); };

            importer.ReadSourcesAndFormats();
            importer.RunFilesProcessing();

            Console.ReadKey();
        }
    }
}
