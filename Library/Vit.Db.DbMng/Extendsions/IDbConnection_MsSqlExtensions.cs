using System.Data;
using Dapper;
using System;
using System.Data.SqlClient;
using Vit.Extensions;
using Vit.Core.Module.Log;
using Vit.Orm.Dapper;
using SqlConnection = System.Data.SqlClient.SqlConnection;
using System.IO;

namespace Vit.Db.DbMng.Extendsions
{
    public static partial class IDbConnection_MsSqlExtensions
    {
        /*
 --------------------------------------------------------------------------------------
先读后写

declare @fileContent varbinary(MAX);

select @fileContent=BulkColumn  from OPENROWSET(BULK 'T:\机电合并.zip', SINGLE_BLOB) as content;

if Exists(select top 1 * from sysObjects where Id=OBJECT_ID(N'sqler_temp_filebuffer') and xtype='U')
	drop table sqler_temp_filebuffer;

select @fileContent as fileContent into sqler_temp_filebuffer;


exec master..xp_cmdshell 'bcp "select null union all select ''0'' union all select ''0'' union all select null union all select ''n'' union all select null " queryout "T:\file.fmt" /T /c'



exec master..xp_cmdshell 'BCP "SELECT fileContent FROM sqler_temp_filebuffer" queryout "T:\file.zip" -T -i "T:\file.fmt"'



if Exists(select top 1 * from sysObjects where Id=OBJECT_ID(N'sqler_temp_filebuffer') and xtype='U')
	drop table sqler_temp_filebuffer;             
             
             */

        #region MsSql_ReadFileFromDisk   

        /// <summary>
        /// 读取SqlServer所在服务器中的文件内容，存储到本地。
        /// 若服务器中不存在指定的文件则抛异常。
        /// （文件内容直接存储到文件，可读取超大文件）
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="serverFilePath"></param>
        /// <param name="localFilePath"></param>
        /// <returns>读取的文件大小。单位：byte</returns>
        public static int ReadFileFromDisk(this SqlConnection conn, string serverFilePath, string localFilePath)
        {
            // Sql DataReader中读取大字段到文件的方法
            // https://www.cnblogs.com/sundongxiang/archive/2009/09/14/1566443.html

            // select BulkColumn  from OPENROWSET(BULK N'T:\机电合并.zip', SINGLE_BLOB) as content;      

            return conn.MsSql_RunUseMaster((c) =>
             {
                 return c.MakeSureOpen((_) =>
                 {
                     var sql = " select BulkColumn  from OPENROWSET(BULK N'" + serverFilePath + "', SINGLE_BLOB) as content";

                     int readedCount = 0;
                     using (var cmd = new SqlCommand())
                     {
                         cmd.Connection = conn;
                         cmd.CommandText = sql;

                         if (DapperConfig.CommandTimeout.HasValue)
                             cmd.CommandTimeout = DapperConfig.CommandTimeout.Value;

                         using (var dr = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                         {
                             if (dr.Read())
                             {
                                 Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

                                 using (var output = new FileStream(localFilePath, FileMode.Create))
                                 {
                                     int bufferSize = 100 * 1024;
                                     byte[] buff = new byte[bufferSize];

                                     while (true)
                                     {
                                         int buffCount = (int)dr.GetBytes(0, readedCount, buff, 0, bufferSize);

                                         output.Write(buff, 0, buffCount);
                                         readedCount += buffCount;

                                         if (buffCount < bufferSize) break;
                                     }
                                 }
                             }
                         }
                     }
                     return readedCount;
                 });
             });
        }


        /// <summary>
        /// 从磁盘读取文件内容(文件内容会先缓存到内存，若读取超大文件，请使用ReadFileFromDisk代替)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filePath"></param>
        /// <returns>读取的文件的内容</returns>
        public static byte[] MsSql_ReadFileFromDisk(this IDbConnection conn, string filePath)
        {
            // select BulkColumn  from OPENROWSET(BULK N'T:\机电合并.zip', SINGLE_BLOB) as content;                 

            return conn.MsSql_RunUseMaster((c) =>
            {
                return conn.ExecuteScalar<byte[]>(
                " select BulkColumn  from OPENROWSET(BULK N'" + filePath + "', SINGLE_BLOB) as content"
                , commandTimeout: DapperConfig.CommandTimeout);
            });
        }
        //*/

        #endregion


        #region MsSql_DeleteFileFromDisk    
        /// <summary>
        /// 从磁盘删除文件
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static void MsSql_DeleteFileFromDisk(this IDbConnection conn, string filePath)
        {
            conn.MsSql_Cmdshell("del \"" + filePath + "\"");
        }
        #endregion



        #region MsSql_WriteFileToDisk    
        /// <summary>
        /// 写入文件到磁盘
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filePath"></param>
        /// <param name="fileContent"></param>
        public static void MsSql_WriteFileToDisk(this IDbConnection conn, string filePath, byte[] fileContent)
        {
            string fmtFilePath = filePath + ".sqler.temp.fmt";

            conn.MsSql_Cmdshell(runCmd =>
            {

                #region (x.1)把文件内容写入到临时表
                conn.Execute(@"
if Exists(select top 1 * from sysObjects where Id=OBJECT_ID(N'sqler_temp_filebuffer') and xtype='U')
	drop table sqler_temp_filebuffer;
select @fileContent as fileContent into sqler_temp_filebuffer;
", new { fileContent }, commandTimeout: DapperConfig.CommandTimeout);
                #endregion

                try
                {

                    #region (x.2)写入二进制文件到磁盘
                    var log1 = runCmd("bcp \"select null union all select '0' union all select '0' union all select null union all select 'n' union all select null \" queryout \"" + fmtFilePath + "\" /T /c");
                    Logger.Info("[sqler]-MsDbMng 写入文件到磁盘. 创建fmt文件，outlog: " + log1.Serialize());

                    var log2 = runCmd("BCP \"SELECT fileContent FROM sqler_temp_filebuffer\" queryout \"" + filePath + "\" -T -i \"" + fmtFilePath + "\"");
                    Logger.Info("[sqler]-MsDbMng 写入文件到磁盘.创建文件，outlog: " + log2.Serialize());

                    #endregion

                }
                finally
                {
                    //(x.1)删除fmt文件
                    try
                    {
                        runCmd("del \"" + fmtFilePath + "\"");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    //(x.2)删除临时表
                    conn.Execute(@"
if Exists(select top 1 * from sysObjects where Id=OBJECT_ID(N'sqler_temp_filebuffer') and xtype='U')
	drop table sqler_temp_filebuffer;
", commandTimeout: DapperConfig.CommandTimeout);
                }

            });

        }
        #endregion




        #region MsSql_RunUseMaster
        public static T MsSql_RunUseMaster<T>(this IDbConnection conn, Func<IDbConnection, T> run)
        {
            string oriConnectionString = conn.ConnectionString;
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(oriConnectionString);
                builder.InitialCatalog = "";
                conn.ConnectionString = builder.ToString();
                return run(conn);
            }
            finally
            {
                conn.ConnectionString = oriConnectionString;
            }
        }
        #endregion




        #region MsSql_Cmdshell
        public static DataTable MsSql_Cmdshell(this IDbConnection conn, string cmd)
        {
            DataTable dt = null;
            conn.MsSql_Cmdshell(runCmd => dt = runCmd(cmd));
            return dt;
        }

        public static void MsSql_Cmdshell(this IDbConnection conn, Action<Func<string, DataTable>> handleToRun)
        {
            conn.MsSql_RunUseMaster((c) =>
            {

                bool advancedOptionsIsOpened = false;
                try
                {
                    advancedOptionsIsOpened = c.ExecuteDataTable("EXEC SP_CONFIGURE 'show advanced options'").Rows[0]["config_value"]?.Convert<string>() != "0";
                }
                catch (Exception)
                {
                }

                bool cmdshellIsOpened = false;
                try
                {
                    cmdshellIsOpened = c.ExecuteDataTable("EXEC SP_CONFIGURE 'xp_cmdshell'").Rows[0]["config_value"]?.Convert<string>() != "0";
                }
                catch (Exception)
                {
                }


                try
                {
                    if (!advancedOptionsIsOpened)
                        c.Execute(@"
--打开高级选项
EXEC SP_CONFIGURE 'show advanced options', 1;
RECONFIGURE;
", commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
                    if (!cmdshellIsOpened)
                        c.Execute(@"
--启用执行CMD命令
EXEC SP_CONFIGURE 'xp_cmdshell', 1;
RECONFIGURE;
", commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
                    Func<string, DataTable> runCmd = (cmd) => c.ExecuteDataTable("exec master..xp_cmdshell @cmd ", new { cmd });
                    handleToRun(runCmd);
                    return true;
                }
                finally
                {
                    if (!cmdshellIsOpened)
                        c.Execute(@"
--关闭执行CMD命令
EXEC SP_CONFIGURE 'xp_cmdshell', 0;
RECONFIGURE;
", commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);

                    if (!advancedOptionsIsOpened)
                        c.Execute(@"
--关闭高级选项
EXEC SP_CONFIGURE 'show advanced options', 0;
RECONFIGURE;
", commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);

                }

            });

        }
        #endregion

    }


}
