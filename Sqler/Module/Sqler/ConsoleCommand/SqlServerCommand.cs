using App.Module.Sqler.Logical;
using App.Module.Sqler.Logical.SqlBackup.SqlServerBackup;
using Vit.ConsoleUtil;
using Vit.Extensions;

namespace App.Module.Sqler.ConsoleCommand
{
    public class SqlServerCommand
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

        #region DropDataBase
        [Command("SqlServer.DropDataBase")]
        [Remarks("若数据库存在，则删除数据库。参数说明：")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.DropDataBase -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" ")]
        public static void DropDataBase(string[] args)
        {
            ConsoleHelp.Log("删除数据库...");

            #region SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion


            SqlServerLogical.DropDataBase();
            ConsoleHelp.Log("操作成功");
        }
        #endregion






        #region Restore
        [Command("SqlServer.Restore")]
        [Remarks("远程还原数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-f[--force] 强制还原数据库。若指定此参数，则在数据库已经存在时仍然还原数据库；否则仅在数据库尚未存在时还原数据库。")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.Restore -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void Restore(string[] args)
        {
            ConsoleHelp.Log("远程还原数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");

            bool force = (ConsoleHelp.GetArg(args, "-f") ?? ConsoleHelp.GetArg(args, "--force")) != null;

            SqlServerLogical.Restore(filePath: filePath, fileName: fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion


        #region RestoreLocalBak
        [Command("SqlServer.RestoreLocalBak")]
        [Remarks("通过本地bak文件还原数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.RestoreLocalBak -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void RestoreLocalBak(string[] args)
        {
            ConsoleHelp.Log("通过本地bak文件还原数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.RestoreLocalBak(filePath: filePath, fileName: fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion


        #region BackupBak
        [Command("SqlServer.BackupBak")]
        [Remarks("远程bak备份数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.BackupBak -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void BackupBak(string[] args)
        {
            ConsoleHelp.Log("远程bak备份数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.BackupBak(filePath, fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion



        #region BackupSqler
        [Command("SqlServer.BackupSqler")]
        [Remarks("Sqler备份数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.BackupSqler -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.zip\"")]
        public static void BackupSqler(string[] args)
        {
            ConsoleHelp.Log("Sqler备份数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.BackupSqler(filePath, fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion


        #region BackupLocalBak
        [Command("SqlServer.BackupLocalBak")]
        [Remarks("本地bak备份数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlServer.BackupLocalBak -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void BackupLocalBak(string[] args)
        {
            ConsoleHelp.Log("本地bak备份数据库...");

            #region (x.1) arg SqlBackup.SqlServerBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "SqlServerBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            SqlServerLogical.BackupLocalBak(filePath, fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion






    }
}
