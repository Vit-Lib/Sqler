﻿using App.Module.Sqler.Logical;
using App.Module.Sqler.Logical.SqlBackup.MySqlBackup;
using Sqler.Module.Sqler.Logical.SqlBackup.MySqlBackup;
using Vit.ConsoleUtil;
using Vit.Extensions;

namespace App.Module.Sqler.ConsoleCommand
{
    public class MySqlCommand
    {

        #region CreateDataBase
        [Command("MySql.CreateDataBase")]
        [Remarks("若数据库不存在，则创建数据库。参数说明：")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]       
        [Remarks("示例： MySql.CreateDataBase -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" ")]
        public static void CreateDataBase(string[] args)
        {
            ConsoleHelp.Log("创建数据库...");

            #region SqlBackup.MySqlBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "MySqlBackup", "ConnectionString");                
            }
            #endregion           
           

            MySqlLogical.CreateDataBase();
            ConsoleHelp.Log("操作成功");
        }
        #endregion

        #region DropDataBase
        [Command("MySql.DropDataBase")]
        [Remarks("若数据库存在，则删除数据库。参数说明：")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： MySql.DropDataBase -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" ")]
        public static void DropDataBase(string[] args)
        {
            ConsoleHelp.Log("删除数据库...");

            #region SqlBackup.MySqlBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "MySqlBackup", "ConnectionString");
            }
            #endregion


            MySqlLogical.DropDataBase();
            ConsoleHelp.Log("操作成功");
        }
        #endregion

        #region RemoteRestore
        [Command("MySql.RemoteRestore")]
        [Remarks("通过备份文件远程还原数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-f[--force] 强制还原数据库。若指定此参数，则在数据库已经存在时仍然还原数据库；否则仅在数据库尚未存在时还原数据库。")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： MySql.RemoteRestore -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void RemoteRestore(string[] args)
        {
            ConsoleHelp.Log("通过备份文件远程还原数据库...");

            #region (x.1) arg SqlBackup.MySqlBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "MySqlBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");

            bool force = (ConsoleHelp.GetArg(args, "-f") ?? ConsoleHelp.GetArg(args, "--force"))!=null;


            MySqlLogical.Restore(filePath: filePath, fileName: fileName, force: force);

            ConsoleHelp.Log("操作成功");
        }
        #endregion    


        #region RemoteBackup
        [Command("MySql.RemoteBackup")]
        [Remarks("远程备份数据库。参数说明：备份文件名称和路径指定其一即可")]
        [Remarks("-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 \"DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-fp[--filePath] (可选)备份文件路径，例如 \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        [Remarks("-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\"")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： MySql.RemoteBackup -ConnStr \"Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;\" --filePath \"/root/docker/DbDev_2020-06-08_135203.bak\"")]
        public static void RemoteBackup(string[] args)
        {
            ConsoleHelp.Log("远程备份数据库...");

            #region (x.1) arg SqlBackup.MySqlBackup.ConnectionString
            string connStr = ConsoleHelp.GetArg(args, "-ConnStr") ?? ConsoleHelp.GetArg(args, "--ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                SqlerHelp.sqlerConfig.root.ValueSetByPath(connStr, "SqlBackup", "MySqlBackup", "ConnectionString");
            }
            #endregion

            string fileName = ConsoleHelp.GetArg(args, "-fn") ?? ConsoleHelp.GetArg(args, "--fileName");
            string filePath = ConsoleHelp.GetArg(args, "-fp") ?? ConsoleHelp.GetArg(args, "--filePath");


            MySqlLogical.SqlerBackup(filePath, fileName);

            ConsoleHelp.Log("操作成功");
        }
        #endregion

    }
}
