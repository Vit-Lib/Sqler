using App.Module.Sqler.Logical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vit.Core.Module.Log;

namespace Sqler.Module.Sqler.Logical.SqlBackup.MySqlBackup
{
    public class MySqlLogical
    {

        ///  所有操作： 创建、删除
        ///             远程备份、远程还原
        ///          
        

        #region (x.1) CreateDataBase         
        public static void CreateDataBase()
        {
            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);               

                dbMng.CreateDataBase();
                Logger.Info("Sqler-CreateDataBase");
            }
        }
        #endregion


        #region (x.2) DropDataBase
        public static void DropDataBase()
        {
            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);

                dbMng.DropDataBase();
                Logger.Info("[Sqler]MsDbMng-DropDataBase");
            }

        }
        #endregion





        #region (x.8) RemoteBackup

        public static void RemoteBackup(string filePath = null, string fileName = null)
        {
            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);
                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.RemoteBackup(filePath);

                Logger.Info("[Sqler]MySqlDbMng-RemoteBackup,filePath:" + filePath);
            }             
        }
        #endregion



        #region (x.9) RemoteRestore

        public static void RemoteRestore(string filePath = null, string fileName = null)
        {
            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.RemoteRestore(filePath);

                Logger.Info("[Sqler]MySqlDbMng-RemoteRestore,filePath:" + filePath);
            }
        }
        #endregion



    }
}
