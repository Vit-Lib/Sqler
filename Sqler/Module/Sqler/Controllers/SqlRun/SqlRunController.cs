using System;
using System.Data;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using App.Module.Sqler.Logical;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Extensions;
using Vit.Orm.Dapper;
using Sqler.Module.Sqler.Logical.MessageWrite;
using Sqler.Module.Sqler.Logical.DbPort;
using System.Collections.Generic;

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


                #region SqlRunConfig
                var sqlRunConfig = DbPortLogical.GetSqlRunConfig(sql);
                if (sqlRunConfig.TryGetValue("tableNames", out var value))
                {
                    var tableNames = value.Deserialize<List<string>>();
                    for (var t = 0; t < tableNames.Count && t < ds.Tables.Count; t++)
                    {
                        ds.Tables[t].TableName = tableNames[t];
                    }
                }
                #endregion

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

        [HttpPost("Export")]
        [HttpGet("Export")]
        public void Export([FromForm]string sql, [FromForm]string exportFileType)
        {
            Response.ContentType = "text/html;charset=utf-8";

            var connInfo = SqlerHelp.sqlerConfig.GetByPath<Vit.Orm.Dapper.ConnectionInfo>("SqlRun.Config");

            DbPortLogical.ExportData(SendMsg, connInfo.type, connInfo.ConnectionString, exportFileType, sql);
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
