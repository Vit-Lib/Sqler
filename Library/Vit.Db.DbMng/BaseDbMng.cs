using Dapper;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Vit.Core.Module.Log;
using Vit.Extensions;
using Vit.Orm.Dapper;

namespace Vit.Db.DbMng
{
    public abstract class BaseDbMng<DbConnection>
        where DbConnection : IDbConnection
    {

        protected DbConnection conn;

        public BaseDbMng(DbConnection conn) 
        {
            this.conn = conn;
        }




        /// <summary>
        /// 获取数据库状态
        /// </summary>
        /// <returns></returns>
        public abstract EDataBaseState GetDataBaseState();
  

        /// <summary>
        /// 创建数据库
        /// </summary>
        public abstract void CreateDataBase();
 

        /// <summary>
        /// 删除数据库
        /// </summary>
        public abstract void DropDataBase();


        /// <summary>
        /// 数据库是否存在
        /// </summary>
        /// <returns></returns>
        public virtual bool DataBaseIsOnline()
        {
            return GetDataBaseState() == EDataBaseState.online;
        }




        /// <summary>
        /// 构建建库语句
        /// </summary>
        /// <returns></returns>
        public abstract string BuildCreateDataBaseSql();


        protected abstract string Quote(string name);


        #region BackupSqler
        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="filePath">备份的文件路径。demo:@"F:\\website\appdata\dbname_2020-02-02_121212.bak"</param>
        /// <returns>备份的文件路径</returns>
        public virtual string BackupSqler(string filePath)
        {
            var tempPath = filePath + "_Temp";


            try
            {
                //(x.1)创建临时文件夹
                Directory.CreateDirectory(tempPath);



                #region (x.2)构建备份文件

                #region (x.x.1)创建建库语句文件（CreateDataBase.sql）
                var sqlPath = Path.Combine(tempPath, "CreateDataBase.sql");
                var sqlText = BuildCreateDataBaseSql();
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

                    var tableNames = conn.GetAllTableName();
                    int tbCount = 0;
                    int sumRowCount = 0;
                    foreach (var tableName in tableNames)
                    {
                        tbCount++;
                        //try
                        //{
                        //Logger.Info($"      [{tbCount}/{tableNames.Count}]start backup table " + tableName);

                           
                        int rowCount;
                        using (IDataReader dr = conn.ExecuteReader($"select * from {Quote(tableName)}"))
                        {
                            //(x.x.1)create table
                            //Logger.Info("           [x.x.1]create table " + tableName);
                            connSqlite.Sqlite_CreateTable(dr, tableName);

                            //(x.x.2)import table
                            //Logger.Info("           [x.x.2]import table " + tableName + " start...");
                            rowCount = connSqlite.Import(dr, tableName);
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

        #endregion



        #region RestoreSqler

        protected virtual Regex RestoreSqler_SqlSplit => null;
        

        /// <summary>
        /// 远程还原数据库   
        /// </summary>
        /// <param name="filePath">数据库备份文件的路径</param>
        /// <returns>备份文件的路径</returns>
        public virtual string RestoreSqler(string filePath)
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
                if (DataBaseIsOnline()) DropDataBase();

                //创建数据库
                CreateDataBase();


                #region (x.x.2)创建建库语句文件（CreateDataBase.sql）
                var sqlPath = Path.Combine(tempPath, "CreateDataBase.sql");
                var sqlText = File.ReadAllText(sqlPath, System.Text.Encoding.GetEncoding("utf-8"));

                Action runSql = () => { 

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        int index = 1;
              
              
                        Regex reg = RestoreSqler_SqlSplit;
                        if (reg == null)
                        {
                            conn.Execute(sqlText, transaction: tran);
                        }
                        else
                        {
                            var sqls = reg.Split(sqlText);
                            foreach (String sql in sqls)
                            {
                                if (String.IsNullOrEmpty(sql.Trim()))
                                {
                                    //sendMsg(EMsgType.Title, $"[{(index++)}/{sqls.Length}]空语句，无需执行.");
                                }
                                else
                                {
                                    conn.Execute(sql,transaction: tran);
                                    //sendMsg(EMsgType.Title, $"[{(index++)}/{sqls.Length}]执行sql语句：");
                                    //sendMsg(EMsgType.Nomal, sql);
                                    //var result = "执行结果:" + conn.Execute(sql, null, tran) + " Lines effected.";
                                    //sendMsg(EMsgType.Title, result);
                                }
                            }
                        }
                        tran.Commit();               
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        tran.Rollback();
                        throw;
                    }
                }

                };


                //确保conn打开
                if (conn.State == ConnectionState.Open)
                {
                    runSql();
                }
                else 
                {
                    try
                    {
                        conn.Open();
                        runSql();
                    }
                    finally
                    {
                        conn.Close();
                    }
                }

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
                        using (var dr = connSqlite.ExecuteReader($"select * from {connSqlite.Quote(tableName)}"))
                        {
                            //(x.4)
                            //Logger.Info("           [x.x.4]import table " + dt.TableName + ",row count:" + dt.Rows.Count);
                            var rowCount = conn.BulkImport(dr, tableName);
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
    }
}
