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
using System.Text.RegularExpressions;

namespace MentolDataImporter
{
    class DataImporter
    {
        public Logger Logger { get; private set; }
        public string GetModuleName { get; } = "DataImporter";
        public bool IsRunning { get; private set; } = false;

        SqlConnection connection;
        List<DataFormatRecord> dataFormatsList;
        List<DataSourceRecord> dataSourcesList;
        string dataSourcesDllsPath, dataFormatsDllsPath;
        string dataRootPath;
        string inputDir, outputDir, processedDir;
        int threads;
        string separator;
        Queue<Task> tasksQueue;
        Semaphore tasksCountLimiter;
        Encoding outputEncoding;

        public DataImporter()
        {
            Logger = new Logger();

            try
            {
                dataSourcesDllsPath = LoadPathFromConfig("DataSourcesDllsPath", "DataSources");
                dataFormatsDllsPath = LoadPathFromConfig("DataFormatsDllsPath", "DataFormats");
                dataRootPath = LoadPathFromConfig("DataRootPath", "Data");

                inputDir = ConfigurationManager.AppSettings["InputDir"].Trim() ?? "Input";
                outputDir = ConfigurationManager.AppSettings["OutputDir"].Trim() ?? "Output";
                processedDir = ConfigurationManager.AppSettings["ProcessedDir"].Trim() ?? "Processed";

                separator = ConfigurationManager.AppSettings["Separator"] ?? ";";
                outputEncoding = Encoding.GetEncoding(ConfigurationManager.AppSettings["OutputEncoding"]);

                try
                {
                    threads = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadsCount"]);
                }
                finally
                {
                    if (threads < 1) threads = 1;
                }
            }
            catch (Exception ex)
            {
                Logger.Critical(GetModuleName, "Error during initialization. App.config has wrong parameters");
                throw ex;
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
                Logger.FlushLog();
                IsRunning = false;
            }
        }


        public void FillTasksQueue()
        {
            foreach (DataSourceRecord item in dataSourcesList)
            {
                string inputPath = Path.Combine(dataRootPath, item.Name, inputDir);
                List<string> files = new List<string>();

                //Add files to processing list according to extensions
                if (item.FormatRecord.Extensions == null || item.FormatRecord.Extensions.Length == 0)
                {
                    files.AddRange(Directory.GetFiles(inputPath));
                }
                else
                {
                    foreach (string ext in item.FormatRecord.Extensions)
                    {
                        files.AddRange(Directory.GetFiles(inputPath, ext));
                    }
                }

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
            Logger.Info(GetModuleName, "Started processing of file '" + fileName + "'");

            //Read file and convert it to list of strings
            string inputFilePath = Path.Combine(dataRootPath, record.Name, inputDir, fileName);
            string errorMessageDataFormat = "There is an error while reading file '" + fileName + "'";
            List<string> rawStrings = null;
            IDataFormat dataFormat = record.FormatRecord.FormatObj;

            try
            {
                rawStrings = dataFormat.ReadFile(inputFilePath, Logger, record.Encoding);
                if (rawStrings == null || rawStrings.Count == 0)
                    throw new IOException(errorMessageDataFormat);

                Logger.Info(dataFormat.GetModuleName, "Read " + rawStrings.Count + " lines from file '" + fileName + "'");
            }
            catch
            {
                Logger.Error(dataFormat.GetModuleName, errorMessageDataFormat);
                return;
            }

            //Parse list of strings to final view
            string errorMessageSource = "There is an error while parsing file '" + fileName + "'";
            List<string[]> processedStrings = null;
            IDataSource dataSource = record.SourceObj;
            try
            {
                processedStrings = dataSource.Parse(rawStrings, Logger);
                if (processedStrings == null || processedStrings.Count == 0)
                    throw new InvalidDataException(errorMessageSource);
                Logger.Info(dataSource.GetModuleName, "Parsed " + processedStrings.Count + " lines from file '" + fileName + "'");
            }
            catch
            {
                Logger.Error(dataSource.GetModuleName, errorMessageSource);
                return;
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

                if (outputEncoding == null) File.WriteAllLines(outputFilePath, joinedStrings);
                else File.WriteAllLines(outputFilePath, joinedStrings, outputEncoding);
                File.Move(inputFilePath, processedFilePath);
                Logger.Info(GetModuleName, "Written " + joinedStrings.Count() + " lines to file '" + Path.GetFileName(outputFilePath) + "'");
            }
            catch
            {
                Logger.Error(GetModuleName, "There is an error while writing processed file '" + fileName + "'");
                return;
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
                Logger.Critical(GetModuleName, "Can't connect to DB ");
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
                Logger.Critical(GetModuleName, "Can't execute query to DB");
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

                    //Parse extensions list
                    string extLine = line.Field<string>("Extensions");
                    if (extLine != null && extLine.Length > 0)
                    {
                        var allExt = extLine.Split(',');
                        List<string> goodExt = new List<string>();

                        foreach (string ext in allExt)
                        {
                            if (!Regex.IsMatch(ext, "[\\/:\"<>|]+")) //Forbidden filename symbols except ? and *
                                goodExt.Add(ext.Trim());
                        }
                        if (goodExt.Count > 0) record.Extensions = allExt.ToArray();
                        else
                        {
                            string errMsg = "It seems there is an error in defenition of extensions for data format '" + record.Name + "'";
                            Logger.Critical(GetModuleName, errMsg);
                            throw new SyntaxErrorException(errMsg);
                        }
                    }
                    else record.Extensions = null;

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
                        Logger.Critical(GetModuleName, message);
                        throw new KeyNotFoundException();
                    }

                    //Parse encoding of files
                    string encLine = line.Field<string>("Encoding");
                    if (encLine != null && encLine.Length > 0)
                    {
                        Encoding enc = Encoding.GetEncoding(encLine.Trim().ToLowerInvariant());
                        if (enc == null)
                        {
                            string errMsg = "It seems there is an error in defenition of encoding for data source '" + record.Name + "'";
                            Logger.Critical(GetModuleName, errMsg);
                            throw new SyntaxErrorException(errMsg);
                        }
                        else record.Encoding = enc;
                    }
                    else record.Encoding = null;

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
                Logger.Critical(GetModuleName, "Can't load dll " + dllPath);
                throw ex;
            }

            foreach (TypeInfo item in dll.DefinedTypes)
            {
                if (item.ImplementedInterfaces.Contains(typeof(T)))
                    return (T)Activator.CreateInstance(item);
            }

            string message = "Can't find type " + typeof(T).ToString() + " in " + dllPath;
            Logger.Critical(GetModuleName, message);
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
        public Encoding Encoding;

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
        public string[] Extensions;
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
