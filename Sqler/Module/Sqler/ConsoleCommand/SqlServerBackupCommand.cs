using Sqler.Module.Sqler.Logical;
using Sqler.Module.Sqler.Logical.SqlBackup;
using Vit.ConsoleUtil;
using Vit.Extensions;

namespace Sqler.Module.Sqler.ConsoleCommand
{
    public class SqlServerBackupCommand
    {

        #region CreateDataBase
        [Command("SqlServer.CreateDataBase")]
        [Remarks("若数据库不存在，则创建数据库。参数说明：")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]       
        [Remarks("示例： SqlServer.CreateDataBase -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" ")]
        public static void CreateDataBase(string[] args)
        {
            ConsoleHelp.Log("创建数据库...");

            #region SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");                
            }
            #endregion           
           

            SqlServerLogical.CreateDataBase();
            ConsoleHelp.Log("操作成功");
        }
        #endregion


        #region Restore
        [Command("SqlServer.Restore")]
        [Remarks("通过备份文件还原数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.Restore -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void Restore(string[] args)
        {
            ConsoleHelp.Log("通过备份文件还原数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.Restore(filePath: filePath, fileName: fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion



        #region RemoteRestore
        [Command("SqlServer.RemoteRestore")]
        [Remarks("通过备份文件远程还原数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.RemoteRestore -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void RemoteRestore(string[] args)
        {
            ConsoleHelp.Log("通过备份文件远程还原数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.RemoteRestore(filePath: filePath, fileName: fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion




        #region Backup
        [Command("SqlServer.Backup")]
        [Remarks("备份数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.Backup -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void Backup(string[] args)
        {
            ConsoleHelp.Log("备份数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.Backup(filePath, fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion



        #region RemoteBackup
        [Command("SqlServer.RemoteBackup")]
        [Remarks("远程备份数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.RemoteBackup -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void RemoteBackup(string[] args)
        {
            ConsoleHelp.Log("远程备份数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.RemoteBackup(filePath, fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion



    }
}
