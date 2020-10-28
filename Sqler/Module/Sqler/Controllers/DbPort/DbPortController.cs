using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Vit.Extensions;
using Vit.Core.Module.Log;
using System;
using Microsoft.AspNetCore.Http;
using Vit.Core.Util.ComponentModel.Model;
using Vit.Core.Util.Common;
using System.IO;
using Vit.Orm.Dapper;
using Dapper;
using Vit.Db.Excel;
using System.Data;
using System.Linq;
using Sqler.Module.Sqler.Logical.Message;
using Sqler.Module.Sqler.Logical.DbPort;

namespace App.Module.Sqler.Controllers.DbPort
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/DbPort")]
    [ApiController]
    public class DbPortController : ControllerBase
    {
        #region Util
        void SendMsg(EMsgType type, String msg)
        {
            MessageHelp.SendMsg(Response, type, msg);
        }
        #endregion





        #region (x.1) Export

        [HttpPost("Export")]
        [HttpGet("Export")]
        public void Export
           ([FromForm] string type,
            [FromForm]string ConnectionString,
            [FromForm,SsDescription("sqlite/excel/txt")]string exportFileType)
        {
            Response.ContentType = "text/html;charset=utf-8";

            DbPortLogical.ExportData(SendMsg, 
                type, ConnectionString,
                exportFileType
                );             
        }
        #endregion


        #region (x.2) Import

        class DataTableReader : IDisposable
        {
            public Action OnDispose;
            public void Dispose()
            {
                OnDispose?.Invoke();
            }
            public Func<DataTable> ReadData; 
        }


        [HttpPost("Import")]
        [DisableRequestSizeLimit]
        public void Import(
            [FromForm] IList<IFormFile> files,
            [FromForm] string type,
            [FromForm] string ConnectionString,
            [FromForm, SsDescription("on代表true")]string createTable,
            [FromForm] string delete,
            [FromForm] string truncate
            )
        {
            Response.ContentType = "text/html;charset=utf-8";

            SendMsg(EMsgType.Title, "  Import");

            //(x.2)连接字符串
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err, "import error - invalid arg conn.");
                return;
            }


            #region (x.3)检验文件是否合法       
            if (files == null || files.Count != 1)
            {
                SendMsg(EMsgType.Err, "请指定合法的文件");
                return;
            }
            #endregion

            try
            {
                #region (x.4)sqlite文件保存至本地       
                var file = files[0];
                string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", "Import_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong() + file.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    file.CopyTo(fs);
                }
                #endregion

                List<string> tableNames;
                List<int> rowCounts;

                Func<int, DataTableReader> GetDataTableReader;


                #region (x.5)get data from file
                if (Path.GetExtension(filePath).ToLower().IndexOf(".xls") >= 0)
                {
                    SendMsg(EMsgType.Title, "   import data from excel file");                
                    tableNames = ExcelHelp.GetAllTableName(filePath);
                    rowCounts = ExcelHelp.GetAllTableRowCount(filePath);

                    GetDataTableReader = (index) => {

                        int sumRowCount = rowCounts[index];
                        int readedRowCount = 0;

                        return new DataTableReader {
                            ReadData = () =>
                            { 
                                if (readedRowCount>= sumRowCount) return null; 
                             
                                var dt= ExcelHelp.ReadTable(filePath, index,true, readedRowCount, DbPortLogical.batchRowCount);
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

                        rowCounts = tableNames.Select(tableName=> 
                            Convert.ToInt32(connSqlite.ExecuteScalar($"select Count(*) from {tableName}", commandTimeout: DbPortLogical.commandTimeout))
                        ).ToList();
                    }

                            

                    GetDataTableReader =
                        (index) =>
                            {
                                var tableName = tableNames[index];
                                var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath);
                                var dataReader=connSqlite.ExecuteReader($"select * from {tableName}", commandTimeout: DbPortLogical.commandTimeout);

                                return new DataTableReader
                                {
                                    OnDispose =   () => {
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
                            SendMsg(EMsgType.Title, $"       [{(curTbIndex + 1)}/{tableNames.Count}]start import table " + tableName+ ",sourceRowCount:" + sourceRowCount);

                            //(x.x.1)read data
                            SendMsg(EMsgType.Nomal, " ");
                            SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                            var dt = tableReader.ReadData();
                            dt.TableName = tableName;

                            //(x.x.2)
                            if (createTable == "on")
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
                            if (delete == "on")
                            {
                                SendMsg(EMsgType.Title, "           [x.x.3]delete table ");
                                try
                                {
                                    conn.Execute("delete from  " + dt.TableName);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }

                            //(x.x.4)
                            if (truncate == "on")
                            {
                                SendMsg(EMsgType.Title, "           [x.x.4]truncate table ");
                                try
                                {
                                    conn.Execute("truncate table " + dt.TableName);
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
                                conn.ConnectionString = ConnectionString;
                               
                                while(true) {

                                    SendMsg(EMsgType.Nomal, "           [x.x.5]write data,row count:" + dt.Rows.Count);
                                    dt.TableName = tableName;
                                    conn.BulkImport(dt);

                                    importedRowCount += dt.Rows.Count;
                                    importedSumRowCount += dt.Rows.Count;                             

                                    SendMsg(EMsgType.Nomal, "                      current progress:    " + 
                                        (((float)importedRowCount) / sourceRowCount*100).ToString("f2") + " % ,    "
                                        + importedRowCount+" / " + sourceRowCount);

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

        [HttpPost("DataTransfer")]
        [DisableRequestSizeLimit]
        public void DataTransfer(

            [FromForm] string from_type,
            [FromForm] string from_ConnectionString,
            [FromForm] string from_sql,
            [FromForm] string to_type,
            [FromForm] string to_ConnectionString,

            [FromForm, SsDescription("on代表true")]string createTable,
            [FromForm] string delete,
            [FromForm] string truncate
            )
        {
            Response.ContentType = "text/html;charset=utf-8";

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
           

            try
            {                
                List<string> tableNames=null;
                List<int> rowCounts=null;

                Func<DataTableReader> GetDataTableReader;

                #region (x.2)init from_data
                SendMsg(EMsgType.Title, "   init from_data");
                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo
                    { type = from_type, ConnectionString = from_ConnectionString }))
                {
                    if (string.IsNullOrWhiteSpace(from_sql))
                    {
                        tableNames = conn.GetAllTableName();
                        from_sql = string.Join(';', tableNames.Select(m=>"select * from "+m));
                    
                        rowCounts = tableNames.Select(tableName =>
                            Convert.ToInt32(conn.ExecuteScalar($"select Count(*) from {tableName}", commandTimeout: DbPortLogical.commandTimeout))
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
                                

                                var dt= dataReader.ReadDataToDataTable(DbPortLogical.batchRowCount);

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
                            if (createTable == "on")
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
                            if (delete == "on")
                            {
                                SendMsg(EMsgType.Title, "           [x.x.3]delete table ");
                                try
                                {
                                    conn.Execute("delete from  " + dt.TableName);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                    SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                                }
                            }

                            //(x.x.4)
                            if (truncate == "on")
                            {
                                SendMsg(EMsgType.Title, "           [x.x.4]truncate table ");
                                try
                                {
                                    conn.Execute("truncate table " + dt.TableName);
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
                                conn.ConnectionString = to_ConnectionString;

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
