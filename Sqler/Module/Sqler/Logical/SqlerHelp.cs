using Sqler.Module.AutoTemp.Logical.Repository;
using Sqler.Module.Sqler.Logical.DataEditor;
using System;
using System.IO;
using System.Threading.Tasks;
using Vit.Core.Module.Log;
using Vit.Core.Util.Common;
using Vit.Core.Util.ConfigurationManager;
using Vit.Extensions;
using Vit.Orm.Dapper;
using Vit.Orm.Dapper.DbMng;

namespace Sqler.Module.Sqler.Logical
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

        public static Func<System.Data.IDbConnection> SqlServerBackup_CreateDbConnection { get; private set; }

        public static MsDbMng SqlServerBackup_CreateMsDbMng(System.Data.IDbConnection conn)
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

            return new MsDbMng(conn, BackupPath, MdfPath);
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

            SqlServerBackup_CreateDbConnection =
            //Vit.Orm.Dapper.ConnectionFactory.GetConnectionCreator(new ConnectionInfo { type = "mssql", ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString") });
            () => Vit.Orm.Dapper.ConnectionFactory.GetConnection(new ConnectionInfo { type = "mssql", ConnectionString = sqlerConfig.GetStringByPath("SqlBackup.SqlServerBackup.ConnectionString") });

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
                AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                    new global::Sqler.Module.Sqler.Logical.SqlRun.ConfigRepository().ToDataProvider("Sqler_SqlRun_Config"));
            }
            #endregion


            #region AutoTemp DataEditor
            {
                Task.Run(()=>{

                    Logger.Info("[Sqler.AutoTemp][DataEditor]init ...");

                    //RegistDataProvider
                    AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                        new global::Sqler.Module.Sqler.Logical.DataEditor.ConfigRepository().ToDataProvider("Sqler_DataEditor_Config"));


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
                AutoTemp.Controllers.AutoTempController.RegistDataProvider( 
                    new global::Sqler.Module.Sqler.Logical.SqlBackup.ConfigRepository().ToDataProvider("Sqler_SqlBackup_Config"));
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
