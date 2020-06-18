using Sqler.Module.Sqler.Controllers.SqlBackup;
using Vit.ConsoleUtil;

namespace Sqler.Module.Sqler.ConsoleCommand
{
    public class SqlServerBackupCommand
    {

        #region CreateDataBase
        [Command("SqlServer.CreateDataBase")]
        [Remarks("若数据库不存在，则创建数据库。参数说明：")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径 例如 \"Data\"")]       
        [Remarks("示例： zip -i \"/data/a\" -o \"/data/a.zip\" ")]
        public static void CreateDataBase(string[] args)
        {
            ConsoleHelp.Log("创建数据库...");
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");

            SqlServerLogical.CreateDataBase();
            ConsoleHelp.Log("操作成功");
        }
        #endregion

    }
}
