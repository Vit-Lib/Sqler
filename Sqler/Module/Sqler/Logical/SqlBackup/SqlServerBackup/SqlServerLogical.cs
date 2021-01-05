using App.Module.Sqler.Logical;
using System.IO;
using Vit.Core.Module.Log;

namespace App.Module.Sqler.Logical.SqlBackup.SqlServerBackup
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
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
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
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
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
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
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
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
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
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                dbMng.KillProcess();
                Logger.Info("[Sqler]MsDbMng-KillProcess");
            }       
        }
        #endregion




        #region (x.6) Backup
 
        public static void Backup(string filePath = null,string fileName=null)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.Backup(filePath);
                Logger.Info("[Sqler]MsDbMng-Backup,filePath:" + filePath);
            }
        }
        #endregion


        #region (x.7) Restore
    
        public static void Restore( string filePath=null, string fileName = null)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }
                filePath = dbMng.Restore(filePath);

                Logger.Info("[Sqler]MsDbMng-Restore,filePath:" + filePath);
            }   
        } 
        #endregion



        #region (x.8) RemoteBackup

        public static void RemoteBackup(string filePath = null, string fileName = null)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }                

                filePath = dbMng.RemoteBackup(filePath);

                Logger.Info("[Sqler]MsDbMng-RemoteBackup,filePath:" + filePath);
            }       
        }
        #endregion



        #region (x.9) RemoteRestore
     
        public static void RemoteRestore(string filePath = null, string fileName = null)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.RemoteRestore(filePath);

                Logger.Info("[Sqler]MsDbMng-RemoteRestore,filePath:" + filePath);
            }          
        }  
        #endregion


    }
}
