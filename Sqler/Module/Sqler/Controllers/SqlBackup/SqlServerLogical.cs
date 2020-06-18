using Sqler.Module.Sqler.Logical;
using Vit.Core.Module.Log;

namespace Sqler.Module.Sqler.Controllers.SqlBackup
{
    public static class SqlServerLogical
    {

        ///  所有操作： 创建、删除
        ///             附加、分离
        ///             强关所有连接
        ///             备份、还原
        ///             
        #region (x.1) CreateDataBase         
        public static void CreateDataBase()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                dbMng.CreateDataBase();
                Logger.Info("Sqler-CreateDataBase");
            }
        }
        #endregion


        #region (x.2) DropDataBase        

   
        public static void DropDataBase()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                dbMng.DropDataBase();
                Logger.Info("[Sqler]MsDbMng-DropDataBase");
            }       
        }
        #endregion



        #region (x.3) AttachDataBase        

       
        public static void AttachDataBase()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                dbMng.Attach();
                Logger.Info("[Sqler]MsDbMng-AttachDataBase");
            }      
        }
        #endregion

        #region (x.4) DetachDataBase
   
        public static void DetachDataBase()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                dbMng.Detach();
                Logger.Info("[Sqler]MsDbMng-DetachDataBase");
            }         
        }
        #endregion


        #region (x.5) KillProcess
  
        public static void KillProcess()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                dbMng.KillProcess();
                Logger.Info("[Sqler]MsDbMng-KillProcess");
            }       
        }
        #endregion




        #region (x.6) Backup
 
        public static void Backup()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                var filePath = dbMng.Backup();
                Logger.Info("[Sqler]MsDbMng-Backup,filePath:" + filePath);
            }
        }
        #endregion


        #region (x.7) Restore
    
        public static void Restore(string fileName)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                var filePath = dbMng.RestoreByFileName(fileName);
                Logger.Info("[Sqler]MsDbMng-Restore,filePath:" + filePath);
            }   
        }
        #endregion



        #region (x.8) RemoteBackup
 
        public static void RemoteBackup()
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                var filePath = dbMng.RemoteBackup();
                Logger.Info("[Sqler]MsDbMng-RemoteBackup,filePath:" + filePath);
            }       
        }
        #endregion



        #region (x.9) RemoteRestore
     
        public static void RemoteRestore(string fileName)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
                var filePath = dbMng.RemoteRestoreByFileName(fileName);
                Logger.Info("[Sqler]MsDbMng-RemoteRestore,filePath:" + filePath);
            }          
        }
        #endregion
    }
}
