using Dapper;
using MySql.Data.MySqlClient;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Vit.Core.Util.Common;
using Vit.Extensions;
using Vit.Orm.Dapper;

namespace Vit.Db.DbMng
{
    public class MySqlDbMng
    {


        #region 构造函数
        /// <summary>
        ///
        /// </summary>
        public MySqlDbMng(MySqlConnection conn, string BackupPath = null)
        {
            this.conn = conn;
            oriConnectionString = conn.ConnectionString;

            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                BackupPath = CommonHelp.GetAbsPath("Data", "MySqlBackup");
            }

            this.BackupPath = BackupPath;

            dbName = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(conn.ConnectionString).Database;
        }
        #endregion


        #region 成员变量

        MySqlConnection conn;
        string oriConnectionString;

        /// <summary>
        /// 数据库名称
        /// </summary>
        private string dbName { get; set; } = null;


        /// <summary>
        /// 备份时是否把空表也创建到数据文件中
        /// </summary>
        public bool createNullTableToBackupFile = false;


        #endregion





        #region Exec
        public T Exec<T>(Func<IDbConnection, T> run)
        {
            try
            {
                var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(oriConnectionString);
                builder.Database = "";
                conn.ConnectionString = builder.ToString();
                return run(conn);
            }
            finally
            {
                conn.ConnectionString = oriConnectionString;
            }
        }
        #endregion





        #region DataBaseIsOnline 数据库是否在线
        /// <summary>
        /// 数据库是否存在
        /// </summary>
        /// <returns></returns>
        public bool DataBaseExists()
        {
            return null != Exec(conn => conn.ExecuteScalar("show databases like @dbName", new { dbName = dbName }));
        }
        #endregion


        #region GetDataBaseState 获取数据库状态
        /// <summary>
        /// 获取数据库状态(online、none、unknow)
        /// </summary>
        /// <returns></returns>
        public EDataBaseState GetDataBaseState()
        {
            try
            {
                if (DataBaseExists())
                {
                    return EDataBaseState.online;
                }
            }
            catch
            {
                return EDataBaseState.unknow;
            }

            return EDataBaseState.none;
        }


        #endregion


        #region 获取数据库的当前连接数 

        /// <summary>
        /// 获取指定数据库的当前连接数
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public int GetProcessCount(string dbName = null)
        {
            // show full processlist  ;
            // select id, db, user, host, command, time, state, info	from information_schema.processlist	order by time desc 
            return Convert.ToInt32(Exec(conn => conn.ExecuteScalar("select count(*)	from information_schema.processlist	where db=@dbName", new { dbName = dbName ?? this.dbName })));
        }

        #endregion



        #region CreateDataBase 创建数据库       

        /// <summary>
        /// 创建数据库
        /// </summary>
        public void CreateDataBase()
        {
            Exec(conn => conn.Execute("create database `" + dbName+"`"));
        }
        #endregion

        #region DropDataBase 删除数据库       

        /// <summary>
        /// 删除数据库
        /// </summary>
        public void DropDataBase()
        {
            Exec(conn => conn.Execute("drop database `" + dbName + "`"));
        }

        #endregion





        #region RemoteBackup 远程备份

        #region RemoteBackup

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="filePath">备份的文件路径，若不指定则自动构建。demo:@"F:\\website\appdata\dbname_2020-02-02_121212.bak"</param>
        /// <returns>备份的文件路径</returns>
        public string RemoteBackup(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath)) filePath = Path.Combine(BackupPath, BuildBackupFileName(dbName));


            var tempPath = filePath + "_Temp";


            try
            {
                //(x.1)创建临时文件夹
                Directory.CreateDirectory(tempPath);



                #region (x.2)构建备份文件

                #region (x.x.1)创建建库语句文件（CreateDataBase.sql）
                var sqlPath = Path.Combine(tempPath, "CreateDataBase.sql");
                var sqlText = BuildCreateSql();
                File.WriteAllText(sqlPath, sqlText, System.Text.Encoding.GetEncoding("utf-8"));
                #endregion


                #region (x.x.2)获取所有表数据（Data.sqlite3）

                string sqlitePath = Path.Combine(tempPath, "Data.sqlite3");

                #region backup to sqlite           
                //using (var conn = ConnectionFactory.MySql_GetOpenConnection(sqlConnectionString))
                using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(sqlitePath))
                {
                    //Logger.Info("   mysql backup");
                    //Logger.Info("   backup database " + conn.Database);

                    var tableNames = conn.MySql_GetAllTableName();
                    int tbCount = 0;
                    int sumRowCount = 0;
                    foreach (var tableName in tableNames)
                    {
                        tbCount++;
                        //try
                        //{
                        //Logger.Info($"      [{tbCount}/{tableNames.Count}]start backup table " + tableName);

                        //(x.x.1)create table
                        //Logger.Info("           [x.x.1]create table " + tableName);
                        var dt = conn.ExecuteDataTable($"select * from `{tableName}` limit 1");

                        if (!createNullTableToBackupFile && dt.Rows.Count == 0) continue;

                        dt.TableName = tableName;
                        connSqlite.Sqlite_CreateTable(dt);

                        //(x.x.2)import table
                        //Logger.Info("           [x.x.2]import table " + tableName + " start...");
                        int rowCount;
                        using (IDataReader dr = conn.ExecuteReader($"select * from `{tableName}`"))
                        {
                            rowCount = connSqlite.Import(dr,tableName);
                        }
                        sumRowCount += rowCount;
                        //Logger.Info("            import table " + tableName + " success,row count:" + rowCount);
                        //}
                        //catch (Exception ex)
                        //{
                        //    Logger.Error(ex);
                        //}
                    }
                    //var span = (DateTime.Now - startTime);
                    //Logger.Info("   mysql backup success,sum row count:" + sumRowCount + $",耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
                }

                #endregion


                #endregion



                #endregion



                #region (x.3)压缩备份文件        
                //待压缩文件夹
                string input = tempPath;
                //压缩后文件名
                string output = filePath;

                var archiveType = ArchiveType.Zip;
                var compressionType = SharpCompress.Common.CompressionType.Deflate;

                var writerOptions = new WriterOptions(compressionType);
                writerOptions.ArchiveEncoding.Default = System.Text.Encoding.GetEncoding("utf-8");

                using (var fileStream = File.OpenWrite(output))
                using (var writer = WriterFactory.Open(fileStream, archiveType, writerOptions))
                {
                    writer.WriteAll(input, "*", SearchOption.AllDirectories);
                }
                #endregion

            }
            finally
            {
                Directory.Delete(tempPath, true);
            }

            return filePath;
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="fileName">备份文件的名称。demo:"dbname_2020-02-02_121212.bak"</param>
        /// <returns></returns>
        public string RemoteBackupByFileName(string fileName)
        {
            return RemoteBackup(BackupFile_GetPathByName(fileName));
        }


        static string BuildBackupFileName(string dbName)
        {
            // dbname_2010-02-02_121212.sqler.mysql.zip
            return $"{dbName}_{DateTime.Now.ToString("yyyy-MM-dd")}_{DateTime.Now.ToString("HHmmss")}.sqler.mysql.zip";
        }
        #endregion




        #region BuildCreateSql
       
        public string BuildCreateSql()
        {

            // show命令可以提供关于数据库、表、列，或关于服务器的状态信息
            // https://www.cnblogs.com/Rohn/p/11072228.html
            StringBuilder builder = new StringBuilder();
            {
                string dbName = conn.ExecuteScalar("select database()") as string;

                string delimiter = "/*GO*/";

                #region (x.1)构建标头  备份时间、MySQL版本、数据库名称 
                {
                    builder.AppendLine("-- (x.1)备份信息");
                    builder.AppendLine("-- 备份时间  ：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    builder.AppendLine("-- MySQL版本 ：" + conn.ExecuteScalar("select version()"));
                    builder.AppendLine("-- 数据库名称：" + dbName);

                    builder.AppendLine("-- DELIMITER " + delimiter);

                }
                #endregion



                #region (x.2)//建库

                #endregion


                #region (x.3)建表
                {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("-- (x.3)建表");

                    var names = conn.MySql_GetAllTableName();
                    int index = 0;
                    foreach (var name in names)
                    {
                        builder.AppendLine("  -- (x.x." + (++index) + ")创建表 " + name);

                        #region(x.x.x.1)建表语句
                        {
                            var dt = conn.ExecuteDataTable("SHOW CREATE table " + name);
                            string sql = dt.Rows[0][1] as string;
                            builder.Append(sql).AppendLine(";");
                            builder.AppendLine(delimiter);
                            builder.AppendLine();
                        }
                        #endregion


                        //建表语句已经制定索引，无需再次创建
                        continue;

                        #region(x.x.x.2)创建索引语句
                        {
                            #region const builder
                            string indexBuilderSql = @"SELECT
CONCAT('ALTER TABLE `',TABLE_NAME,'` ', 'ADD ', 
 IF(NON_UNIQUE = 1,
 CASE UPPER(INDEX_TYPE)
 WHEN 'FULLTEXT' THEN 'FULLTEXT INDEX'
 WHEN 'SPATIAL' THEN 'SPATIAL INDEX'
 ELSE CONCAT('INDEX `',
  INDEX_NAME,
  '` USING ',
  INDEX_TYPE
 )
END,
IF(UPPER(INDEX_NAME) = 'PRIMARY',
 CONCAT('PRIMARY KEY USING ',
 INDEX_TYPE
 ),
CONCAT('UNIQUE INDEX `',
 INDEX_NAME,
 '` USING ',
 INDEX_TYPE
)
)
),'(', GROUP_CONCAT(DISTINCT CONCAT('`', COLUMN_NAME, '`') ORDER BY SEQ_IN_INDEX ASC SEPARATOR ', '), ');') AS 'Show_Add_Indexes'
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = @dbName and TABLE_NAME=@tableName
--  and UPPER(INDEX_NAME) != 'PRIMARY'       -- 剔除主码
GROUP BY TABLE_NAME, INDEX_NAME
ORDER BY TABLE_NAME ASC, INDEX_NAME ASC;";
                            #endregion


                            var sqlList = conn.Query<string>(indexBuilderSql, new { dbName = dbName, tableName = name }).ToList();
                            foreach (var sql in sqlList)
                            {
                                builder.Append(sql).AppendLine(";");
                                builder.AppendLine(delimiter);
                                builder.AppendLine();
                            }
                        }
                        #endregion

                    }
                }
                #endregion


                #region (x.4)创建触发器
                {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("-- (x.4)创建触发器");
                    var names = conn.Query<string>("show TRIGGERS;").ToList();
                    var index = 0;
                    foreach (var name in names)
                    {
                        builder.AppendLine("  -- (x.x." + (++index) + ")创建触发器 " + name);

                        var dt = conn.ExecuteDataTable("SHOW CREATE TRIGGER " + name);
                        string sql = dt.Rows[0][2] as string;
                        builder.Append(sql).AppendLine(";");
                        builder.AppendLine(delimiter);
                        builder.AppendLine();
                    }
                }
                #endregion



                #region (x.5)创建事件
                {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("-- (x.5)创建事件");
                    var dtEvents = conn.ExecuteDataTable("show EVENTs;");
                    var index = 0;
                    foreach (DataRow row in dtEvents.Rows)
                    {
                        if (row["Db"].ToString() != dbName) continue;

                        var name = row["Name"].ToString();
                        var enabled = row["Status"].ToString().ToUpper() == "ENABLED";

                        //(x.x.1)创建事件
                        {
                            builder.AppendLine("  -- (x.x." + (++index) + ")创建事件 " + name);
                            var dt = conn.ExecuteDataTable("SHOW CREATE EVENT " + name);
                            string sql = dt.Rows[0][3] as string;
                            builder.Append(sql).AppendLine(";");
                            builder.AppendLine(delimiter);
                            builder.AppendLine();
                        }

                        //(x.x.2)启用事件
                        if (enabled)
                        {
                            builder.AppendLine("  -- (x.x." + index + ")启用事件 " + name);
                            string sql = "ALTER EVENT " + name + " ON COMPLETION PRESERVE ENABLE";
                            builder.Append(sql).AppendLine(";");
                            builder.AppendLine(delimiter);
                            builder.AppendLine();
                        }
                    }
                }
                #endregion


                #region (x.6)创建函数
                {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("-- (x.6)创建函数");
                    var dtName = conn.ExecuteDataTable("show FUNCTION status;");
                    var index = 0;
                    foreach (DataRow row in dtName.Rows)
                    {
                        if (row["Db"].ToString() != dbName) continue;

                        var name = row["Name"].ToString();

                        builder.AppendLine("  -- (x.x." + (++index) + ")创建函数 " + name);

                        var dt = conn.ExecuteDataTable("SHOW CREATE FUNCTION " + name);
                        string sql = dt.Rows[0][2] as string;
                        builder.Append(sql).AppendLine(";");
                        builder.AppendLine(delimiter);
                        builder.AppendLine();
                    }
                }
                #endregion




                #region (x.7)创建存储过程
                {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("-- (x.7)创建存储过程");
                  
                    var dtName = conn.ExecuteDataTable("show procedure status WHERE Db = @dbName AND `Type` = 'PROCEDURE'", new { dbName = dbName });


                    var index = 0;
                    foreach (DataRow row in dtName.Rows)
                    {
                        var name = row["Name"].ToString();

                        builder.AppendLine("  -- (x.x." + (++index) + ")创建存储过程 " + name);

                        var dt = conn.ExecuteDataTable("SHOW CREATE procedure " + name);
                        string sql = dt.Rows[0][2] as string;
                        builder.Append(sql).AppendLine(";");
                        builder.AppendLine(delimiter);
                        builder.AppendLine();
                    }
                }
                #endregion


                #region (x.8)创建视图                 
                {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("-- (x.8)创建视图");
                    //var dtName = conn.ExecuteDataTable("SELECT TABLE_NAME as Name from information_schema.VIEWS;");
                    var dtName = conn.ExecuteDataTable("SELECT TABLE_NAME as Name from information_schema.VIEWS where Table_Schema=@dbName", new { dbName = dbName });
                    var index = 0;
                    foreach (DataRow row in dtName.Rows)
                    {
                        var name = row["Name"].ToString();

                        builder.AppendLine("  -- (x.x." + (++index) + ")创建存储过程 " + name);

                        var dt = conn.ExecuteDataTable("SHOW CREATE view " + name);
                        string sql = dt.Rows[0][1] as string;
                        builder.Append(sql).AppendLine(";");
                        builder.AppendLine(delimiter);
                        builder.AppendLine();
                    }
                }
                #endregion



                //builder.AppendLine("-- DELIMITER ;");
            }

            return builder.ToString();
        }

        #endregion


        #endregion




        #region RemoteRestore 远程还原
        /// <summary>
        /// 通过备份文件名称远程还原数据库，备份文件在当前管理的备份文件夹中
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string RemoteRestoreByFileName(string fileName)
        {
            return RemoteRestore(BackupFile_GetPathByName(fileName));
        }

        /// <summary>
        /// 远程还原数据库   
        /// </summary>
        /// <param name="filePath">数据库备份文件的路径</param>
        /// <returns>备份文件的路径</returns>
        public string RemoteRestore(string filePath)
        {


            var tempPath = filePath + "_Temp";


            try
            {
                //(x.1)创建临时文件夹
                Directory.CreateDirectory(tempPath);


                #region (x.1)解压备份文件到临时文件夹
                //待解压文件
                var input = filePath;
                var output = tempPath;
                using (var archive = ArchiveFactory.Open(input))
                {
                    foreach (var entry in archive.Entries)
                    {
                        entry.WriteToDirectory(output, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                    }
                }
                #endregion


                #region (x.2)还原数据库

                //(x.x.1)若数据库存在，则删除数据库
                if (DataBaseExists()) DropDataBase();

                //创建数据库
                CreateDataBase();


                #region (x.x.2)创建建库语句文件（CreateDataBase.sql）
                var sqlPath = Path.Combine(tempPath, "CreateDataBase.sql");
                var sqlText = File.ReadAllText(sqlPath, System.Text.Encoding.GetEncoding("utf-8"));
                conn.Execute(sqlText);
                #endregion


                #region (x.x.3)导入所有表数据（Data.sqlite3）

                string sqlitePath = Path.Combine(tempPath, "Data.sqlite3");

                //using (var conn = ConnectionFactory.MySql_GetOpenConnection(sqlConnectionString))
                using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(sqlitePath))
                {
                    var tableNames = connSqlite.Sqlite_GetAllTableName();
                    int tbCount = 0;
                    int sumRowCount = 0;
                    foreach (var tableName in tableNames)
                    {
                        tbCount++;

                        //Logger.Info($"       [{tbCount}/{tableNames.Count}]start import table " + tableName);

                        //get data
                        using (var dr = connSqlite.ExecuteReader($"select * from `{tableName}`"))
                        {
                            //(x.4)
                            //Logger.Info("           [x.x.4]import table " + dt.TableName + ",row count:" + dt.Rows.Count);
                            var rowCount = conn.Import(dr, tableName);
                            sumRowCount += rowCount;
                            //Logger.Info("                    import table " + dt.TableName + " success");
                        }
                    }

                    //var span = (DateTime.Now - startTime);
                    //Logger.Info("   mysql import success,sum row count:" + sumRowCount + $",耗时:{span.Hours}小时{span.Minutes}分{span.Seconds}秒{span.Milliseconds}毫秒");
                }


                #endregion



                #endregion




            }
            finally
            {
                Directory.Delete(tempPath, true);
            }


            return filePath;

        }
        #endregion




        #region 备份文件夹


        /// <summary>
        /// 数据库备份文件的文件夹路径。例：@"F:\\db"
        /// </summary>
        private string BackupPath { get; set; }


        #region BackupFile_GetPathByName
        public string BackupFile_GetPathByName(string fileName)
        {
            return Path.Combine(BackupPath, fileName);
        }
        #endregion      


        #region BackupFile_GetFileInfos

        /// <summary>
        /// <para>获取所有备份文件的信息</para>
        /// <para>返回的DataTable的列分别为 Name（包含后缀）、Remark、Size</para>
        /// </summary>
        /// <returns></returns>
        public List<BackupFileInfo> BackupFile_GetFileInfos()
        {
            DirectoryInfo bakDirectory = new DirectoryInfo(BackupPath);
            if (!bakDirectory.Exists)
            {
                return new List<BackupFileInfo>();
            }
            return bakDirectory.GetFiles().Select(f => new BackupFileInfo { fileName = f.Name, size = f.Length / 1024.0f / 1024.0f, createTime = f.CreationTime }).ToList();
        }


        #endregion


        #endregion













    }
}
