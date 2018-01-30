using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentolDataImporter.Interfaces;
using System.Text.RegularExpressions;

namespace MentolDataImporter.DataSources.Peterstar
{
    /// <summary>
    /// Parses Peterstar txt files
    /// </summary>
    public class DataSource : IDataSource
    {
        public string GetModuleName { get; } = "PeterstarDataSource";

        public List<string[]> ParseStrings(List<string> data, ILogger logger)
        {
            List<string[]> result = new List<string[]>();

            for (int i = 0; i < data.Count; i++)
            {
                if (Regex.IsMatch(data[i], @"^\d\d\d(-\d\d){2}  \d\d/\d\d  \d\d:\d\d")) //"326-66-37  02/04  09:18"
                {
                    try
                    {
                        string[] cells = new string[7];
                        cells[0] = data[i].Substring(0, 9).Trim(); //Line
                        cells[1] = data[i].Substring(11, 5).Trim(); //Date
                        cells[2] = data[i].Substring(18, 5).Trim(); //Time
                        cells[3] = data[i].Substring(25, 21).Trim(); //Called Number
                        cells[4] = data[i].Substring(48, 21).Trim(); //Destination
                        cells[5] = data[i].Substring(71, 5).Trim(); //Duration
                        cells[6] = data[i].Substring(78, 25).Trim(); //Cost

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
