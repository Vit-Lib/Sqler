using System.Data;
using System.Text;
using System.Text.RegularExpressions;

using App.Module.Sqler.Logical;

using Microsoft.AspNetCore.Mvc;

using Sqler.Module.Sqler.Logical.DbPort;
using Sqler.Module.Sqler.Logical.Message;

using Vit.Core.Module.Log;
using Vit.Core.Module.Serialization;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Util.Data;
using Vit.Extensions.Db_Extensions;
using Vit.Extensions.Serialize_Extensions;

namespace App.Module.Sqler.Controllers.SqlRun
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/sqlrun")]
    [ApiController]
    public class SqlRunController : ControllerBase
    {

        #region ExecuteOnline

        [HttpPost("ExecuteOnline")]
        [HttpGet("ExecuteOnline")]
        public void ExecuteOnline([FromForm] string sql)
        {
            if (string.IsNullOrEmpty(sql)) sql = Request.Query["sql"];

            Response.ContentType = "text/html;charset=utf-8";

            ExecSql(SendMsg, sql);
        }

        static void ExecSql(Action<EMsgType, String> sendMsg, string sqlCode)
        {
            using var conn = ConnectionFactory.GetOpenConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Db.Util.Data.ConnectionInfo>("SqlRun.Config"));
            using var tran = conn.BeginTransaction();
            try
            {
                int index = 1;
                //  GO ，包括空格、制表符、换页符等          
                //Regex reg = new Regex("/\\*GO\\*/\\s*GO");
                Regex reg = new Regex("\\sGO\\s");
                var sqls = reg.Split(sqlCode);
                foreach (String sql in sqls)
                {
                    if (String.IsNullOrEmpty(sql.Trim()))
                    {
                        sendMsg(EMsgType.Title, $"[{(index++)}/{sqls.Length}]空语句，无需执行.");
                    }
                    else
                    {
                        sendMsg(EMsgType.Title, $"[{(index++)}/{sqls.Length}]执行sql语句：");
                        sendMsg(EMsgType.Nomal, sql);
                        var result = "执行结果:" + conn.Execute(sql, null, tran) + " Lines effected.";
                        sendMsg(EMsgType.Title, result);
                    }
                }
                tran.Commit();

                sendMsg(EMsgType.Title, "语句执行成功。");
                sendMsg(EMsgType.Nomal, "");
                sendMsg(EMsgType.Nomal, "");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                tran.Rollback();
                sendMsg(EMsgType.Err, "执行出错，原因：");
                sendMsg(EMsgType.Err, ex.GetBaseException().Message);
            }


        }

        #endregion

        #region Execute

        [HttpPost("Execute")]
        [HttpGet("Export")]
        public ApiReturn<int> Execute([FromForm] string sql)
        {
            if (string.IsNullOrEmpty(sql)) sql = Request.Query["sql"];

            using var conn = ConnectionFactory.GetConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Db.Util.Data.ConnectionInfo>("SqlRun.Config"));
            return conn.Execute(sql);
        }

        #endregion


        #region ExecuteDataSet

        [HttpPost("ExecuteDataSet")]
        public ApiReturn<object> ExecuteDataSet([FromForm] string sql)
        {
            try
            {
                DataSet ds;
                using (var conn = ConnectionFactory.GetConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Db.Util.Data.ConnectionInfo>("SqlRun.Config")))
                {
                    ds = conn.ExecuteDataSet(sql);
                }


                #region SqlRunConfig
                var sqlRunConfig = DbPortLogical.GetSqlRunConfig(sql);
                if (sqlRunConfig.TryGetValue("tableNames", out var value))
                {
                    var tableNames = Json.Deserialize<List<string>>(value);
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
        public void Export([FromForm] string sql, [FromForm] string exportFileType)
        {
            if (string.IsNullOrEmpty(sql)) sql = Request.Query["sql"];
            if (string.IsNullOrEmpty(exportFileType)) exportFileType = Request.Query["exportFileType"];

            Response.ContentType = "text/html;charset=utf-8";

            var connInfo = SqlerHelp.sqlerConfig.GetByPath<Vit.Db.Util.Data.ConnectionInfo>("SqlRun.Config");

            DbPortLogical.Export(SendMsg, connInfo.type, connInfo.connectionString, exportFileType, sql: sql);
        }
        #endregion





        #region GetMsSqlStructBuilder
        [HttpGet("GetMsSqlStructBuilder.sql")]
        public IActionResult GetMsSqlStructBuilder()
        {
            var DataBaseStructBuilder = @"
/*
<SqlRunConfig>
<fileName>CreateDataBase.sql</fileName>
<tableSeparator></tableSeparator> 
<rowSeparator></rowSeparator>
<fieldSeparator></fieldSeparator>
</SqlRunConfig>
*/

";

            DataBaseStructBuilder += Vit.Db.DbMng.MsSql.MsSqlDbMng.DataBaseStructBuilder;
            var bytes = DataBaseStructBuilder.StringToBytes();
            return File(bytes, "text/plain", "CreateDataBase.sql");
        }
        #endregion





        #region Util
        void SendMsg(EMsgType type, String msg)
        {
            MessageHelp.SendMsg(Response, type, msg);
        }
        #endregion





    }
}
