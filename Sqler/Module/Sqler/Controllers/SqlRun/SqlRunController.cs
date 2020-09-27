using System;
using System.Data;
using System.IO;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using App.Module.Sqler.Logical;
using Vit.Core.Module.Log;
using Vit.Core.Util.Common;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Excel;
using Vit.Extensions;
using Vit.Orm.Dapper;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Sqler.Module.Sqler.Logical.MessageWrite;

namespace App.Module.Sqler.Controllers.SqlRun
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/sqlrun")]
    [ApiController]
    public class SqlRunController : ControllerBase
    {
    


        #region Execute

        [HttpPost("Execute")]
        public ApiReturn<int> Execute([FromForm]string sql)
        {
            try
            {
                using (var conn = ConnectionFactory.GetConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Orm.Dapper.ConnectionInfo>("SqlRun.Config")))
                {
                    return conn.Execute(sql);
                }
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion


        #region ExecuteDataSet

        [HttpPost("ExecuteDataSet")]
        public ApiReturn<object> ExecuteDataSet([FromForm]string sql)
        {
            try
            {
                DataSet ds;
                using (var conn = ConnectionFactory.GetConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Orm.Dapper.ConnectionInfo>("SqlRun.Config")))
                {
                    ds = conn.ExecuteDataSet(sql);
                }


                #region build html
                StringBuilder builder = new StringBuilder("<div class=\"easyui-tabs\">");

                foreach (DataTable dt in ds.Tables)
                {
                    AppendXmlStr(builder.Append("<div class=\"dvdt\" title=\""), dt.TableName ?? "").Append("\" data-options=\"closable:true\"><table  cellspacing=\"1\" cellpadding=\"3\" border=\"0\"><tr class=\"trT\">");

                    foreach (DataColumn dc in dt.Columns)
                    {
                        AppendXmlStr(builder.Append("<td>"), dc.ColumnName ?? "").Append("</td>");
                    }
                    builder.Append("</tr>");
                    foreach (DataRow dr in dt.Rows)
                    {
                        builder.Append("<tr class=\"trC\">");
                        foreach (object oTemp in dr.ItemArray)
                        {
                            AppendXmlStr(builder.Append("<td>"), oTemp.ToString()).Append("</td>");
                        }
                        builder.Append("</tr>");
                    }
                    builder.Append("</table></div>");
                }
                builder.Append("</div>");
                return builder.ToString();
                #endregion
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }



        /// <summary>
        /// <para>向xml 的 内容 转换 并追加到 builder。                                         </para>
        /// <para>例如 转换为  &lt;a title=""&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）     </para>
        /// <para>或 转换为  &lt;a title=""&gt;ok&lt;/a&gt;  中title的值。                      </para>
        /// <para>                                                                              </para>
        /// <para>转换 " &amp;  &lt;   &gt;  为  &amp;quot;  &amp;amp;   &amp;lt;   &amp;gt; 。 </para>
        /// <para>注： ' 对应 &amp;apos; ，但有些浏览器不支持，故此函数不转换。                 </para>
        /// </summary>
        /// <param name="builder">不可为空</param>
        /// <param name="str">若为 空 或 空字符串，则原样返回</param>
        /// <returns>原 builder</returns>
        public static StringBuilder AppendXmlStr(StringBuilder builder, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return builder;
            }
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break;
                    //case '\'':
                    //    builder.Append("&apos;");
                    //    break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            return builder;
        }

        #endregion


         

        #region Export

        [HttpPost("{exportType}")]
        public void Export([FromForm]string sql, string exportType)
        {
            Response.ContentType = "text/html;charset=utf-8";

            string fileName=null;

            #region (x.1)查询数据
            SendMsg(EMsgType.Title, "   执行sql获取数据...");
            DataSet ds;
            using (var conn = ConnectionFactory.GetConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Orm.Dapper.ConnectionInfo>("SqlRun.Config")))
            {
                ds = conn.ExecuteDataSet(sql);
            }

            List<string> tableNames = new List<string>();
            #region 获取 表名等额外信息
            Regex ctrlAttribute = new Regex("\\[[^\\[\\]]+?\\]"); //正则匹配 [tableName:true] 
            foreach (Match item in ctrlAttribute.Matches(sql))
            {
                string key, value;

                #region (x.x.1)获取key value 用户配置信息
                var comm = item.Value.Substring(1, item.Value.Length - 2);

                SplitStringTo2(comm, ":", out key, out value);
                
                if (string.IsNullOrWhiteSpace(key)) continue;
                #endregion

                if (key == "tableName") 
                {
                    tableNames.AddRange(value.Split(","));
                }
                else if (key == "fileName")
                {
                    fileName = value;
                }
            }
            #region SplitStringTo2
            void SplitStringTo2(string oriString, string splitString, out string part1, out string part2)
            {
                int splitIndex = oriString.IndexOf(splitString);
                if (splitIndex >= 0)
                {
                    part1 = oriString.Substring(0, splitIndex);
                    part2 = oriString.Substring(splitIndex + splitString.Length);
                }
                else
                {
                    part1 = oriString;
                    part2 = null;
                }
            }
            #endregion
            #endregion


            for (var i = 0; i < tableNames.Count && i < ds.Tables.Count; i++) 
            {
                ds.Tables[i].TableName = tableNames[i];
            }
            #endregion



            #region (x.2)构建导入操作回调

            
            Action<DataTable> ExportDataTable;
            if (string.IsNullOrWhiteSpace(fileName)) 
            {
                fileName= "SqlRun_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_") + CommonHelp.NewGuidLong();
            }

            if (exportType == "ExportExcel")
            {
                SendMsg(EMsgType.Title, "   Export File is excel");

                fileName += ".xlsx";
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

                fileName += ".sqlite3";               
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
                var startTime = DateTime.Now;
                SendMsg(EMsgType.Title, "   Export to file");


                int tbCount = 0;
                int sumRowCount = 0;
                foreach (DataTable dt in ds.Tables)
                {
                    tbCount++;

                    var tableName = dt.TableName;
                    try
                    {
                        SendMsg(EMsgType.Title, "");
                        SendMsg(EMsgType.Title, $"      [{tbCount}/{ds.Tables.Count}]start backup table " + tableName);


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
                SendMsg(EMsgType.Title, $"<br/>成功导出数据，地址：<a href='{url}'>{url}</a>");

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SendMsg(EMsgType.Err, "导出失败。" + ex.GetBaseException().Message);
            }
            #endregion


        }



        #endregion



        #region Util
        void SendMsg(EMsgType type, String msg)
        {
            MessageWriteHelp.SendMsg(Response, type, msg);
        }
        #endregion





    }
}
