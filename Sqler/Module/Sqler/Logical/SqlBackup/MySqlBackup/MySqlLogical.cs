using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
             
        }
        #endregion


        #region (x.2) DropDataBase
        public static void DropDataBase()
        {
            
        }
        #endregion





        #region (x.8) RemoteBackup

        public static void RemoteBackup(string filePath = null, string fileName = null)
        {
            //using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            //{
            //    var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);
            //    if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
            //    {
            //        filePath = Path.Combine(dbMng.BackupPath, fileName);
            //    }

            //    filePath = dbMng.RemoteBackup(filePath);

            //    Logger.Info("[Sqler]MsDbMng-RemoteBackup,filePath:" + filePath);
            //}
        }
        #endregion



        #region (x.9) RemoteRestore

        public static void RemoteRestore(string filePath = null, string fileName = null)
        {
            //using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            //{
            //    var dbMng = SqlerHelp.SqlServerBackup_CreateMsDbMng(conn);

            //    if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
            //    {
            //        filePath = Path.Combine(dbMng.BackupPath, fileName);
            //    }

            //    filePath = dbMng.RemoteRestore(filePath);

            //    Logger.Info("[Sqler]MsDbMng-RemoteRestore,filePath:" + filePath);
            //}
        }
        #endregion



    }
}
