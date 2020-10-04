using Dapper;
using Sqler.Module.Sqler.Logical.MessageWrite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vit.Core.Module.Log;
using Vit.Core.Util.Common;
using Vit.Core.Util.ComponentModel.Model;
using Vit.Db.Excel;
using Vit.Extensions;
using Vit.Orm.Dapper;

namespace Sqler.Module.Sqler.Logical.DbPort
{
    public class DbPortLogical
    {



        public static int? commandTimeout => Vit.Orm.Dapper.DbHelp.CommandTimeout;

        public static readonly int batchRowCount = Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<int?>("Sqler.DbPort_batchRowCount") ?? 100000;



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

        public static Dictionary<string, string> GetSqlRunConfig(string sql) 
        {
            Dictionary<string, string> sqlRunConfig = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(sql)) return sqlRunConfig;

            //var regXml = new Regex(@"\<SqlRunConfig\>[\s\S]+?\<\/SqlRunConfig\>"); //正则匹配 <SqlRunConfig></SqlRunConfig>
            //var regTag = new Regex(@"\<[^\\]+?\>"); ; //正则匹配 <>

            var regXml = new Regex(@"(?<=\<SqlRunConfig\>)[\s\S]+?(?=\<\/SqlRunConfig\>)"); //正则匹配 <SqlRunConfig></SqlRunConfig> 中间的字符串
            var regTag = new Regex(@"(?<=\<)[^\/\<\>]+?(?=\>)"); //正则匹配 <> 中间的字符串(不含/)


            var matches = regXml.Matches(sql);
            if (matches.Count == 0) return sqlRunConfig;

            var xml = matches[0].Value;
            foreach (string tagName in regTag.Matches(xml).Select(i=>i.Value).Distinct()) 
            {            
                var values = new Regex($@"(?<=\<{tagName}\>)[\s\S]+?(?=\<\/{tagName}\>)").Matches(xml).Select(m => m.Value).ToArray();
                sqlRunConfig[tagName] = string.Join("",values);
            }
            return sqlRunConfig;
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

        public static void ExportData
           ( 
            Action<EMsgType, string> SendMsg,
            string type,string ConnectionString,
            [SsDescription("sqlite/excel/txt")]string exportFileType,            
            string sql = null,         List<string> inTableNames = null, //指定一个即可,若均不指定，则返回所有表
            string outFileName = null, List<string> outTableNames=null
            )
        {

            SendMsg(EMsgType.Title, "   Export");

            //(x.1)连接字符串
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err, "Export error - invalid arg conn.");
                return;
            }
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




            List<string> filePathList = new List<string> { "wwwroot", "temp", "Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_" + CommonHelp.NewGuid() };
            Func<DataTableWriter> GetDataTableWriter;

            #region (x.2)构建数据导出回调 

            if (exportFileType == "excel")
            {
                #region excel
              
                SendMsg(EMsgType.Title, "   export data to excel file");

                if (string.IsNullOrWhiteSpace(outFileName))
                {
                    outFileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")  + ".xlsx";
                }

                filePathList.Add(outFileName);

                string filePath = CommonHelp.GetAbsPath(filePathList.ToArray());

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                GetDataTableWriter = () =>
                {
                    int exportedRowCount = 0;

                    return new DataTableWriter
                    {
                        WriteData =
                        (dt) =>
                        {
                            SendMsg(EMsgType.Nomal, "           [x.x.3]Export data");
                            ExcelHelp.SaveDataTable(filePath, dt, exportedRowCount == 0, exportedRowCount);

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
                    outFileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")  + ".sqlite";
                }           
              
                filePathList.Add(outFileName);

                string filePath = CommonHelp.GetAbsPath(filePathList.ToArray());
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                GetDataTableWriter = () =>
                {
                    var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath);
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
            else 
            {
                #region txt

                SendMsg(EMsgType.Title, "   export data to txt file");

                if (string.IsNullOrWhiteSpace(outFileName))
                {
                    outFileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                }

                filePathList.Add(outFileName);

                string filePath = CommonHelp.GetAbsPath(filePathList.ToArray());

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                string fieldSeparator = ",";
                string rowSeparator = Environment.NewLine;
                string tableSeparator = Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine;


                sqlRunConfig.TryGetValue("fieldSeparator", out fieldSeparator);
                sqlRunConfig.TryGetValue("rowSeparator", out rowSeparator);
                sqlRunConfig.TryGetValue("tableSeparator", out tableSeparator);


                bool isFirstTable = true;

                GetDataTableWriter = () =>
                {                   

                    StreamWriter writer = new StreamWriter(filePath,true); 

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

                            bool isFirstRow = true;
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

                        sql = String.Join(";select * from ", inTableNames);
                        sql = "select * from " + sql + ";";

                        sumTableCount = inTableNames.Count;

                        rowCounts = inTableNames.Select(tableName =>
                                Convert.ToInt32(conn.ExecuteScalar($"select Count(*) from {tableName}", commandTimeout: commandTimeout))
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
                            var schemaTable=dr.GetSchemaTable();

                            //(x.x.1)
                            var tableName = inTableNames?[curTbIndex] ?? "table" + curTbIndex;
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

    }
}
