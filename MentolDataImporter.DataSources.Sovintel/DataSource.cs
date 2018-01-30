using MentolDataImporter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MentolDataImporter.DataSources.Sovintel
{
    /// <summary>
    /// Parses Sovintel txt files
    /// </summary>
    public class DataSource: IDataSource
    {
        public string GetModuleName { get; } = "SovintelDataSource";

        public List<string[]> ParseStrings(List<string> data, ILogger logger)
        {
            List<string[]> result = new List<string[]>();

            for (int i = 0; i < data.Count; i++)
            {
                if (Regex.IsMatch(data[i], @"^\d\d(\.\d\d){2} \d\d:\d\d ")) //"21.04.09 13:32 "
                {
                    try
                    {
                        string[] cells = new string[6];
                        cells[0] = data[i].Substring(0, 8).Trim(); //Date
                        cells[1] = data[i].Substring(9, 5).Trim(); //Time
                        cells[2] = data[i].Substring(15, 15).Trim(); //Subscriber
                        cells[3] = data[i].Substring(31, 20).Trim(); //Direction
                        cells[4] = data[i].Substring(52, 4).Trim(); //Pr
                        cells[5] = data[i].Substring(57, 9).Trim(); //Sum

                        result.Add(cells);
                    }
                    catch
                    {
                        logger.Error(GetModuleName, "Can't parse at least line " + i);
                        return null;
                    }
                }
            }
            return result;
        }
    }
}
