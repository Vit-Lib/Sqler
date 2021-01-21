using App.Module.Sqler.Logical;
using Dapper;
using Sqler.Module.Sqler.Logical.Message;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vit.Core.Module.Log;
using Vit.Core.Util.Common;
using Vit.Core.Util.ComponentModel.Model;
using Vit.Db.Csv;
using Vit.Db.Excel;
using Vit.Extensions;
using Vit.Orm.Dapper;

namespace Sqler.Module.Sqler.Logical.DbPort
{
    public class DbPortLogical
    {
        public static string NewLine = "\r\n";

        public static int? commandTimeout => Vit.Orm.Dapper.DapperConfig.CommandTimeout;

        public static readonly int batchRowCount = Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<int?>("Sqler.DbPort_batchRowCount") ?? 100000;


        #region GetSqlRunConfig
        public static Dictionary<string, string> GetSqlRunConfig(string sql)
        {
            Dictionary<string, string> sqlRunConfig = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(sql)) return sqlRunConfig;

            //var regXml = new Regex(@"\<SqlRunConfig\>[\s\S]+?\<\/SqlRunConfig\>"); //正则匹配 <SqlRunConfig></SqlRunConfig>
            //var regTag = new Regex(@"\<[^\\]+?\>"); ; //正则匹配 <>

            var regXml = new Regex(@"(?<=\<SqlRunConfig\>)[\s\S]*?(?=\<\/SqlRunConfig\>)"); //正则匹配 <SqlRunConfig></SqlRunConfig> 中间的字符串
            var regTag = new Regex(@"(?<=\<)[^\/\<\>]+?(?=\>)"); //正则匹配 <> 中间的字符串(不含/)


            var matches = regXml.Matches(sql);
            if (matches.Count == 0) return sqlRunConfig;

            var xml = matches[0].Value;
            foreach (string tagName in regTag.Matches(xml).Select(i => i.Value).Distinct())
            {
                var values = new Regex($@"(?<=\<{tagName}\>)[\s\S]*?(?=\<\/{tagName}\>)").Matches(xml).Select(m => m.Value).ToArray();
                sqlRunConfig[tagName] = string.Join("", values);
            }
            return sqlRunConfig;
        }

        #endregion





        #region (x.1) Export

        class DataTableWriter : IDisposable
        {
            public Action OnDispose;
            public void Dispose()
            {
                OnDispose?.Invoke();
            }

            public Action<DataTable> WriteData;

        }

     


        /*         

         txt 配置：
             
<SqlRunConfig>

<fileName>表数据.sqlite</fileName>
<tableNames>["aaa","bbb"]</tableNames>
<tableSeparator>

</tableSeparator>
<rowSeparator>
</rowSeparator>
<fieldSeparator>,</fieldSeparator>
</SqlRunConfig>    
             
             
             */

        public static void Export
           (
            Action<EMsgType, string> SendMsg,
            string type,
            string ConnectionString, //数据库连接字符串。亦可从配置文件获取，如 sqler.config:SqlBackup.SqlServerBackup.ConnectionString
            [SsDescription("sqlite/excel/csv/txt")]string exportFileType,
            string sql = null, List<string> inTableNames = null, //指定一个即可,若均不指定，则返回所有表
            string outFilePath = null,string outFileName = null, //指定一个即可
            List<string> outTableNames=null            
            )
        {

            SendMsg(EMsgType.Title, "   Export");

            #region (x.1)连接字符串

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err, "Export error - invalid arg conn.");
                return;
            }

            //解析ConnectionString
            if (ConnectionString.StartsWith("sqler.config:")) 
            {
                ConnectionString = SqlerHelp.sqlerConfig.GetStringByPath(ConnectionString.Substring("sqler.config:".Length));
            }

 
            if (type == "mysql")
            {
                //确保mysql连接字符串包含 "AllowLoadLocalInfile=true;"（用以批量导入数据）
                ConnectionString = "AllowLoadLocalInfile=true;" + ConnectionString;
            }
            else if (type == "mssql")
            {
                //确保mssql连接字符串包含 "persist security info=true;"（用以批量导入数据）
                ConnectionString = "persist security info=true;" + ConnectionString;
            }
            #endregion



            #region SqlRunConfig
            var sqlRunConfig = GetSqlRunConfig(sql);
            if (string.IsNullOrEmpty(outFileName)) 
            {
                sqlRunConfig.TryGetValue("fileName", out outFileName);
            }
            if (outTableNames==null)
            {
                if (sqlRunConfig.TryGetValue("tableNames", out var value)) 
                {
                    outTableNames = value.Deserialize<List<string>>();
                }                
            }
            #endregion



            List<string> filePathList = new List<string> { "wwwroot", "temp", "Export", DateTime.Now.ToString("yyyyMMdd_HHmmss") };
            Func<DataTableWriter> GetDataTableWriter;

            #region (x.2)构建数据导出回调 

            if (exportFileType == "excel")
            {
                #region excel
              
                SendMsg(EMsgType.Title, "   export data to excel file");

                if (string.IsNullOrWhiteSpace(outFileName))
                {
                    outFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + type + ".xlsx";
                }

                filePathList.Add(outFileName);

                if (string.IsNullOrEmpty(outFilePath)) 
                {
                    outFilePath = CommonHelp.GetAbsPath(filePathList.ToArray());
                }
                

                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));

                GetDataTableWriter = () =>
                {
                    int exportedRowCount = 0;

                    return new DataTableWriter
                    {
                        WriteData =
                        (dt) =>
                        {
                            SendMsg(EMsgType.Nomal, "           [x.x.3]Export data");
                            ExcelHelp.SaveDataTable(outFilePath, dt, exportedRowCount == 0, exportedRowCount);

                            if (exportedRowCount == 0) exportedRowCount++;
                            exportedRowCount += dt.Rows.Count;
                        }
                    };
                };
                #endregion
            }
            else if (exportFileType == "sqlite")
            {
                #region sqlite
              
                SendMsg(EMsgType.Title, "   export data to sqlite file");

                if (string.IsNullOrWhiteSpace(outFileName))
                {
                    outFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + type + ".sqlite";
                }

                filePathList.Add(outFileName);

                if (string.IsNullOrEmpty(outFilePath))
                {
                    outFilePath = CommonHelp.GetAbsPath(filePathList.ToArray());
                }

 
                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));

                GetDataTableWriter = () =>
                {
                    var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(outFilePath);
                    var createdTable = false;
                    return new DataTableWriter
                    {
                        OnDispose = () =>
                        {
                            connSqlite.Dispose();
                        },
                        WriteData =
                        (dt) =>
                        {
                            if (!createdTable)
                            {
                                //(x.x.2)create table
                                SendMsg(EMsgType.Title, "           [x.x.x]create table ");
                                connSqlite.Sqlite_CreateTable(dt);

                                createdTable = true;
                            }

                            //(x.x.3)write data           
                            connSqlite.Import(dt);
                        }
                    };
                };
                #endregion
            }
            else if (exportFileType == "csv")
            {
                #region csv

                SendMsg(EMsgType.Title, "   export data to csv file");

                if (string.IsNullOrWhiteSpace(outFileName))
                {
                    outFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + type + ".csv";
                }

                filePathList.Add(outFileName);


                if (string.IsNullOrEmpty(outFilePath))
                {
                    outFilePath = CommonHelp.GetAbsPath(filePathList.ToArray());
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));


                bool isFirstTable = true;
              

                GetDataTableWriter = () =>
                {
                    bool isFirstRow = true;

                    return new DataTableWriter
                    {
                        OnDispose = () =>
                        {                   
                        },
                        WriteData =
                        (dt) =>
                        {
                            if (isFirstTable)
                            {
                                isFirstTable = false;
                            }   
                         

                            CsvHelp.SaveToCsv(outFilePath, dt, isFirstRow, true);
                            if (isFirstRow)
                                isFirstRow = false;
                        }
                    };
                };
                #endregion
            }
            else 
            {
                #region txt

                SendMsg(EMsgType.Title, "   export data to txt file");

                if (string.IsNullOrWhiteSpace(outFileName))
                {
                    outFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + type + ".txt";
                }

                filePathList.Add(outFileName);


                if (string.IsNullOrEmpty(outFilePath))
                {
                    outFilePath = CommonHelp.GetAbsPath(filePathList.ToArray());
                } 

                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));

                string fieldSeparator = ",";
                string rowSeparator = NewLine;
                string tableSeparator = NewLine + NewLine + NewLine + NewLine;


                sqlRunConfig.TryGetValue("fieldSeparator", out fieldSeparator);
                sqlRunConfig.TryGetValue("rowSeparator", out rowSeparator);
                sqlRunConfig.TryGetValue("tableSeparator", out tableSeparator);


                bool isFirstTable = true;

                GetDataTableWriter = () =>
                {                   

                    StreamWriter writer = new StreamWriter(outFilePath, true);
                    writer.NewLine = NewLine;

                    bool isFirstRow = true;

                    return new DataTableWriter
                    {
                        OnDispose = () =>
                        {
                            writer.Dispose();
                        },
                        WriteData =
                        (dt) =>
                        {
                            if (isFirstTable)
                                isFirstTable = false;
                            else
                                writer.Write(tableSeparator);

                       
                            foreach (DataRow row in dt.Rows) 
                            {
                                if (isFirstRow)
                                    isFirstRow = false;
                                else
                                    writer.Write(rowSeparator);

                                bool isFirstCol = true;
                                foreach (var cell in row.ItemArray)
                                {
                                    if (isFirstCol) 
                                        isFirstCol = false;
                                    else 
                                        writer.Write(fieldSeparator);

                                    writer.Write(cell?.ToString());
                                }                               
                            }                           
                            writer.Flush();                             
                        }
                    };
                };
                #endregion
            }
            #endregion


            #region (x.3)分批读取数据并导出
            try
            {

                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo { type = type, ConnectionString = ConnectionString }))
                {
                    var startTime = DateTime.Now;

                    SendMsg(EMsgType.Title, "   from database " + conn.Database);

                    List<int> rowCounts = null;
                    int? sourceSumRowCount = null;

                    int importedSumRowCount = 0;
                    int curTbIndex = 0;
                    int? sumTableCount = null;

                    #region (x.x.1)按需构建sql语句                   
                    if (string.IsNullOrEmpty(sql)) 
                    {
                        if (inTableNames == null)
                        {
                            inTableNames=conn.GetAllTableName();
                        }

                        if (inTableNames.Count == 0) 
                        {
                            SendMsg(EMsgType.Err, "   导出失败，导入源没有数据。");
                            return;
                        }

                        sql = String.Join(";select * from ", inTableNames.Select(n=> conn.Quote(n)) );
                        sql = "select * from " + sql + ";";

                        sumTableCount = inTableNames.Count;

                        rowCounts = inTableNames.Select(tableName =>
                                Convert.ToInt32(conn.ExecuteScalar("select Count(*) from "+ conn.Quote(tableName), commandTimeout: commandTimeout))
                            ).ToList();

                        sourceSumRowCount = rowCounts.Sum();
                    }
                    #endregion

                    if (outTableNames == null)
                    {
                        outTableNames = inTableNames;
                    }                     

                    SendMsg(EMsgType.Title, "   sum row count: " + sourceSumRowCount);
                    SendMsg(EMsgType.Title, "   table count  : " + inTableNames?.Count);
                    SendMsg(EMsgType.Title, "   inTableNames : " + inTableNames?.Serialize());
                    SendMsg(EMsgType.Title, "   outTableNames: " + outTableNames?.Serialize());


                    

                    using (var dr = conn.ExecuteReader(sql, commandTimeout: commandTimeout))
                    {
                        do
                        {
                            //TODO
                            //var schemaTable=dr.GetSchemaTable();

                            //(x.x.1)
                            var tableName = outTableNames?[curTbIndex] ?? "table" + curTbIndex;
                            int? sourceRowCount = rowCounts?[curTbIndex];

                            #region (x.x.2)logical
                            try
                            {
                                int importedRowCount = 0;

                                SendMsg(EMsgType.Title, "");
                                SendMsg(EMsgType.Title, "");
                                SendMsg(EMsgType.Title, "");
                                SendMsg(EMsgType.Title, $"      [{(curTbIndex + 1)}/{sumTableCount??0}]start export table " + tableName);
                                if (sourceRowCount.HasValue) SendMsg(EMsgType.Title, $"                                                sourceRowCount:" + sourceRowCount);


                                using (var dtWriter = GetDataTableWriter())
                                {
                                    DataTable dt;

                                    while (true)
                                    {
                                        SendMsg(EMsgType.Nomal, " ");
                                        SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                                        dt = dr.ReadDataToDataTable(batchRowCount);
                                        if (dt == null)
                                        {
                                            SendMsg(EMsgType.Nomal, "           already read all data！");
                                            break;
                                        }

                                        SendMsg(EMsgType.Nomal, "           [x.x.2]write data ,row count:" + dt.Rows.Count);
                                        dt.TableName = tableName;
                                        dtWriter.WriteData(dt);

                                        importedRowCount += dt.Rows.Count;
                                        importedSumRowCount += dt.Rows.Count;

                                        SendMsg(EMsgType.Nomal,  "                      current              sum");
                                        SendMsg(EMsgType.Nomal, $"            imported: {importedRowCount }      {importedSumRowCount }");
                                    
                                        if (sourceRowCount.HasValue || sourceSumRowCount.HasValue) 
                                        {
                                            SendMsg(EMsgType.Nomal, $"            total :   {sourceRowCount ?? 0}    {sourceSumRowCount ?? 0}");

                                            SendMsg(EMsgType.Nomal, $@"            progress:   {
                                                (sourceRowCount.HasValue ? (((float)importedRowCount) / sourceRowCount.Value * 100).ToString("f2") : "    ")
                                                }%   {
                                                (sourceSumRowCount.HasValue ? (((float)importedSumRowCount) / sourceSumRowCount.Value * 100).ToString("f2") : "")
                                                }%");
                                        }                                        
                                    }
                                }
                                SendMsg(EMsgType.Title, $"           export table " + tableName + " success,row count:" + importedRowCount);

                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                                SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                            }
                            #endregion


                            //(x.x.3)初始化环境参数                           
                            curTbIndex++;

                        } while (dr.NextResult());
                    }
                    var span = (DateTime.Now - startTime);
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "   Export success");
                    SendMsg(EMsgType.Title, "   sum row count:" + importedSumRowCount);
                    SendMsg(EMsgType.Title, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");

                    var url = "/" + string.Join("/" , filePathList.Skip(1).ToArray());              
                    SendMsg(EMsgType.Html, $"<br/>成功导出数据，地址：<a href='{url}'>{url}</a>");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SendMsg(EMsgType.Err, "导出失败。" + ex.GetBaseException().Message);
            }             
            #endregion
        }
        #endregion



        #region (x.2)Import
        class DataTableReader : IDisposable
        {
            public Action OnDispose;
            public void Dispose()
            {
                OnDispose?.Invoke();
            }
            public Func<DataTable> ReadData;
        }


 
        public static void Import(
             Action<EMsgType, string> SendMsg,
             string filePath,
             string type,
             string ConnectionString, //数据库连接字符串。亦可从配置文件获取，如 sqler.config:SqlBackup.SqlServerBackup.ConnectionString
             bool createTable,
             bool delete,
             bool truncate
            )
        {             

            SendMsg(EMsgType.Title, "  Import");

           
            #region (x.2)连接字符串

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err, "Export error - invalid arg conn.");
                return;
            }

            //解析ConnectionString
            if (ConnectionString.StartsWith("sqler.config:"))
            {
                ConnectionString = SqlerHelp.sqlerConfig.GetStringByPath(ConnectionString.Substring("sqler.config:".Length));
            }


            if (type == "mysql")
            {
                //确保mysql连接字符串包含 "AllowLoadLocalInfile=true;"（用以批量导入数据）
                ConnectionString = "AllowLoadLocalInfile=true;" + ConnectionString;
            }
            else if (type == "mssql")
            {
                //确保mssql连接字符串包含 "persist security info=true;"（用以批量导入数据）
                ConnectionString = "persist security info=true;" + ConnectionString;
            }
            #endregion


            try
            {                 

                List<string> tableNames;
                List<int> rowCounts;

                Func<int, DataTableReader> GetDataTableReader;


                #region (x.3)get data from file
                if (Path.GetExtension(filePath).ToLower().IndexOf(".xls") >= 0)
                {
                    SendMsg(EMsgType.Title, "   import data from excel file");
                    tableNames = ExcelHelp.GetAllTableName(filePath);
                    rowCounts = ExcelHelp.GetAllTableRowCount(filePath);

                    GetDataTableReader = (index) => {

                        int sumRowCount = rowCounts[index];
                        int readedRowCount = 0;

                        return new DataTableReader
                        {
                            ReadData = () =>
                            {
                                if (readedRowCount >= sumRowCount) return null;

                                var dt = ExcelHelp.ReadTable(filePath, index, true, readedRowCount, DbPortLogical.batchRowCount);
                                readedRowCount += dt.Rows.Count;
                                return dt;
                            }
                        };
                    };


                }
                else
                {
                    SendMsg(EMsgType.Title, "   import data from sqlite file");
                    using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath))
                    {
                        tableNames = connSqlite.Sqlite_GetAllTableName();

                        rowCounts = tableNames.Select(tableName =>
                            Convert.ToInt32(connSqlite.ExecuteScalar("select Count(*) from "+ connSqlite.Quote(tableName), commandTimeout: DbPortLogical.commandTimeout))
                        ).ToList();
                    }



                    GetDataTableReader =
                        (index) =>
                        {
                            var tableName = tableNames[index];
                            var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath);
                            var dataReader = connSqlite.ExecuteReader("select * from " + connSqlite.Quote(tableName), commandTimeout: DbPortLogical.commandTimeout);

                            return new DataTableReader
                            {
                                OnDispose = () => {
                                    dataReader.Dispose();
                                    connSqlite.Dispose();
                                },
                                ReadData = () =>
                                {
                                    return dataReader.ReadDataToDataTable(DbPortLogical.batchRowCount);
                                }
                            };
                        };


                }
                #endregion


                #region (x.5)import data to database
                int sourceSumRowCount = rowCounts.Sum();  
      
                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo { type = type, ConnectionString = ConnectionString }))
                {
                    var startTime = DateTime.Now;


                    SendMsg(EMsgType.Title, "   to database " + conn.Database);

                    SendMsg(EMsgType.Title, "   sum row count: " + sourceSumRowCount);
                    SendMsg(EMsgType.Title, "   table count: " + tableNames.Count);
                    SendMsg(EMsgType.Title, "   table name: " + tableNames.Serialize());

                    int importedSumRowCount = 0;
                    for (var curTbIndex = 0; curTbIndex < tableNames.Count; curTbIndex++)
                    {
                        var tableName = tableNames[curTbIndex];
                        var sourceRowCount = rowCounts[curTbIndex];

                        using (var tableReader = GetDataTableReader(curTbIndex))
                        {


                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, $"       [{(curTbIndex + 1)}/{tableNames.Count}]start import table " + tableName + ",sourceRowCount:" + sourceRowCount);

                            //(x.x.1)read data
                            SendMsg(EMsgType.Nomal, " ");
                            SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                            var dt = tableReader.ReadData();
                            dt.TableName = tableName;

                            //(x.x.2)
                            if (createTable)
                            {
                                SendMsg(EMsgType.Title, "           [x.x.2]create table ");
                                try
                                {
                                    conn.CreateTable(dt);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }

                            //(x.x.3)
                            if (delete)
                            {
                                SendMsg(EMsgType.Title, "           [x.x.3]delete table ");
                                try
                                {
                                    conn.Execute("delete from  " + conn.Quote(dt.TableName) );
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }

                            //(x.x.4)
                            if (truncate)
                            {
                                SendMsg(EMsgType.Title, "           [x.x.4]truncate table ");
                                try
                                {
                                    conn.Execute("truncate table " + conn.Quote(dt.TableName));
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }


                            //(x.x.5)import data
                            int importedRowCount = 0;
                            try
                            {
                                while (true)
                                {

                                    SendMsg(EMsgType.Nomal, "           [x.x.5]write data,row count:" + dt.Rows.Count);
                                    dt.TableName = tableName;
                                    conn.BulkImport(dt);

                                    importedRowCount += dt.Rows.Count;
                                    importedSumRowCount += dt.Rows.Count;

                                    SendMsg(EMsgType.Nomal, "                      current progress:    " +
                                        (((float)importedRowCount) / sourceRowCount * 100).ToString("f2") + " % ,    "
                                        + importedRowCount + " / " + sourceRowCount);

                                    SendMsg(EMsgType.Nomal, "                      total progress:      " +
                                        (((float)importedSumRowCount) / sourceSumRowCount * 100).ToString("f2") + " % ,    "
                                        + importedSumRowCount + " / " + sourceSumRowCount);


                                    SendMsg(EMsgType.Nomal, " ");
                                    SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                                    dt = tableReader.ReadData();
                                    if (dt == null)
                                    {
                                        SendMsg(EMsgType.Nomal, "           already read all data！");
                                        break;
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                                SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                            }

                            SendMsg(EMsgType.Title, "                    import table " + tableName + " success,row count:" + importedRowCount);
                        }
                    }

                    var span = (DateTime.Now - startTime);

                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "   Import success");
                    SendMsg(EMsgType.Title, "   sum row count:" + importedSumRowCount);
                    SendMsg(EMsgType.Nomal, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
                }
                #endregion

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SendMsg(EMsgType.Err, "导入失败。" + ex.GetBaseException().Message);
            }
            finally
            {
                System.GC.Collect();
            }
        }
        #endregion


        #region (x.3) DataTransfer
        public static void DataTransfer(
           Action<EMsgType, string> SendMsg,
           string from_type,
           string from_ConnectionString,
           string from_sql,
           string to_type,
           string to_ConnectionString,

           bool createTable,
           bool delete,
           bool truncate
           )
        {
 

            SendMsg(EMsgType.Title, "  DataTransfer");

            //(x.1)参数非空校验
            if (string.IsNullOrWhiteSpace(from_type)
                || string.IsNullOrWhiteSpace(from_ConnectionString)
                || string.IsNullOrWhiteSpace(to_type)
                || string.IsNullOrWhiteSpace(to_ConnectionString)
                )
            {
                SendMsg(EMsgType.Err, "error - invalid arg.");
                return;
            }

            //确保mysql连接字符串包含 "AllowLoadLocalInfile=true;"（用以批量导入数据）
            if (to_type == "mysql") 
            {
                to_ConnectionString = "AllowLoadLocalInfile=true;" + to_ConnectionString;
            }          


            try
            {
                List<string> tableNames = null;
                List<int> rowCounts = null;

                Func<DataTableReader> GetDataTableReader;

                #region (x.2)init from_data
                SendMsg(EMsgType.Title, "   init from_data");
                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo
                { type = from_type, ConnectionString = from_ConnectionString }))
                {
                    if (string.IsNullOrWhiteSpace(from_sql))
                    {
                        tableNames = conn.GetAllTableName();
                        from_sql = string.Join(';', tableNames.Select(tableName => "select * from " + conn.Quote(tableName)));

                        rowCounts = tableNames.Select(tableName =>
                            Convert.ToInt32(conn.ExecuteScalar("select Count(*) from " + conn.Quote(tableName), commandTimeout: DbPortLogical.commandTimeout))
                        ).ToList();
                    }
                }


                #region SqlRunConfig
                var sqlRunConfig = DbPortLogical.GetSqlRunConfig(from_sql);
                if (sqlRunConfig.TryGetValue("tableNames", out var value))
                {
                    tableNames = value.Deserialize<List<string>>();
                }
                #endregion

                var curTbIndex = 0;

                GetDataTableReader =
                    () =>
                    {
                        var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo
                        { type = from_type, ConnectionString = from_ConnectionString });
                        var dataReader = conn.ExecuteReader(from_sql, commandTimeout: DbPortLogical.commandTimeout);
                        int tableIndex = 0;

                        return new DataTableReader
                        {
                            OnDispose = () => {
                                dataReader.Dispose();
                                conn.Dispose();
                            },
                            ReadData = () =>
                            {
                                if (curTbIndex < tableIndex)
                                {
                                    throw new Exception("系统出错！lith_20201004_01");
                                }

                                while (curTbIndex > tableIndex)
                                {
                                    dataReader.NextResult();
                                    tableIndex++;
                                }


                                var dt = dataReader.ReadDataToDataTable(DbPortLogical.batchRowCount);

                                if (dt == null)
                                {
                                    dataReader.NextResult();
                                    tableIndex++;
                                }
                                else
                                {
                                    dt.TableName = tableNames[tableIndex];
                                }
                                return dt;
                            }
                        };
                    };
                #endregion


                #region (x.3)import data to to_data
                int? sourceSumRowCount = rowCounts?.Sum();
                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo { type = to_type, ConnectionString = to_ConnectionString }))
                using (var tableReader = GetDataTableReader())
                {
                    var startTime = DateTime.Now;

                    SendMsg(EMsgType.Title, "   to database " + conn.Database);

                    SendMsg(EMsgType.Title, "   sum row count: " + sourceSumRowCount);
                    SendMsg(EMsgType.Title, "   table count: " + tableNames.Count);
                    SendMsg(EMsgType.Title, "   table name: " + tableNames.Serialize());

                    int importedSumRowCount = 0;
                    for (; curTbIndex < tableNames.Count; curTbIndex++)
                    {
                        var tableName = tableNames[curTbIndex];
                        int? sourceRowCount = rowCounts?[curTbIndex];
                        {
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, $"       [{(curTbIndex + 1)}/{tableNames.Count}]start import table " + tableName + ",sourceRowCount:" + sourceRowCount);

                            //(x.x.1)read data
                            SendMsg(EMsgType.Nomal, " ");
                            SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");

                            var dt = tableReader.ReadData();
                            if (dt == null)
                            {
                                SendMsg(EMsgType.Nomal, "           read none data！");
                                continue;
                            }
                            //(x.x.2)
                            if (createTable)
                            {
                                SendMsg(EMsgType.Title, "           [x.x.2]create table ");
                                try
                                {
                                    conn.CreateTable(dt);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }

                            //(x.x.3)
                            if (delete)
                            {
                                SendMsg(EMsgType.Title, "           [x.x.3]delete table ");
                                try
                                {
                                    conn.Execute("delete from  " + conn.Quote(dt.TableName));
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }

                            //(x.x.4)
                            if (truncate)
                            {
                                SendMsg(EMsgType.Title, "           [x.x.4]truncate table ");
                                try
                                {
                                    conn.Execute("truncate table " + conn.Quote(dt.TableName));
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }


                            //(x.x.5)import data
                            int importedRowCount = 0;
                            try
                            {
                                

                                while (true)
                                {

                                    SendMsg(EMsgType.Nomal, "           [x.x.5]write data,row count:" + dt.Rows.Count);

                                    conn.BulkImport(dt);

                                    importedRowCount += dt.Rows.Count;
                                    importedSumRowCount += dt.Rows.Count;

                                    SendMsg(EMsgType.Nomal, "                      current              sum");
                                    SendMsg(EMsgType.Nomal, $"            imported: {importedRowCount }      {importedSumRowCount }");

                                    if (sourceRowCount.HasValue || sourceSumRowCount.HasValue)
                                    {
                                        SendMsg(EMsgType.Nomal, $"            total :   {sourceRowCount ?? 0}    {sourceSumRowCount ?? 0}");

                                        SendMsg(EMsgType.Nomal, $@"            progress:   {
                                            (sourceRowCount.HasValue ? (((float)importedRowCount) / sourceRowCount.Value * 100).ToString("f2") : "    ")
                                            }%   {
                                            (sourceSumRowCount.HasValue ? (((float)importedSumRowCount) / sourceSumRowCount.Value * 100).ToString("f2") : "")
                                            }%");
                                    }


                                    SendMsg(EMsgType.Nomal, " ");
                                    SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                                    dt = tableReader.ReadData();
                                    if (dt == null)
                                    {
                                        SendMsg(EMsgType.Nomal, "           already read all data！");
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                                SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                            }
                            SendMsg(EMsgType.Title, "                    import table " + tableName + " success,row count:" + importedRowCount);
                        }
                    }

                    var span = (DateTime.Now - startTime);

                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "   DataTransfer success");
                    SendMsg(EMsgType.Title, "   sum row count:" + importedSumRowCount);
                    SendMsg(EMsgType.Nomal, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
                }
                #endregion

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SendMsg(EMsgType.Err, "失败。" + ex.GetBaseException().Message);
            }
            finally
            {
                System.GC.Collect();
            }
        }



        #endregion

    }
}
