using App.Module.Sqler.Logical.DataEditor;

using MySqlConnector;

using Vit.Core.Module.Log;
using Vit.Core.Util.Common;
using Vit.Core.Util.ConfigurationManager;
using Vit.Db.DbMng;
using Vit.Db.DbMng.MsSql;
using Vit.Db.Util.Data;
using Vit.Extensions;
using Vit.Extensions.Newtonsoft_Extensions;


namespace App.Module.Sqler.Logical
{
    public class SqlerHelp
    {



        #region (Member.1)DataPath

        /// <summary>
        /// Data文件夹绝对路径。
        /// </summary>
        public static string DataPath { get; private set; }

        public static string GetDataFilePath(params string[] path)
        {
            return Path.Combine(DataPath, Path.Combine(path));
            //return CommonHelp.GetAbsPathByRealativePath(Path.Combine(path))
        }
        #endregion


        /// <summary>
        /// Data/sqler.json
        /// </summary>
        public static JsonFile sqlerConfig { get; private set; }

        #region (Member.3)SqlServerBackup

        public static string SqlServer_FormatConnectionString(string oriConnStr)
        {
            //确保MsSql连接字符串包含 "persist security info=true;"（用以批量导入数据）
            return "persist security info=true;" + oriConnStr;
        }

        public static Microsoft.Data.SqlClient.SqlConnection SqlServerBackup_CreateDbConnection()
        {
            var ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString");
            ConnectionString = SqlServer_FormatConnectionString(ConnectionString);
            return ConnectionFactory.MsSql_GetConnection(ConnectionString);
        }



        public static MsSqlDbMng SqlServerBackup_CreateDbMng(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            var BackupPath = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.BackupPath");
            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                BackupPath = GetDataFilePath("SqlServerBackup");
            }

            string MdfPath = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.MdfPath");
            if (!string.IsNullOrEmpty(MdfPath))
            {
                MdfPath = CommonHelp.GetAbsPath(MdfPath);
            }

            return new MsSqlDbMng(conn, BackupPath, MdfPath);
        }

        #endregion






        #region (Member.4)MySqlBackup     

        public static string MySql_FormatConnectionString(string oriConnStr)
        {
            //确保连接字符串包含 "AllowLoadLocalInfile=true;"（用以批量导入数据）

            //确保连接字符串包含 "Old Guids=true;"（防止mysql自动把char(36)转换为GUID类型） 
            //https://www.cnblogs.com/tigerjacky/p/1901853.html

            return "AllowLoadLocalInfile=true;Old Guids=true;" + oriConnStr;
        }


        public static MySqlConnection MySqlBackup_CreateDbConnection()
        {
            var ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.MySqlBackup.ConnectionString");
            ConnectionString = MySql_FormatConnectionString(ConnectionString);
            return ConnectionFactory.MySql_GetConnection(ConnectionString);
        }

        public static MySqlDbMng MySqlBackup_CreateDbMng(MySqlConnection conn)
        {
            var BackupPath = sqlerConfig.GetStringByPath("SqlBackup.MySqlBackup.BackupPath");
            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                BackupPath = GetDataFilePath("MySqlBackup");
            }

            return new MySqlDbMng(conn, BackupPath);
        }

        public static string MySqlBackup_BackupPath
        {
            get
            {
                var BackupPath = sqlerConfig.GetStringByPath("SqlBackup.MySqlBackup.BackupPath");
                if (string.IsNullOrWhiteSpace(BackupPath))
                {
                    BackupPath = GetDataFilePath("MySqlBackup");
                }

                return BackupPath;
            }
        }
        #endregion







        #region InitEnvironment

        public static void InitEnvironment(string dataDirectoryPath, string[] args)
        {
            Logger.Info("[Sqler]init ...");

            #region #1 DataPath

            // #1.1 from appsettings.json
            if (string.IsNullOrWhiteSpace(dataDirectoryPath))
            {
                dataDirectoryPath = Appsettings.json.GetStringByPath("Sqler.DataPath");
            }

            // #1.2 from Data
            if (string.IsNullOrWhiteSpace(dataDirectoryPath))
            {
                dataDirectoryPath = "Data";
            }

            DirectoryInfo di = new DirectoryInfo(dataDirectoryPath);
            if (di.Exists)
            {
                DataPath = di.FullName;
                Logger.Info("[Sqler]Data Directory:  " + DataPath);
            }
            else
            {
                throw new Exception("[Sqler]Data Directory(" + dataDirectoryPath + ") does not exist");
            }
            #endregion


            // #2 init sqlerConfig
            sqlerConfig = new JsonFile(GetDataFilePath("sqler.json"));


            #region #3 --set path=value
            {
                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i - 1] == "--set")
                    {
                        try
                        {
                            var str = args[i];
                            var ei = str?.IndexOf('=') ?? -1;
                            if (ei < 1) continue;

                            var path = str.Substring(0, ei);
                            var value = str.Substring(ei + 1);

                            sqlerConfig.root.ValueSetByPath(value, path.Split('.'));
                        }
                        catch { }
                    }
                }
            }
            #endregion


            // #4
            //SqlVersion.SqlVersionHelp.InitEnvironment();


            Logger.Info("[Sqler]inited!");
        }
        #endregion


        #region InitAutoTemp
        public static void InitAutoTemp()
        {
            Logger.Info("[Sqler.AutoTemp]init ...");

            #region init SqlRun
            {
                Vit.AutoTemp.AutoTempHelp.RegistDataProvider(
                    new global::App.Module.Sqler.Logical.SqlRun.ConfigRepository().ToDataProvider("Sqler_SqlRun_Config"));
            }
            #endregion


            #region init DataEditor
            {
                Task.Run(() =>
                {

                    Logger.Info("[Sqler.AutoTemp][DataEditor] init ...");

                    //RegistDataProvider
                    Vit.AutoTemp.AutoTempHelp.RegistDataProvider(
                        new global::App.Module.Sqler.Logical.DataEditor.ConfigRepository().ToDataProvider("Sqler_DataEditor_Config"));


                    //init
                    if (!DataEditorHelp.Init())
                    {
                        Logger.Info("[Sqler.AutoTemp][DataEditor] not config Database Connection, not inited.");
                        return;
                    }


                    Logger.Info("[Sqler.AutoTemp][DataEditor] init succeed!");
                });
            }
            #endregion

            #region init SqlBackup
            {
                //config
                Vit.AutoTemp.AutoTempHelp.RegistDataProvider(
                    new global::App.Module.Sqler.Logical.SqlBackup.SqlServerBackup.ConfigRepository().ToDataProvider("Sqler_SqlBackup_SqlServerBackup_Config"));

                Vit.AutoTemp.AutoTempHelp.RegistDataProvider(
                   new global::App.Module.Sqler.Logical.SqlBackup.MySqlBackup.ConfigRepository().ToDataProvider("Sqler_SqlBackup_MySqlBackup_Config"));

                Logger.Info("[Sqler.AutoTemp] inited SqlBackup!");
            }
            #endregion


            // init SqlVersion
            SqlVersion.SqlVersionHelp.InitEnvironmentAndAutoTemp();
            Logger.Info("[Sqler.AutoTemp] inited SqlVersion!");


            Logger.Info("[Sqler.AutoTemp] init succeed!");

        }
        #endregion












    }
}
