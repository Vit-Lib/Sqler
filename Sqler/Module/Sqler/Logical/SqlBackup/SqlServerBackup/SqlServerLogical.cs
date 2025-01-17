﻿using Vit.Core.Module.Log;

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
            using var conn = SqlerHelp.SqlServerBackup_CreateDbConnection();
            var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
            dbMng.CreateDataBase();
            Logger.Info("Sqler-CreateDataBase");
        }
        #endregion


        #region (x.2) DropDataBase


        public static void DropDataBase()
        {
            using var conn = SqlerHelp.SqlServerBackup_CreateDbConnection();
            var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
            dbMng.DropDataBase();
            Logger.Info("[Sqler]MsSqlDbMng-DropDataBase");
        }
        #endregion



        #region (x.3) AttachDataBase

        public static void AttachDataBase()
        {
            using var conn = SqlerHelp.SqlServerBackup_CreateDbConnection();
            var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
            dbMng.Attach();
            Logger.Info("[Sqler]MsSqlDbMng-AttachDataBase");
        }
        #endregion

        #region (x.4) DetachDataBase

        public static void DetachDataBase()
        {
            using var conn = SqlerHelp.SqlServerBackup_CreateDbConnection();
            var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
            dbMng.Detach();
            Logger.Info("[Sqler]MsSqlDbMng-DetachDataBase");
        }
        #endregion


        #region (x.5) KillProcess

        public static void KillProcess()
        {
            using var conn = SqlerHelp.SqlServerBackup_CreateDbConnection();
            var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
            dbMng.KillProcess();
            Logger.Info("[Sqler]MsSqlDbMng-KillProcess");
        }
        #endregion




        #region (x.6) BackupSqler

        public static void BackupSqler(string filePath = null, string fileName = null, bool useMemoryCache = true)
        {
            Logger.Info("[Sqler]MsSqlDbMng Sqler备份数据库...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.BackupSqler(filePath, useMemoryCache: useMemoryCache);
            }

            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MsSqlDbMng 数据库已Sqler备份");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion




        #region (x.7) BackupToLocalBak
        public static void BackupToLocalBak(string filePath = null, string fileName = null)
        {
            Logger.Info("[Sqler]MsSqlDbMng 备份数据库到ServerBak并下载到本地...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {

                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.BackupToLocalBak(filePath);
            }

            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MsSqlDbMng 数据库备份成功");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion



        #region (x.8) BackupLocalBak

        public static void BackupToServerBak(string filePath = null, string fileName = null)
        {
            Logger.Info("[Sqler]MsSqlDbMng 备份数据库到ServerBak文件...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                filePath = dbMng.BackupToServerBak(filePath);
            }

            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MsSqlDbMng 数据库备份成功");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion








        #region (x.9) Restore

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="force">若数据库已经存在，是否仍然还原</param>
        /// <param name="sliceMb">文件切片大小。默认：100,单位 MB。若指定小于备份文件大小的正数，则在传递文件到远程时进行分片传递。建议在备份文件过大时使用。</param>
        public static void Restore(string filePath = null, string fileName = null, bool force = true, int sliceMb = 100)
        {
            Logger.Info("[Sqler]MsSqlDbMng 远程还原数据库...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }

                if (!force)
                {
                    if (dbMng.GetDataBaseState() == Vit.Db.DbMng.EDataBaseState.online)
                    {
                        Logger.Info("[Sqler]MsSqlDbMng 已取消。数据库已经存在，且没有指定强制还原参数。");
                        return;
                    }
                }

                dbMng.Restore(filePath, sliceMb);
            }

            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MsSqlDbMng 已远程还原数据库");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion


        #region (x.10) RestoreServerBak
        public static void RestoreServerBak(string filePath = null, string fileName = null)
        {
            Logger.Info("[Sqler]MsSqlDbMng 通过server bak文件还原数据库...");
            var startTime = DateTime.Now;

            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);

                if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName))
                {
                    filePath = dbMng.BackupFile_GetPathByName(fileName);
                }
                filePath = dbMng.RestoreServerBak(filePath);
            }
            var span = (DateTime.Now - startTime);
            Logger.Info("[Sqler]MsSqlDbMng 数据库已通过bak文件还原");
            Logger.Info($"       耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
            Logger.Info("       filePath:" + filePath);
        }
        #endregion






    }
}
