using Sqler.Module.AutoTemp.Logical.Repository;
using Sqler.Module.Sqler.Logical.DataEditor;
using System;
using System.IO;
using System.Threading.Tasks;
using Vit.Core.Module.Log;
using Vit.Core.Util.ConfigurationManager;
using Vit.Extensions;
using Vit.Orm.Dapper;
using Vit.Orm.Dapper.DbMng;

namespace Sqler.Module.Sqler.Logical
{
    public class SqlerHelp
    {

        #region SqlDataPath        

        /// <summary>
        /// Data文件夹绝对路径。
        /// </summary>
        public static string SqlDataPath { get; private set; }

        public static void InitSqlDataPath(string[] args)
        {
            string dataDirectoryPath = null;

            //(x.1) from Commend args
            if (args != null && args.Length >= 1)
            {
                dataDirectoryPath = args[0];
            }

            //(x.2)from appsettings.json
            if (string.IsNullOrWhiteSpace(dataDirectoryPath))
            {
                dataDirectoryPath = Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetStringByPath("Sqler.DataPath");
            }

            //(x.3)默认
            if (string.IsNullOrWhiteSpace(dataDirectoryPath))
            {
                dataDirectoryPath = "Data";
            }

            DirectoryInfo di = new DirectoryInfo(dataDirectoryPath);
            if (di.Exists)
            {
                SqlDataPath = di.FullName;
                Logger.Info("[Sqler] Data Directory:" + SqlDataPath);
            }
            else 
            {
                throw new Exception("Data Directory(" + dataDirectoryPath + ") does not exist");
            }
        }
        #endregion




        public static string GetDataFilePath(params string[] path) 
        {      
            return Path.Combine(SqlDataPath, Path.Combine(path)); 
            //return CommonHelp.GetAbsPathByRealativePath(Path.Combine(path))
        }



        #region static Init
        public static void Init()
        {
            Logger.Info("init Sqler...");
            #region init member        
            sqlerConfig = new JsonFile(GetDataFilePath("sqler.json"));

            SqlServerBackup_CreateDbConnection =
            //Vit.Orm.Dapper.ConnectionFactory.GetConnectionCreator(new ConnectionInfo { type = "mssql", ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString") });
            () => Vit.Orm.Dapper.ConnectionFactory.GetConnection(new ConnectionInfo { type = "mssql", ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString") });

            #endregion



          


            #region SqlRun
            {
                //config
                AutoTemp.Controllers.AutoTempController.RegistDataProvider( 
                    new global::Sqler.Module.Sqler.Logical.SqlRun.ConfigRepository().ToDataProvider("Sqler_SqlRun_Config"));
            }
            #endregion


            #region DataEditor
            {
                Task.Run(()=>{

                    Logger.Info("init Sqler-DataEditor...");
                    //init
                    DataEditorHelp.Init();

                    //RegistDataProvider
                    AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                        new global::Sqler.Module.Sqler.Logical.DataEditor.ConfigRepository().ToDataProvider("Sqler_DataEditor_Config"));

                    Logger.Info("init Sqler-DataEditor succeed!");
                });
            }
            #endregion

            #region SqlBackup
            {
                //config
                AutoTemp.Controllers.AutoTempController.RegistDataProvider( 
                    new global::Sqler.Module.Sqler.Logical.SqlBackup.ConfigRepository().ToDataProvider("Sqler_SqlBackup_Config"));
            }
            #endregion


            // SqlVersion
            SqlVersion.SqlVersionHelp.Init();

            Logger.Info("inited Sqler!");

        }
        #endregion

        public static JsonFile sqlerConfig { get; private set; }



        #region SqlServerBackup     

        public static Func<System.Data.IDbConnection> SqlServerBackup_CreateDbConnection { get; private set; } 
        


        public static MsDbMng SqlServerBackup_CreateMsDbMng(System.Data.IDbConnection conn)
        {
            var BackupPath = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.BackupPath");
            if (string.IsNullOrWhiteSpace(BackupPath)) 
            {
                BackupPath = GetDataFilePath("SqlServerBackup");
            }
            return new MsDbMng(conn, BackupPath, sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.MdfPath"));
        }

        #endregion



    }
}
