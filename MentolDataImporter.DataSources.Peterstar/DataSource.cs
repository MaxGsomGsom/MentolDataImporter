using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentolDataImporter.Interfaces;
using System.Text.RegularExpressions;

namespace MentolDataImporter.DataSources.Peterstar
{
    public class DataSource : IDataSource
    {
        const string moduleName = "PeterstarDataFormat";

        public List<string[]> Parse(List<string> data, ILogger logger, string fileName)
        {
            List<string[]> result = new List<string[]>();
            for (int i = 0; i < data.Count; i++) {
                if (Regex.IsMatch(data[i], @"^\d\d\d(-\d\d){2}  \d\d/\d\d  \d\d:\d\d")) //"326-66-37  02/04  09:18"
                {
                    try
                    {
                        string[] cells = new string[7];
                        cells[0] = data[i].Substring(0, 9); //phone number
                        cells[1] = data[i].Substring(12, 5); //data
                        cells[2] = data[i].Substring(19, 5); //time
                        cells[3] = data[i].Substring(26, 21).Trim(); //phone number
                        cells[4] = data[i].Substring(49, 21).Trim(); //location
                        cells[5] = data[i].Substring(72, 5); //time
                        cells[6] = data[i].Substring(79, 25).Trim(); //price

                        result.Add(cells);
                    }
                    catch
                    {
                        logger.Warn(moduleName, "Can't parse line " + i + " from file " + fileName);
                    }
                }
            }

            return result;
        }
    }
}
