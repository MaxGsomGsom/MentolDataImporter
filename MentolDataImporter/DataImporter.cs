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
using System.Threading;

namespace MentolDataImporter
{
    class DataImporter
    {
        Logger logger;
        SqlConnection connection;
        public string GetModuleName { get; } = "DataImporter";
        List<DataFormatRecord> dataFormatsList;
        List<DataSourceRecord> dataSourcesList;
        string dataSourcesDllsPath, dataFormatsDllsPath;
        string dataRootPath;
        string inputDir, outputDir, processedDir;
        int threads;
        string separator;
        Queue<Task> tasksQueue;
        Semaphore tasksCountLimiter;

        public bool IsRunning { get; private set; } = false;


        public DataImporter()
        {
            logger = new Logger();

            dataSourcesDllsPath = LoadPathFromConfig("DataSourcesDllsPath", "DataSources");
            dataFormatsDllsPath = LoadPathFromConfig("DataFormatsDllsPath", "DataFormats");
            dataRootPath = LoadPathFromConfig("DataRootPath", "Data");

            inputDir = ConfigurationManager.AppSettings["InputDir"].Trim() ?? "Input";
            outputDir = ConfigurationManager.AppSettings["OutputDir"].Trim() ?? "Output";
            processedDir = ConfigurationManager.AppSettings["ProcessedDir"].Trim() ?? "Processed";

            separator = ConfigurationManager.AppSettings["Separator"] ?? ";";

            try
            {
                threads = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadsCount"]);
            }
            finally
            {
                if (threads < 1) threads = 1;
            }

            tasksQueue = new Queue<Task>();
            tasksCountLimiter = new Semaphore(threads, threads);
        }



        public void RunFilesProcessing()
        {
            if (IsRunning) return;
            IsRunning = true;

            try
            {
                FillTasksQueue();
                List<Task> inProgressTasksList = new List<Task>();

                while (tasksQueue.Count > 0)
                {
                    tasksCountLimiter.WaitOne();
                    Task task = tasksQueue.Dequeue();
                    task.Start();
                    inProgressTasksList.Add(task);
                }

                Task.WaitAll(inProgressTasksList.ToArray());
            }
            finally
            {
                logger.FlushLog();
                IsRunning = false;
            }
        }


        public void FillTasksQueue()
        {
            foreach (DataSourceRecord item in dataSourcesList)
            {
                string inputPath = Path.Combine(dataRootPath, item.Name, inputDir);
                string[] files = Directory.GetFiles(inputPath);

                foreach (string fileName in files)
                {
                    Task task = new Task(() =>
                    {
                        try
                        {
                            ProcessFile(item, Path.GetFileName(fileName));
                        }
                        finally
                        {
                            tasksCountLimiter.Release();
                        }
                    });

                    tasksQueue.Enqueue(task);
                }
            }
        }



        void ProcessFile(DataSourceRecord record, string fileName)
        {
            logger.Info(GetModuleName, "Started processing of file '" + fileName + "'");

            //Read file and convert it to list of strings
            string inputFilePath = Path.Combine(dataRootPath, record.Name, inputDir, fileName);
            string errorMessageDataFormat = "There is an error while reading file '" + fileName + "'";
            List<string> rawStrings = null;
            IDataFormat dataFormat = record.FormatRecord.FormatObj;

            try
            {
                rawStrings = dataFormat.ReadFile(inputFilePath, logger);
                if (rawStrings == null || rawStrings.Count == 0)
                    throw new IOException(errorMessageDataFormat);

                logger.Info(dataFormat.GetModuleName, "Read " + rawStrings.Count + " lines from file '" + fileName + "'");
            }
            catch (Exception ex)
            {
                logger.Error(dataFormat.GetModuleName, errorMessageDataFormat);
                throw ex;
            }

            //Parse list of strings to final view
            string errorMessageSource = "There is an error while parsing file '" + fileName + "'";
            List<string[]> processedStrings = null;
            IDataSource dataSource = record.SourceObj;
            try
            {
                processedStrings = dataSource.Parse(rawStrings, logger);
                if (processedStrings == null || processedStrings.Count == 0)
                    throw new InvalidDataException(errorMessageSource);
                logger.Info(dataSource.GetModuleName, "Parsed " + processedStrings.Count + " lines from file '" + fileName + "'");
            }
            catch (Exception ex)
            {
                logger.Error(dataSource.GetModuleName, errorMessageSource);
                throw ex;
            }

            //Write strings to output file, move input file to processed
            try
            {
                string outputFilePath = Path.Combine(dataRootPath, record.Name, outputDir, fileName);
                string processedFilePath = Path.Combine(dataRootPath, record.Name, processedDir, fileName);

                string[] joinedStrings = new string[processedStrings.Count];
                for (int i = 0; i < processedStrings.Count; i++)
                {
                    joinedStrings[i] = string.Join(separator, processedStrings[i]);
                }

                //Check if output file exists
                string tempPath = outputFilePath;
                int n = 1;
                while (File.Exists(tempPath))
                {
                    tempPath = Path.Combine(Path.GetDirectoryName(outputFilePath),
                        Path.GetFileNameWithoutExtension(outputFilePath) + "_" + n + Path.GetExtension(outputFilePath));
                    n++;
                }
                outputFilePath = tempPath;

                //Check if processed file exists
                tempPath = processedFilePath;
                n = 1;
                while (File.Exists(tempPath))
                {
                    tempPath = Path.Combine(Path.GetDirectoryName(processedFilePath), 
                        Path.GetFileNameWithoutExtension(processedFilePath) + "_" + n + Path.GetExtension(processedFilePath));
                    n++;
                }
                processedFilePath = tempPath;

                File.WriteAllLines(outputFilePath, joinedStrings);
                File.Move(inputFilePath, processedFilePath);
                logger.Info(GetModuleName, "Written " + joinedStrings.Count() + " lines to file '" + Path.GetFileName(outputFilePath) + "'");
            }
            catch (Exception ex)
            {
                logger.Error(GetModuleName, "There is an error while writing processed file '" + fileName + "'");
                throw ex;
            }
        }



        string LoadPathFromConfig(string param, string def, string prefix = null)
        {
            string result = ConfigurationManager.AppSettings[param].Trim() ?? def;
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
                logger.Critical(GetModuleName, "Can't connect to DB ");
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
                logger.Critical(GetModuleName, "Can't execute query to DB");
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
                record.Name = line.Field<string>("Name").Trim();

                if (!dataFormatsList.Contains(record))
                {
                    record.Id = line.Field<int>("Id");
                    record.DllFileName = line.Field<string>("DllFileName").Trim();

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
                record.Name = line.Field<string>("Name").Trim();
                if (!dataSourcesList.Contains(record))
                {
                    record.Id = line.Field<int>("Id");
                    record.DllFileName = line.Field<string>("DllFileName").Trim();

                    string dllPath = Path.Combine(dataSourcesDllsPath, record.DllFileName);
                    record.SourceObj = LoadDllAndCreateObjectInstance<IDataSource>(dllPath);
                    record.FormatRecord = dataFormatsList.First((e) => e.Id == line.Field<int>("DataFormatId"));

                    if (record.FormatRecord.FormatObj == null)
                    {
                        string message = "Can't find specific DataFormat for DataSource with name " + record.Name;
                        logger.Critical(GetModuleName, message);
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
                logger.Critical(GetModuleName, "Can't load dll " + dllPath);
                throw ex;
            }

            foreach (TypeInfo item in dll.DefinedTypes)
            {
                if (item.ImplementedInterfaces.Contains(typeof(T)))
                    return (T)Activator.CreateInstance(item);
            }

            string message = "Can't find type " + typeof(T).ToString() + " in " + dllPath;
            logger.Critical(GetModuleName, message);
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
