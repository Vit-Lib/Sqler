using App.Module.Sqler.Logical;
using Dapper;
using System;
using System.Data;
using Vit.ConsoleUtil;
using Vit.Db.Util.Data;
using Vit.Extensions;
using System.Linq;

namespace App.Module.Sqler.ConsoleCommand
{
    public class SqlRunCommand
    {
        #region Exec
        [Command("SqlRun.Exec")]
        [Remarks("执行sql语句。参数说明：")]
        [Remarks("--quiet (可选)静默模式，只打印结果信息，忽略info信息")]
        [Remarks("--sql 执行的sql语句")]
        [Remarks("--format (可选)显示结果的格式，可为 json（默认值，序列化为json字符串）、AffectedRowCount（仅显示影响行数）、FirstCell(仅返回第一行第一列数据)、Values(通过在行列直接加分隔符的方式返回所有数据，分隔符默认为逗号和换行，可通过--columnSeparator 和 --rowSeparator参数指定)")]
        [Remarks("--set (可选)设置配置文件（/Data/sqler.json）的值，格式为\"name=value\"。 连接字符串的name为SqlRun.Config.ConnectionString")]
        [Remarks("示例： SqlRun.Exec --quiet --sql \"select 1\" --format Values --set SqlRun.Config.type=sqlite --set \"SqlRun.Config.ConnectionString=Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" ")]
        public static void Exec(string[] args)
        {
            ConsoleHelp.Log("执行sql语句...");

            string sql = ConsoleHelp.GetArg(args, "--sql");
            string format = ConsoleHelp.GetArg(args, "--format");

            using (var conn = ConnectionFactory.GetConnection(SqlerHelp.sqlerConfig.GetByPath<Vit.Db.Util.Data.ConnectionInfo>("SqlRun.Config")))
            {
                string str;
                switch (format) 
                {
                    case "AffectedRowCount":
                        {
                            str = conn.Execute(sql).ToString();                           
                            break;
                        }
                    case "FirstCell":
                        {
                            str = conn.ExecuteScalar(sql)?.ToString();
                            break;
                        }
                    case "Values":
                        {
                            string columnSeparator = ConsoleHelp.GetArg(args, "--columnSeparator") ?? ",";
                            string rowSeparator = ConsoleHelp.GetArg(args, "--rowSeparator") ?? Environment.NewLine;

                            var dt = conn.ExecuteDataTable(sql);


                            str = dt.Rows.IEnumerable_ToList<DataRow>()
                                .Select(row =>
                                    row.ItemArray.Select(m => m.Serialize()).StringJoin(columnSeparator)
                                ).StringJoin(rowSeparator);
                            break;
                        }
                    default:
                        {
                            var dt = conn.ExecuteDataTable(sql);
                            str = dt.Serialize();
                            break;
                        }
                }

                ConsoleHelp.Out(str);
            }

            ConsoleHelp.Log("");
            ConsoleHelp.Log("操作成功");
        }
        #endregion

 
        

       

    }
}
