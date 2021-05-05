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
using Vit.Core.Util;
using Vit.Core.Util.Common;
using Vit.Core.Util.ComponentModel.Model;
using Vit.Db.Util.Csv;
using Vit.Db.Util.Data;
using Vit.Db.Util.Excel;
using Vit.Extensions;

namespace Sqler.Module.Sqler.Logical.DbPort
{
    public class DbPortLogical
    {
        public static string NewLine = "\r\n";

        public static int? commandTimeout => Vit.Db.Util.Data.ConnectionFactory.CommandTimeout;

        public static int batchRowCount => Vit.Db.BulkImport.BulkImport.batchRowCount;


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
            [SsDescription("sqlite/sqlite-NoMemoryCache/excel/csv/txt")]string exportFileType,
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
                ConnectionString = SqlerHelp.MySql_FormatConnectionString(ConnectionString);
            }
            else if (type == "mssql")
            {
                ConnectionString = SqlerHelp.SqlServer_FormatConnectionString(ConnectionString);
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
 

            Func<IDataReader, string,int> DataWriter=null;
            Action onDispose=null;

            int importedSumRowCount = 0;
            int? sourceRowCount=null;
            int? sourceSumRowCount = null;


            #region WriteProcess
            void WriteProcess(int importedRowCount) 
            {
                SendMsg(EMsgType.Nomal, "");

                if (sourceRowCount.HasValue)
                {
                    var process= (((float)importedRowCount) / sourceRowCount.Value * 100 ) .ToString("f2");
                    SendMsg(EMsgType.Nomal, $"           cur: [{process}%] {importedRowCount }/{sourceRowCount}");
                }
                else 
                {
                    SendMsg(EMsgType.Nomal, $"           cur: {importedRowCount }");
                }

                if (sourceSumRowCount.HasValue)
                {
                    var process = (((float)importedSumRowCount) / sourceSumRowCount.Value * 100).ToString("f2");
                    SendMsg(EMsgType.Nomal, $"           sum: [{process}%] {importedSumRowCount }/{sourceSumRowCount}");
                }
                else
                {
                    SendMsg(EMsgType.Nomal, $"           sum: {importedSumRowCount }");
                }                 
            }
            #endregion


            #region (x.2)构建数据导出回调 

            if (exportFileType == "sqlite" || exportFileType == "sqlite-NoMemoryCache")
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

                bool useMemoryCache = exportFileType == "sqlite";

                var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(useMemoryCache ? null : outFilePath);



                onDispose = () => 
                {
                    try
                    {
                        if (useMemoryCache && connSqlite!=null) 
                        {
                            using (var conn = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(outFilePath))
                            {
                                connSqlite.BackupTo(conn);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }


                    try
                    {
                        connSqlite?.Dispose();
                        connSqlite = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);                       
                    }                  
                
                };

                DataWriter = (dr, tableName) =>
                {
                    //(x.x.1)create table
                    SendMsg(EMsgType.Nomal, "           [x.x.1]create table ");
                    connSqlite.Sqlite_CreateTable(dr, tableName);

                    //(x.x.2)write data  
                    SendMsg(EMsgType.Nomal, "           [x.x.2]write data ");
 
                    
                    return connSqlite.Import(dr, tableName
                         , batchRowCount: batchRowCount, onProcess: (rowCount, sumRowCount) =>
                         {              
                             importedSumRowCount += rowCount;

                             WriteProcess(sumRowCount);
                         }
                        , useTransaction: true, commandTimeout: commandTimeout); 

            

                };

                #endregion
            }
            else if (exportFileType == "excel")
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

                DataWriter = (dr, tableName) =>
                {                  
                    SendMsg(EMsgType.Nomal, "           Export data");

                    int importedRowCount = ExcelHelp.SaveDataReader(outFilePath, dr, tableName,firstRowIsColumnName: true);

                    importedSumRowCount += importedRowCount;

                    WriteProcess(importedRowCount);

                    return importedRowCount;
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


                DataWriter = (dr, tableName) =>
                {
                    //(x.x.3)write data  
                    SendMsg(EMsgType.Title, "           [x.x.x]write data ");
                    int importedRowCount = 0;
                    while (true)
                    {      
                        int rowCount = CsvHelp.SaveToCsv(outFilePath, dr, firstRowIsColumnName: importedRowCount == 0, append: true, maxRowCount: batchRowCount);                       

                        importedRowCount += rowCount;
                        importedSumRowCount += rowCount;

                        WriteProcess(importedRowCount);

                        if (rowCount < batchRowCount)
                        {
                            break;
                        }
                    }
                    return importedRowCount;

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


                SendMsg(EMsgType.Title, "fieldSeparator:[" + fieldSeparator + "]");
                SendMsg(EMsgType.Title, "rowSeparator  :[" + rowSeparator + "]");
                SendMsg(EMsgType.Title, "tableSeparator:[" + tableSeparator + "]");


                bool isFirstTable = true;                

                DataWriter = (dr, tableName) =>
                {
                    using (StreamWriter writer = new StreamWriter(outFilePath, true))
                    {
                        writer.NewLine = NewLine;

                        if (isFirstTable)
                            isFirstTable = false;
                        else
                            writer.Write(tableSeparator);
            
                        SendMsg(EMsgType.Nomal, "           Export data");

                        int importedRowCount = 0;
                        int FieldCount = dr.FieldCount;
                        while (dr.Read()) 
                        {
                            if (importedRowCount != 0)
                                writer.Write(rowSeparator);

                            for (var i = 0; i < FieldCount; i++) 
                            {
                                if (i!=0)
                                    writer.Write(fieldSeparator);

                                writer.Write(dr.SerializeToString(i));                                
                            }
                            importedRowCount++;
                        }             

                        importedSumRowCount += importedRowCount;

                        WriteProcess(importedRowCount);

                        return importedRowCount;
                    }                  
                };

                #endregion
            }
            #endregion


            #region (x.3)分批读取数据并导出
            try
            {
                using (new Disposable(onDispose))
                using (var conn = ConnectionFactory.GetConnection(new ConnectionInfo { type = type, ConnectionString = ConnectionString }))
                {
                    var startTime = DateTime.Now;

                    SendMsg(EMsgType.Title, "   from database " + conn.Database);

                    List<int> rowCounts = null;

                    int curTbIndex = 0;
                    int? sumTableCount = null;

                    #region (x.x.1)按需构建sql语句                   
                    if (string.IsNullOrEmpty(sql))
                    {
                        if (inTableNames == null)
                        {
                            inTableNames = conn.GetAllTableName();
                        }

                        if (inTableNames.Count == 0)
                        {
                            SendMsg(EMsgType.Err, "   导出失败，导入源没有数据。");
                            return;
                        }

                        sql = String.Join(";select * from ", inTableNames.Select(n => conn.Quote(n)));
                        sql = "select * from " + sql + ";";

                        sumTableCount = inTableNames.Count;

                        rowCounts = inTableNames.Select(tableName =>
                                Convert.ToInt32(conn.ExecuteScalar("select Count(*) from " + conn.Quote(tableName), commandTimeout: commandTimeout))
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
                    SendMsg(EMsgType.Title, "   inTable      : " + inTableNames?.Serialize());
                    SendMsg(EMsgType.Title, "   outTable     : " + outTableNames?.Serialize());




                    using (var dr = conn.ExecuteReader(sql, commandTimeout: commandTimeout))
                    {
                        do
                        {

                            //(x.x.1)
                            var tableName = outTableNames?[curTbIndex] ?? "table" + curTbIndex;

                            sourceRowCount = rowCounts?[curTbIndex];

                            #region (x.x.2)导入
                            try
                            {

                                SendMsg(EMsgType.Nomal, "");
                                SendMsg(EMsgType.Nomal, "");
                                SendMsg(EMsgType.Nomal, "");
                                SendMsg(EMsgType.Title, $"      [{(curTbIndex + 1)}/{sumTableCount ?? 0}]start export table " + tableName);
                                if (sourceRowCount.HasValue) SendMsg(EMsgType.Nomal, $"                                                sourceRowCount:" + sourceRowCount);


                                int importedRowCount = DataWriter(dr, tableName);

                                SendMsg(EMsgType.Title, $"           export table " + tableName + " success");


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
                    SendMsg(EMsgType.Nomal, "");
                    SendMsg(EMsgType.Nomal, "");
                    SendMsg(EMsgType.Nomal, "");
                    SendMsg(EMsgType.Title, "   Export success");
                    SendMsg(EMsgType.Title, "   sum row count:" + importedSumRowCount);
                    SendMsg(EMsgType.Title, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");

                    var url = "/" + string.Join("/", filePathList.Skip(1).ToArray());
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
                ConnectionString = SqlerHelp.MySql_FormatConnectionString(ConnectionString);
            }
            else if (type == "mssql")
            {              
                ConnectionString = SqlerHelp.SqlServer_FormatConnectionString(ConnectionString);
            }
            #endregion


           
            try
            {                 

                List<string> tableNames;
                List<int> rowCounts;

                Func<string,int, DataTableReader> GetDataTableReader;


                #region (x.3)get data from file
                if (Path.GetExtension(filePath).ToLower().IndexOf(".xls") >= 0)
                {
                    //excel

                    SendMsg(EMsgType.Title, "   import data from excel file");
                    tableNames = ExcelHelp.GetAllTableName(filePath);
                    rowCounts = ExcelHelp.GetAllTableRowCount(filePath);

                    GetDataTableReader = (tableName,index) => {

                        int sumRowCount = rowCounts[index];
                        int readedRowCount = 0;

                        return new DataTableReader
                        {
                            GetDataTable = () =>
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
                    //sqlite

                    SendMsg(EMsgType.Title, "   import data from sqlite file");
                    using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath))
                    {
                        tableNames = connSqlite.Sqlite_GetAllTableName();

                        rowCounts = tableNames.Select(tableName =>
                            Convert.ToInt32(connSqlite.ExecuteScalar("select Count(*) from "+ connSqlite.Quote(tableName), commandTimeout: DbPortLogical.commandTimeout))
                        ).ToList();
                    }



                    GetDataTableReader =
                        (tableName, index) =>
                        {                        
                            var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath);
                            var dataReader = connSqlite.ExecuteReader("select * from " + connSqlite.Quote(tableName), commandTimeout: DbPortLogical.commandTimeout);

                            return new DataTableReader
                            {
                                OnDispose = () => {
                                    dataReader.Dispose();
                                    connSqlite.Dispose();
                                },
                                GetDataReader = () => {
                                    return dataReader;
                                }
                                
                            };
                        };


                }
                #endregion

                 

 

                #region (x.4)import data to database

                //(x.x.1)初始化
                var output = new DataOutput();
                output.SendMsg=SendMsg;
                output.type=type;
                output.ConnectionString=ConnectionString;  
                output.createTable=createTable;
                output.delete=delete;
                output.truncate=truncate;
                output.tableInfos = tableNames.Select( (tableName, index) =>
                    new TableInfo { tableName = tableName, tableIndex = index, rowCount = rowCounts[index] }
                ).ToList();
                output.sourceSumRowCount= rowCounts.Sum();

                output.GetDataTableReader = GetDataTableReader;

                //(x.x.2)数据导入
                var startTime = DateTime.Now;
                output.Output();

                var span = (DateTime.Now - startTime);
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "   Import success");
                SendMsg(EMsgType.Title, "    table count: " + tableNames?.Count + ",  row count:" + output.importedSumRowCount);
                SendMsg(EMsgType.Nomal, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
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

           
            if (from_type == "mysql")
            {               
                from_ConnectionString = SqlerHelp.MySql_FormatConnectionString(from_ConnectionString);
            }
            else if (from_type == "mssql")
            {                
                from_ConnectionString = SqlerHelp.SqlServer_FormatConnectionString(from_ConnectionString);
            }

            if (to_type == "mysql")
            { 
                to_ConnectionString = SqlerHelp.MySql_FormatConnectionString(to_ConnectionString);
            }
            else if (to_type == "mssql")
            {                
                to_ConnectionString = SqlerHelp.SqlServer_FormatConnectionString(to_ConnectionString);
            }

            try
            {
                List<string> tableNames = null;
                List<int> rowCounts = null;

                Func<string,int,DataTableReader> GetDataTableReader;

                #region (x.2)init from_data
                SendMsg(EMsgType.Title, "   init from_data");
                using (var conn = ConnectionFactory.GetConnection(new ConnectionInfo
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

            
                GetDataTableReader =
                    (tableName, curTbIndex) =>
                    {
                        var conn = ConnectionFactory.GetConnection(new ConnectionInfo
                        { type = from_type, ConnectionString = from_ConnectionString });
                        var dataReader = conn.ExecuteReader(from_sql, commandTimeout: DbPortLogical.commandTimeout);
                        int tableIndex = 0;

                        return new DataTableReader
                        {
                            OnDispose = () => {
                                dataReader.Dispose();
                                conn.Dispose();
                            },
                            GetDataReader = () => {

                                while (curTbIndex > tableIndex)
                                {
                                    dataReader.NextResult();
                                    tableIndex++;
                                }

                                return dataReader;
                            }
                        };
                    };
                #endregion



                #region (x.3)import data to database

                //(x.x.1)初始化
                var output = new DataOutput();
                output.SendMsg = SendMsg;
                output.type = to_type;
                output.ConnectionString = to_ConnectionString;
                output.createTable = createTable;
                output.delete = delete;
                output.truncate = truncate;
                output.tableInfos = tableNames.Select((tableName, index) =>
                   new TableInfo { tableName = tableName, tableIndex = index, rowCount = rowCounts?[index]??-1 }
                ).ToList();
                output.sourceSumRowCount = rowCounts?.Sum()??-1;

                output.GetDataTableReader = GetDataTableReader;

                //(x.x.2)数据导入
                var startTime = DateTime.Now;
                output.Output();

                var span = (DateTime.Now - startTime);
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "   DataTransfer success");
                SendMsg(EMsgType.Title, "   sum row count:" + output.importedSumRowCount);
                SendMsg(EMsgType.Nomal, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
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
