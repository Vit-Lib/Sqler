using App.Module.Sqler.Logical.DataEditor;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;
using Vit.Core.Module.Log;
using Vit.Core.Util.Common;
using Vit.Core.Util.ConfigurationManager;
using Vit.Db.DbMng;
using Vit.Db.DbMng.MsSql;
using Vit.Extensions;
using Vit.Orm.Dapper;
 

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
        /// (Member.2)
        /// </summary>
        public static JsonFile sqlerConfig { get; private set; }

        #region (Member.3)SqlServerBackup     

        public static System.Data.IDbConnection SqlServerBackup_CreateDbConnection() 
        {
            //Vit.Orm.Dapper.ConnectionFactory.GetConnectionCreator(new ConnectionInfo { type = "mssql", ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString") });

            return Vit.Orm.Dapper.ConnectionFactory.GetConnection(new ConnectionInfo { type = "mssql", ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString") });
        }



        public static MsSqlDbMng SqlServerBackup_CreateDbMng(System.Data.IDbConnection conn)
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

        public static MySqlConnection MySqlBackup_CreateDbConnection()
        {
            //确保mysql连接字符串包含 "AllowLoadLocalInfile=true;"（用以批量导入数据）
            return Vit.Orm.Dapper.ConnectionFactory.GetConnection(
                new ConnectionInfo
                {
                    type = "mysql",
                    ConnectionString = "AllowLoadLocalInfile=true;" + sqlerConfig.GetStringByPath("SqlBackup.MySqlBackup.ConnectionString")
                }) as MySqlConnection;
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

        public static void InitEnvironment(string dataDirectoryPath = null)
        {
            Logger.Info("[Sqler]init ...");

            #region (x.1)DataPath

            //(x.x.1)from appsettings.json
            if (string.IsNullOrWhiteSpace(dataDirectoryPath))
            {
                dataDirectoryPath = Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetStringByPath("Sqler.DataPath");
            }

            //(x.x.2)默认
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
                                           

            #region init member        
            sqlerConfig = new JsonFile(GetDataFilePath("sqler.json"));        

            #endregion

            SqlVersion.SqlVersionHelp.InitEnvironment();

            Logger.Info("[Sqler]inited!");
        }
        #endregion


        #region InitAutoTemp
        public static void InitAutoTemp()
        {
            Logger.Info("[Sqler.AutoTemp]init ...");
            #region AutoTemp SqlRun
            {
                //config
                global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                    new global::App.Module.Sqler.Logical.SqlRun.ConfigRepository().ToDataProvider("Sqler_SqlRun_Config"));
            }
            #endregion


            #region AutoTemp DataEditor
            {
                Task.Run(()=>{

                    Logger.Info("[Sqler.AutoTemp][DataEditor]init ...");

                    //RegistDataProvider
                    global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                        new global::App.Module.Sqler.Logical.DataEditor.ConfigRepository().ToDataProvider("Sqler_DataEditor_Config"));


                    //init
                    if (!DataEditorHelp.Init()) 
                    {
                        Logger.Info("[Sqler.AutoTemp][DataEditor] not config Database Connnection,not inited.");
                        return;
                    }
                   

                    Logger.Info("[Sqler.AutoTemp][DataEditor]init succeed!");                 
                });
            }
            #endregion

            #region AutoTemp SqlBackup
            {
                //config
                global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider( 
                    new global::App.Module.Sqler.Logical.SqlBackup.SqlServerBackup.ConfigRepository().ToDataProvider("Sqler_SqlBackup_SqlServerBackup_Config"));

                global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                   new global::App.Module.Sqler.Logical.SqlBackup.MySqlBackup.ConfigRepository().ToDataProvider("Sqler_SqlBackup_MySqlBackup_Config"));

                Logger.Info("[Sqler.AutoTemp]inited SqlBackup!");
            }
            #endregion


            // SqlVersion
            SqlVersion.SqlVersionHelp.InitAutoTemp();
            Logger.Info("[Sqler.AutoTemp]inited SqlVersion!");


            Logger.Info("[Sqler.AutoTemp]init succeed!");

        }
        #endregion





  

         


 

    }
}
