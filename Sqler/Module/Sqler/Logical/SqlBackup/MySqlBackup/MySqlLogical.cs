﻿using App.Module.Sqler.Logical;
using System;
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
            Logger.Info("[Sqler]MySqlDbMng 远程备份数据库...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);
                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.RemoteBackup(filePath);  
            }

            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MySqlDbMng 数据库已远程备份");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion



        #region (x.9) RemoteRestore

        public static void RemoteRestore(string filePath = null, string fileName = null)
        {
            Logger.Info("[Sqler]MySqlDbMng 远程还原数据库...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.RemoteRestore(filePath); 
            }

            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MySqlDbMng 数据库已远程还原");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion



    }
}
