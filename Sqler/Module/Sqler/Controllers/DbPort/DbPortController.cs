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

namespace Sqler.Module.Sqler.Controllers.DbPort
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/DbPort")]
    [ApiController]
    public class DbPortController : ControllerBase
    {




        #region (x.2) Export        

        [HttpPost("Export")]
        public void Export
           ([FromForm] string type,
            [FromForm]string ConnectionString,
            [FromForm,SsDescription("sqlite、excel")]string exportFileType)
        {
            Response.ContentType = "text/html;charset=utf-8";          


            //(x.1)连接字符串
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err ,"Export error - invalid arg conn.");
                return;
            }

            #region (x.2)构建导入操作回调

            string fileName;
            Action<DataTable> ExportDataTable;

            if (exportFileType == "excel")
            {
                SendMsg(EMsgType.Title, "   Export File is excel");

                fileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong() + ".xlsx";
                string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                ExportDataTable = (dt) =>
                {
                    //(x.x.3)import table
                    SendMsg(EMsgType.Nomal, "           [x.x.3]Export data");
                    ExcelHelp.SaveDataTable(filePath, dt);
                };
            }
            else 
            {
                SendMsg(EMsgType.Title, "   Export File is sqlite");

                fileName = "DbPort_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong() + ".sqlite3";
                string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                ExportDataTable = (dt) =>
                {
                    using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath)) 
                    {

                        //(x.x.2)create table
                        SendMsg(EMsgType.Nomal, "           [x.x.2]create table ");
                        connSqlite.Sqlite_CreateTable(dt);

                        //(x.x.3)import table
                        SendMsg(EMsgType.Nomal, "           [x.x.3]Export data"); 
                        connSqlite.Import(dt);
                    }
                };               
            }
            #endregion


            #region (x.3)Export data to db
            try
            {  
               
                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo { type = type, ConnectionString = ConnectionString }))               
                {
                    var startTime = DateTime.Now;
                    SendMsg(EMsgType.Title, "   Export");
                    SendMsg(EMsgType.Title, "   Export database " + conn.Database);


                    var tableNames = conn.GetAllTableName();
                    int tbCount = 0;
                    int sumRowCount = 0;
                    foreach (var tableName in tableNames)
                    {
                        tbCount++;
                        try
                        {
                            SendMsg(EMsgType.Title, "");
                            SendMsg(EMsgType.Title, $"      [{tbCount}/{tableNames.Count}]start backup table " + tableName);


                            //(x.x.1)get table data
                            SendMsg(EMsgType.Nomal, "           [x.x.1]get data ");
                            var dt = conn.ExecuteDataTable($"select * from {tableName}");
                            dt.TableName = tableName;

                            ExportDataTable(dt);

                            var rowCount = dt.Rows.Count;
                            sumRowCount += rowCount;
                            SendMsg(EMsgType.Title, "            success,row count:" + rowCount);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }
                    var span = (DateTime.Now - startTime);
                    SendMsg(EMsgType.Title, "   Export success");
                    SendMsg(EMsgType.Title, "   row count:" + sumRowCount);
                    SendMsg(EMsgType.Title, $"   耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");

                    var url = "/temp/" + fileName;

                    Logger.Info("成功导出数据，地址：" + url);
                    SendMsg(EMsgType.Html, $"<br/>成功导出数据，地址：<a href='{url}'>{url}</a>");
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


        #region (x.1) Import        

        [HttpPost("Import")]
        [DisableRequestSizeLimit]
        public void Import(
            [FromForm] IList<IFormFile> files,
            [FromForm] string type,
            [FromForm]string ConnectionString,
            [FromForm, SsDescription("on代表true")]string createTable,
            [FromForm]string delete,
            [FromForm]string truncate
            )
        {
            Response.ContentType = "text/html;charset=utf-8";            

            //(x.2)连接字符串
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SendMsg(EMsgType.Err, "import error - invalid arg conn.");
                return;
            }


            #region (x.3)检验zip文件是否合法       
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
                Func<int, DataTable> GetDataTable;

                #region (x.5)import excel data to database
                if (Path.GetExtension(filePath).ToLower().IndexOf(".xls") >= 0)
                {
                    SendMsg(EMsgType.Title, "   DataSource is Excel");
                    tableNames = ExcelHelp.GetAllTableName(filePath);

                    GetDataTable = (index) =>
                     {
                         return ExcelHelp.ReadTable(filePath, index);
                     };
                }
                else
                {
                    SendMsg(EMsgType.Title, "   DataSource is sqlite");
                    using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath))
                    {
                        tableNames = connSqlite.Sqlite_GetAllTableName();
                    }
                    GetDataTable = (index) =>
                    {
                        using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath))
                        {
                            var tableName = tableNames[index];
                            return connSqlite.ExecuteDataTable($"select * from {tableName}");
                        }
                    };
                }
                #endregion


                #region (x.5)import sqlite date to database

                using (var conn = ConnectionFactory.GetConnection(new Vit.Orm.Dapper.ConnectionInfo { type = type, ConnectionString = ConnectionString }))
                {
                    var startTime = DateTime.Now;

                    SendMsg(EMsgType.Title, "   import");
                    SendMsg(EMsgType.Title, "   import to database " + conn.Database);

                    int sumRowCount = 0;
                    for (var curTbIndex = 0; curTbIndex < tableNames.Count; curTbIndex++)
                    {
                        var tableName = tableNames[curTbIndex];
                        SendMsg(EMsgType.Title, "");
                        SendMsg(EMsgType.Title, $"       [{(curTbIndex + 1)}/{tableNames.Count}]start import table " + tableName);

                        //(x.x.1)read data
                        SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                        var dt = GetDataTable(curTbIndex);
                        dt.TableName = tableName;

                        //(x.x.2)
                        if (createTable == "on")
                        {
                            SendMsg(EMsgType.Nomal, "           [x.x.2]create table ");
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
                            SendMsg(EMsgType.Nomal, "           [x.x.3]delete table ");
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
                            SendMsg(EMsgType.Nomal, "           [x.x.4]truncate table ");
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


                        //(x.5)
                        SendMsg(EMsgType.Nomal, "           [x.x.5]import data ");
                        SendMsg(EMsgType.Nomal, "                      row count:" + dt.Rows.Count);
                        try
                        {
                            conn.ConnectionString = ConnectionString;
                            conn.BulkImport(dt);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                        sumRowCount += dt.Rows.Count;
                        SendMsg(EMsgType.Nomal, "                    import table " + dt.TableName + " success");
                    }

                    var span = (DateTime.Now - startTime);
                    SendMsg(EMsgType.Title, "   import success");
                    SendMsg(EMsgType.Title, "   sum row count:" + sumRowCount);
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
            Html,
            Err,
            Title,
            Nomal
        }


        void SendMsg(EMsgType type, String msg)
        {
            Logger.Info(msg);
           
            switch (type)
            {
                case EMsgType.Html:
                    {

                        Response.WriteAsync(msg);
                        break;
                    }
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
