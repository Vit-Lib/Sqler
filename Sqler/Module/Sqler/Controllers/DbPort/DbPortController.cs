using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Vit.Extensions;
using Vit.Core.Module.Log;
using System;
using Microsoft.AspNetCore.Http;
using Vit.Core.Util.ComponentModel.Model;
using System.Text;
using Vit.Core.Util.Common;
using System.IO;
using Vit.Orm.Dapper;
using Dapper;
using Vit.Db.Excel;
using System.Data;
using System.Linq;

namespace App.Module.Sqler.Controllers.DbPort
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/DbPort")]
    [ApiController]
    public class DbPortController : ControllerBase
    {

        int? commandTimeout => Vit.Orm.Dapper.DbHelp.CommandTimeout;

        static readonly int batchRowCount = Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<int?>("Sqler.DbPort_batchRowCount") ?? 100000;



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



        [HttpPost("Export")]
        public void Export
           ([FromForm] string type,
            [FromForm]string ConnectionString,
            [FromForm,SsDescription("sqlite、excel")]string exportFileType)
        {
            Response.ContentType = "text/html;charset=utf-8";


            SendMsg(EMsgType.Title, "   Export");


            //(x.1)连接字符串
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err ,"Export error - invalid arg conn.");
                return;
            }


            #region (x.2)构建数据导出回调

            string fileName;
     
            Func<DataTableWriter> GetDataTableWriter;

            if (exportFileType == "excel")
            {
                SendMsg(EMsgType.Title, "   export data to excel file");

                fileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong() + ".xlsx";
                string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", fileName);
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
               
            }
            else 
            {
                SendMsg(EMsgType.Title, "   export data to sqlite file");

                fileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong() + ".sqlite3";
                string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", fileName);
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
            }
            #endregion


            #region (x.3)分批读取数据并导出
            try
            {  
               
                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo { type = type, ConnectionString = ConnectionString }))               
                {
                    var startTime = DateTime.Now;
                
                    SendMsg(EMsgType.Title, "   from database " + conn.Database);


                    var tableNames = conn.GetAllTableName();

                    var rowCounts = tableNames.Select(tableName =>
                            Convert.ToInt32(conn.ExecuteScalar($"select Count(*) from {tableName}", commandTimeout: commandTimeout))
                        ).ToList();

                    int sourceSumRowCount = rowCounts.Sum();

                    SendMsg(EMsgType.Title, "   sum row count: " + sourceSumRowCount);
                    SendMsg(EMsgType.Title, "   table count: " + tableNames.Count);
                    SendMsg(EMsgType.Title, "   table name: " + tableNames.Serialize());




                    int importedSumRowCount = 0;

                    for (var curTbIndex = 0; curTbIndex < tableNames.Count; curTbIndex++)
                    {
                        var tableName = tableNames[curTbIndex];
                        var sourceRowCount = rowCounts[curTbIndex];                        
                     
                        try
                        {
                            int importedRowCount = 0;

                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, $"      [{(curTbIndex + 1)}/{tableNames.Count}]start export table " + tableName+ ",sourceRowCount:"+ sourceRowCount); 


                            using (var dtWriter= GetDataTableWriter())
                            using (var dr = conn.ExecuteReader($"select * from {tableName}", commandTimeout: commandTimeout))
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
                       

                                    SendMsg(EMsgType.Nomal, "                      current progress:    " +
                                        (((float)importedRowCount) / sourceRowCount * 100).ToString("f2") + " % ,    "
                                        + importedRowCount + " / " + sourceRowCount);

                                    SendMsg(EMsgType.Nomal, "                      total progress:      " +
                                        (((float)importedSumRowCount) / sourceSumRowCount * 100).ToString("f2") + " % ,    "
                                        + importedSumRowCount + " / " + sourceSumRowCount);
                                }
                            }
                            SendMsg(EMsgType.Title, $"           export table " + tableName+ " success,row count:" + importedRowCount);
                            
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }
                    var span = (DateTime.Now - startTime);
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "");
                    SendMsg(EMsgType.Title, "   Export success");
                    SendMsg(EMsgType.Title, "   sum row count:" + importedSumRowCount);
                    SendMsg(EMsgType.Title, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");

                    var url = "/temp/" + fileName;

                    Logger.Info("成功导出数据，地址：" + url);
                    SendMsg(EMsgType.Nomal, $"<br/>成功导出数据，地址：<a href='{url}'>{url}</a>");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SendMsg(EMsgType.Err, "导出失败。" + ex.GetBaseException().Message);
            }
            finally
            {
                System.GC.Collect();
            }
            #endregion
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
                string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", "DbPort_Import_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong() + file.FileName);
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
                             
                                var dt= ExcelHelp.ReadTable(filePath, index,true, readedRowCount, batchRowCount);
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
                            Convert.ToInt32(connSqlite.ExecuteScalar($"select Count(*) from {tableName}", commandTimeout: commandTimeout))
                        ).ToList();
                    }

                            

                    GetDataTableReader =
                        (index) =>
                            {
                                var tableName = tableNames[index];
                                var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath);
                                var dataReader=connSqlite.ExecuteReader($"select * from {tableName}", commandTimeout: commandTimeout);

                                return new DataTableReader
                                {
                                    OnDispose =   () => {
                                        dataReader.Dispose();
                                        connSqlite.Dispose();
                                    },
                                    ReadData = () =>
                                    {                                         
                                        return dataReader.ReadDataToDataTable(batchRowCount);
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

        #region util
        public enum EMsgType
        {         
            Err,
            Title,
            Nomal
        }


        void SendMsg(EMsgType type, String msg)
        {
            if (type == EMsgType.Err)
                Logger.Info("[Error]"+msg);
            else
                Logger.Info(msg);

            switch (type)
            {               
                case EMsgType.Err:
                    {
                        var escapeMsg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                        Response.WriteAsync("<br/><font style='color:#f00;font-weight:bold;'>" + escapeMsg + "</font>");
                        break;
                    }
                case EMsgType.Title:
                    {
                        var escapeMsg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                        Response.WriteAsync("<br/><font style='color:#005499;font-weight:bold;'>" + escapeMsg + "</font>");
                        break;
                    }
                default:
                    {
                        var escapeMsg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                        Response.WriteAsync("<br/>" + escapeMsg);
                        break;
                    }
            }
            //Response.Flush();            
        }

        static string Str2XmlStr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        stringBuilder.Append("&quot;");
                        break;
                    case '&':
                        stringBuilder.Append("&amp;");
                        break;
                    case '<':
                        stringBuilder.Append("&lt;");
                        break;
                    case '>':
                        stringBuilder.Append("&gt;");
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }
            }
            return stringBuilder.ToString();
        }

        #endregion
    }
}
