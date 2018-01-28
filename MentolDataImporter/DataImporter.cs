using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentolDataImporter.Interfaces;
using System.Reflection;
using System.IO;

namespace MentolDataImporter
{
    class DataImporter
    {
        Logger logger;
        SqlConnection connection;
        const string moduleName = "DataImporter";
        List<DataFormatRecord> dataFormatsList;
        List<DataSourceRecord> dataSourcesList;
        string dataSourcesDllsPath, dataFormatsDllsPath;
        string dataRootPath;
        string inputDir, outputDir, processedDir;
        int threads;
        string separator;

        public DataImporter()
        {
            logger = new Logger();

            dataSourcesDllsPath = LoadPathFromConfig("DataSourcesDllsPath", "DataSources");
            dataFormatsDllsPath = LoadPathFromConfig("DataFormatsDllsPath", "DataFormats");
            dataRootPath = LoadPathFromConfig("DataRootPath", "Data");

            inputDir = ConfigurationManager.AppSettings["InputDir"] ?? "Input";
            outputDir = ConfigurationManager.AppSettings["OutputDir"] ?? "Output";
            processedDir = ConfigurationManager.AppSettings["ProcessedDir"] ?? "Processed";

            separator = ConfigurationManager.AppSettings["Separator"] ?? ";";

            try
            {
                threads = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadsCount"]);
            }
            finally
            {
                if (threads < 1) threads = 1;
            }
        }


        string LoadPathFromConfig(string param, string def, string prefix = null)
        {
            string result = ConfigurationManager.AppSettings[param] ?? def;
            if (!Path.IsPathRooted(result))
                result = Path.Combine(Directory.GetCurrentDirectory(), result);
            Directory.CreateDirectory(result);
            return result;
        }

        public void ReadSourcesAndFormats()
        {
            try
            {
                connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Default"].ConnectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                logger.Error(moduleName, "Can't connect to DB ");
                connection?.Dispose();
                throw ex;
            }

            DataTable formatsTable = ExecuteCommand("select * from DataFormats", connection);
            dataFormatsList = LoadDataFormats(formatsTable);

            DataTable sourcesTable = ExecuteCommand("select * from DataSources", connection);
            dataSourcesList = LoadDataSources(sourcesTable, dataFormatsList);


            foreach (DataSourceRecord item in dataSourcesList)
            {
                Directory.CreateDirectory(Path.Combine(dataRootPath, item.Name, inputDir));
                Directory.CreateDirectory(Path.Combine(dataRootPath, item.Name, outputDir));
                Directory.CreateDirectory(Path.Combine(dataRootPath, item.Name, processedDir));
            }

            connection.Close();
        }


        DataTable ExecuteCommand(string query, SqlConnection connection)
        {
            SqlCommand cmd = null;
            DataTable table = null;

            try
            {
                table = new DataTable();
                cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(table);
            }
            catch (Exception ex)
            {
                logger.Error(moduleName, "Can't execute query to DB");
                throw ex;
            }

            return table;
        }

        List<DataFormatRecord> LoadDataFormats(DataTable formatsTable)
        {
            var dataFormatsList = new List<DataFormatRecord>();
            foreach (DataRow line in formatsTable.Rows)
            {
                var record = new DataFormatRecord();
                record.Name = line.Field<string>("Name");

                if (!dataFormatsList.Contains(record))
                {
                    record.Id = line.Field<int>("Id");
                    record.DllFileName = line.Field<string>("DllFileName");

                    string dllPath = Path.Combine(dataFormatsDllsPath, record.DllFileName);
                    record.FormatObj = LoadDllAndCreateObjectInstance<IDataFormat>(dllPath);
                    dataFormatsList.Add(record);
                }
            }

            return dataFormatsList;
        }


        List<DataSourceRecord> LoadDataSources(DataTable sourcesTable, List<DataFormatRecord> dataFormatsList)
        {
            var dataSourcesList = new List<DataSourceRecord>();
            foreach (DataRow line in sourcesTable.Rows)
            {
                var record = new DataSourceRecord();
                record.Name = line.Field<string>("Name");
                if (!dataSourcesList.Contains(record))
                {
                    record.Id = line.Field<int>("Id");
                    record.DllFileName = line.Field<string>("DllFileName");

                    string dllPath = Path.Combine(dataSourcesDllsPath, record.DllFileName);
                    record.SourceObj = LoadDllAndCreateObjectInstance<IDataSource>(dllPath);
                    record.FormatRecord = dataFormatsList.First((e) => e.Id == line.Field<int>("DataFormatId"));

                    if (record.FormatRecord.FormatObj == null)
                    {
                        string message = "Can't find specific DataFormat for DataSource with name " + record.Name;
                        logger.Error(moduleName, message);
                        throw new KeyNotFoundException();
                    }

                    dataSourcesList.Add(record);
                }
            }

            return dataSourcesList;
        }


        T LoadDllAndCreateObjectInstance<T>(string dllPath)
        {
            Assembly dll;
            try
            {
                dll = Assembly.LoadFrom(dllPath);
            }
            catch (Exception ex)
            {
                logger.Error(moduleName, "Can't load dll " + dllPath);
                throw ex;
            }

            foreach (TypeInfo item in dll.DefinedTypes)
            {
                if (item.ImplementedInterfaces.Contains(typeof(T)))
                    return (T)Activator.CreateInstance(item);
            }

            string message = "Can't find type " + typeof(T).ToString() + " in " + dllPath;
            logger.Error(moduleName, message);
            throw new TypeLoadException(message);
        }
    }


    public struct DataSourceRecord
    {
        public int Id;
        public string Name;
        public string DllFileName;
        public IDataSource SourceObj;
        public DataFormatRecord FormatRecord;

        public override bool Equals(object obj)
        {
            return ((DataFormatRecord)obj).Name == Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct DataFormatRecord
    {
        public int Id;
        public string Name;
        public string DllFileName;
        public IDataFormat FormatObj;

        public override bool Equals(object obj)
        {
            return ((DataFormatRecord)obj).Name == Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}
