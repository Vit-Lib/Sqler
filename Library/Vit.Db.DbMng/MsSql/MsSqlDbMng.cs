#region << 版本注释-v4 >>
/*
 * ========================================================================
 * 版本：v4
 * 时间：2021-01-21
 * 作者：lith
 * 邮箱：sersms@163.com
 * 说明： 
 * ========================================================================
*/
#endregion

using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vit.Core.Util.Common;
using Vit.Db.DbMng.Extendsions;
using Vit.Extensions;
using SqlConnection = System.Data.SqlClient.SqlConnection;

namespace Vit.Db.DbMng.MsSql
{
    /// <summary>
    ///  数据库 管理类
    ///  所有操作： 创建、删除
    ///             附加、分离
    ///             强关所有连接
    ///             备份、还原
    /// </summary>
    public partial class MsSqlDbMng : BaseDbMng<SqlConnection>
    {


        #region 构造函数
        /// <summary>
        ///
        /// </summary>
        public MsSqlDbMng(SqlConnection conn, string BackupPath = null, string mdfPath = null):base(conn)
        {
        

            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                BackupPath = CommonHelp.GetAbsPath("Data", "SqlServerBackup");
            }

            this.BackupPath = BackupPath;
            this.MdfFileDirectory = mdfPath;           
            dbName = new SqlConnectionStringBuilder(conn.ConnectionString).InitialCatalog;
        }
        #endregion


        #region 成员变量    


        /// <summary>
        /// 数据库名称
        /// </summary>
        private string dbName { get; set; } = null;


        #region 数据库的路径

        /// <summary>
        /// 数据库文件所在文件夹
        /// 空或空字符串：系统默认路径       其它：指定路径
        /// </summary>
        private string _MdfFileDirectory = null;
        /// <summary>
        /// 数据库文件所在文件夹。例：@"C:\Program Files (x86)\Microsoft SQL Server\MSSQL\data"
        /// 空或空字符串：系统默认路径       其它：指定路径
        /// </summary>
        public string MdfFileDirectory
        {
            get
            {
                return string.IsNullOrEmpty(_MdfFileDirectory) ? (_MdfFileDirectory = GetDefaultMdfDirectory()) : _MdfFileDirectory;
            }
            set
            {
                _MdfFileDirectory = value;
            }
        }


        #endregion

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



        #region GetMdfPath 获取数据库mdf文件的路径
        /// <summary>
        /// 
        /// 例：C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\Db_Dev_Data.MDF
        /// </summary>
        /// <returns></returns>
        private string GetMdfPath(string dbName=null)
        {
            return Exec((conn) =>
                 {
                     return conn.ExecuteScalar("select filename from   master.dbo.sysdatabases  where name=@dbName",
                         new { dbName = dbName ?? this.dbName }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout) as string;
                 });
        }
        #endregion


        #region GetDefaultMdfDirectory 获取默认数据库文件所在文件夹
        /// <summary>
        /// 获取默认数据库文件所在文件夹
        /// 注：获取的是第一个数据库的路径
        /// 例：C:\Program Files (x86)\Microsoft SQL Server\MSSQL\data
        /// </summary>
        /// <returns></returns>
        private string GetDefaultMdfDirectory()
        {            
            string directory =
                Exec((conn) =>
                {
                    return conn.ExecuteScalar("select filename from dbo.sysfiles where fileid = 1", commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout) as string;
                });

            return string.IsNullOrEmpty(directory) ? null : Path.GetDirectoryName(directory);
        }
        #endregion



        #region Exec

        /// <summary>
        /// return Exec((conn)=>{ return ""; });
        /// </summary>
        /// <param name="run"></param>
        private T Exec<T>(Func<IDbConnection, T> run)
        {
            return conn.MsSql_RunUseMaster(run);           
        }
        #endregion

        #region Quote
        protected override string Quote(string name)
        {
            return conn.Quote(name);
        }
        #endregion


        

        #region GetDataBaseState 获取数据库状态      

        /// <summary>
        /// 获取数据库状态
        /// </summary>
        /// <returns></returns>
        public override EDataBaseState GetDataBaseState()
        {
            try
            {
                var isOnline = Exec((conn) =>
                {
                    return 0 != conn.ExecuteScalar<int>("select count(1) from sysdatabases where name =@dbName", new { dbName = dbName }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
                });

                if (isOnline)
                {
                    return EDataBaseState.online;
                }
            }
            catch
            {
                return EDataBaseState.unknow;
            }

            try
            {
                GetPrimaryFileInfo(Path.Combine(MdfFileDirectory, dbName + "_Data.MDF"));
                return EDataBaseState.offline;
            }
            catch { }
            try
            {
                GetPrimaryFileInfo(Path.Combine(MdfFileDirectory, dbName + ".MDF"));
                return EDataBaseState.offline;
            }
            catch { }
            return EDataBaseState.none;
        }


        #endregion

                     

        #region  获取MDF文件的信息
        /// <summary>
        /// 获取MDF文件的信息
        /// </summary>
        /// <param name="mdfFilePath"></param>
        /// <returns></returns>
        public DataTable GetPrimaryFileInfo(string mdfFilePath)
        {
            return Exec(conn =>
            {
                return conn.ExecuteDataTable("dbcc checkprimaryfile (@mdfFilePath, 3);", new { mdfFilePath= mdfFilePath });           
            });
        }
        #endregion      


        #region GetAllDataBase 获取所有数据库
        /// <summary>
        /// 获取所有数据库
        /// </summary>
        /// <returns></returns>
        public DataTable GetAllDataBase()
        {
            return Exec((conn) =>
            {
                return conn.ExecuteDataSet("exec sp_helpdb;").Tables[0];
            });
        }
        #endregion



        #region GetDataBaseVersion      
        public string GetDataBaseVersion() 
        {
            try
            {
                return Exec((conn) =>
                {
                    return  conn.ExecuteScalar<string>("select @@version");
                });
            }
            catch  
            {               
            }
            return null;            
        }
        #endregion


        #region BuildCreateDataBaseSql
      
        public override string BuildCreateDataBaseSql()
        {     
            StringBuilder builder = new StringBuilder(); 
            #region 构建 建库语句           
            using (var dr = conn.ExecuteReader(DataBaseStructBuilder)) 
            {
                while(true) 
                {
                    if (!dr.Read()) 
                    {
                        if (dr.NextResult())
                        {
                            continue;
                        }
                        else 
                        {
                            break;
                        }
                    }

                    int FieldCount = dr.FieldCount;
                    for (var index = 0; index < FieldCount; index++) 
                    {
                        builder.Append(dr[index]);
                    }
                }
            }
            #endregion

            return builder.ToString();
        }
        #endregion


        #region CreateDataBase 创建数据库       

        /// <summary>
        /// 若数据库不存在，则创建数据库
        /// </summary>
        public override void CreateDataBase()
        {

            var strDBName = dbName;

            StringBuilder builder = new StringBuilder("if not Exists (select 1 from sysdatabases where name =N'").Append(strDBName).Append("' )  create database [").Append(dbName).Append("] ");

            //使用设定的路径创建数据库
            if (!string.IsNullOrEmpty(MdfFileDirectory))
            {
                string strDBPath = Path.Combine(MdfFileDirectory.Replace("'", "''") , strDBName);
                builder.Append(" ON PRIMARY ( NAME = N'").Append(strDBName).Append("_Data',FILENAME = N'").Append(strDBPath).Append("_Data.MDF' ,FILEGROWTH = 10%) LOG ON ( NAME =N'").Append(strDBName).Append("_Log',FILENAME = N'").Append(strDBPath).Append("_Log.LDF' ,FILEGROWTH = 10%) ");
            }
            builder.Append("; if Exists(select 1 from sysdatabases where  name ='").Append(strDBName).Append("' and (status & 0x200) != 0) ALTER DATABASE [").Append(dbName).Append("] SET ONLINE; ");
            Exec((conn) => { return conn.Execute(builder.ToString(), commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout); });

            /*

--若数据库不存在，则创建数据库
use master;
if not Exists (select 1 from sysdatabases where name =N'Lit_Base' )  
    create database [Lit_Base] 
              ON PRIMARY ( NAME = N'Lit_Base_Data',FILENAME = N'F:\\del/Lit_Base_Data.MDF' ,FILEGROWTH = 10%)
              LOG ON ( NAME = N'Lit_Base_Log',FILENAME = N'F:\\del/Lit_Base_Log.LDF',FILEGROWTH = 10%) ;
 


--若数据库脱机，则设置数据库联机
if Exists(select 1 from sysdatabases where  name =N'Lit_Base' and (status & 0x200) != 0)
     ALTER DATABASE [Lit_Base] SET ONLINE; 

             */
        }
        #endregion


        #region DropDataBase 删除数据库       

        /// <summary>
        /// 删除数据库
        /// </summary>
        public override void DropDataBase()
        {
            Exec((conn) => { return conn.Execute("drop DataBase [" + dbName + "]", commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout); });
        }

        #endregion



        #region Attach 附加数据库

        /// <summary>
        /// 附加数据库
        /// </summary>
        public void Attach()
        {
            string mdfPath = Path.Combine(MdfFileDirectory, dbName + "_Data.MDF");
            try
            {
                GetPrimaryFileInfo(mdfPath);
            }
            catch (Exception e)
            {
                try
                {
                    mdfPath = Path.Combine(MdfFileDirectory, dbName + ".MDF");
                    GetPrimaryFileInfo(mdfPath);
                }
                catch
                {
                    throw e;
                }
            }
            Attach(dbName, mdfPath, Path.Combine(MdfFileDirectory,dbName + "_Log.LDF") );
        }

        /// <summary>
        /// 附加数据库
        /// </summary>
        /// <param name="MdfPath">数据文件路径</param>
        /// <param name="LogPath">日志文件路径</param>
        public void Attach(string MdfPath, string LogPath)
        {
            Attach(dbName, MdfPath, LogPath);
        }

        /// <summary>
        /// 附加数据库
        /// 注：会清空所有数据库参数
        /// </summary>
        /// <param name="DBName">数据库名</param>
        /// <param name="MdfPath">数据文件路径</param>
        /// <param name="LogPath">日志文件路径</param>
        public void Attach(string DBName, string MdfPath, string LogPath)
        {
            Exec((conn) =>
            {               
                return conn.Execute("EXEC sp_attach_db @DBName, @MdfPath,@LogPath;",new { DBName, MdfPath, LogPath }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
            });
        }

        #endregion

        #region Detach 分离数据库
        /// <summary>
        /// 分离数据库
        /// </summary>
        public void Detach(string dbName = null)
        {
            Exec((conn) =>
            {     
                return conn.Execute("EXEC sp_detach_db @dbName", new { dbName = dbName ?? this.dbName }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
            });
        }

        #endregion

        


        #region 获取数据库当前连接数 

        /// <summary>
        /// 获取数据库当前连接数
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public int GetProcessCount(string dbName=null)
        {
            return Exec((conn) =>
            {            
                return (int)conn.ExecuteScalar("select count(*) from master..sysprocesses where dbid=db_id(@dbName);", new { dbName = dbName??this.dbName }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
            });
        }

        #endregion


        #region 强制关闭数据库的所有连接进程     

        /// <summary>
        /// 强制关闭数据库的所有连接进程
        /// </summary>
        public void KillProcess(string dbName=null)
        {
            Exec((conn) =>
            {              
                return conn.Execute("declare @programName nvarchar(200), @spid nvarchar(20) declare cDblogin cursor for select cast(spid as varchar(20)) AS spid from master..sysprocesses where dbid=db_id(@dbName) open cDblogin fetch next from cDblogin into @spid while @@fetch_status=0 begin  IF @spid <> @@SPID exec( 'kill '+@spid) fetch next from cDblogin into @spid end close cDblogin deallocate cDblogin ",
                    new { dbName = dbName?? this.dbName }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
            });

            /*
USE master  
 
declare @dbname nvarchar(260);
set @dbname='Lit_ZB';
 
      
declare   @programName     nvarchar(200), @spid   nvarchar(20)      
declare   cDblogin   cursor   for    
select   cast(spid   as   varchar(20))  AS spid   from   master..sysprocesses   where   dbid=db_id(@dbname)    
open   cDblogin   
fetch   next   from   cDblogin   into   @spid    
while   @@fetch_status=0    
begin       
--防止自己终止自己的进程     
--否则会报错不能用KILL 来终止您自己的进程。      
IF  @spid <> @@SPID   
    exec( 'kill   '+@spid)    
fetch   next   from  cDblogin   into   @spid    
end        
close   cDblogin    
deallocate   cDblogin   

             */
        }

        #endregion





        #region BackupToLocalBak

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="bakFilePath">备份的文件路径，若不指定则自动构建。demo:@"F:\\website\appdata\dbname_2020-02-02_121212.bak"</param>
        /// <returns>备份的文件路径</returns>
        public string BackupToLocalBak(string bakFilePath = null)
        {
            if (string.IsNullOrEmpty(bakFilePath)) bakFilePath = BackupFile_GetPathByName(GenerateBackupFileName(dbName));

            return Exec((conn) =>
            {
                conn.Execute("backup database @database to disk = @filePath ", new { database = this.dbName, filePath = bakFilePath }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);
                return bakFilePath;
            });
        }

        #endregion



        #region BackupToBak 远程备份数据库
        /// <summary>
        /// 远程备份数据库（当数据库服务器和代码运行服务器不为同一个时，可以远程备份数据库）
        /// </summary>
        /// <param name="bakFilePath">本地备份文件的路径，若不指定则自动构建。demo:@"F:\\website\appdata\dbname_20102020_121212.bak"</param>
        /// <returns>本地备份文件的路径</returns>
        public string BackupToBak(string bakFilePath = null)
        {
            /*
             远程备份的步骤： 

                (x.1)远程数据库服务器-获取当前数据库mdf文件所在文件夹
                (x.2)远程数据库服务器-备份当前数据库到mdf所在文件夹中
                (x.3)远程数据库服务器-通过sql语句读取备份文件内容，并删除备份文件

                (x.4)本地服务器-读取远程的备份文件内容到本地指定文件                
             
             */

            #region (x.1)拼接远程数据库服务器本地文件路径
            var remote_mdfDirectory=   Path.GetDirectoryName(GetMdfPath());
            var remote_bakFilePath = Path.Combine(remote_mdfDirectory,"sqler_temp_"+dbName+".bak");
            #endregion


            #region (x.2)远程数据库服务器 备份数据库
            BackupToLocalBak(remote_bakFilePath);
            #endregion

            #region (x.3)从远程数据库服务器 获取备份文件二进制内容，存储备份文件到本地，并删除临时备份文件

            if (string.IsNullOrEmpty(bakFilePath)) 
                bakFilePath = BackupFile_GetPathByName(GenerateBackupFileName(dbName));

            try
            {
                conn.ReadFileFromDisk(remote_bakFilePath, bakFilePath);
            }
            finally
            {
                //远程删除文件
                conn.MsSql_DeleteFileFromDisk(remote_bakFilePath);
            }
            #endregion

            return bakFilePath;            
        }
        #endregion




        #region BackupSqler
        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="filePath">备份的文件路径，若不指定则自动构建。demo:@"F:\\website\appdata\dbname_2020-02-02_121212.zip"</param>
        /// <param name="useMemoryCache">是否使用内存进行全量缓存，默认:true。缓存到内存可以加快备份速度。在数据源特别庞大时请禁用此功能。</param>
        /// <returns>备份的文件路径</returns>
        public override string BackupSqler(string filePath = null, bool useMemoryCache = true)
        {
            if (string.IsNullOrEmpty(filePath)) filePath = Path.Combine(BackupPath, GenerateBackupFileName(dbName,".zip"));

            base.BackupSqler(filePath, useMemoryCache: useMemoryCache);
            return filePath;
        }

        #endregion


        #region GenerateBackupFileName
        static string GenerateBackupFileName(string dbName, string fileExtension = ".bak")
        {
            // dbname_2010-02-02_121212.bak
            return dbName + "_" + DateTime.Now.ToString("yyyy-MM-dd") + "_" + DateTime.Now.ToString("HHmmss") + fileExtension;
        }
        #endregion




        #region RestoreLocalBak
        /// <summary>
        /// 还原数据库   
        /// </summary>
        /// <param name="bakFilePath">数据库备份文件的路径</param>
        /// <returns>备份文件的路径</returns>
        public string RestoreLocalBak(string bakFilePath)
        {
            //若数据库不存在，则创建数据库
            CreateDataBase();

            return Exec((conn) =>
            {

                //获取 数据名 和 日志名          

                string strDataName = null, strLogName = null;
                //也可用 GetBakInfo 函数
                foreach (DataRow dr in conn.ExecuteDataTable("restore filelistonly from disk=@backupFilePath;", new { backupFilePath = bakFilePath }).Rows)
                {
                    if ("D" == dr["Type"].ToString().Trim())
                    {
                        if (string.IsNullOrEmpty(strDataName))
                        {
                            strDataName = dr["LogicalName"].ToString().Trim();
                        }
                    }
                    else if ("L" == dr["Type"].ToString().Trim())
                    {
                        if (string.IsNullOrEmpty(strLogName))
                        {
                            strLogName = dr["LogicalName"].ToString().Trim();
                        }
                    }
                }
                if (string.IsNullOrEmpty(strDataName) || string.IsNullOrEmpty(strLogName))
                {
                    throw new Exception("备份文件出错。");
                }


                conn.Execute(new StringBuilder("declare @DataPath nvarchar(260),@LogPath nvarchar(260); use [").Append(this.dbName).Append("]; set @DataPath= (SELECT top 1 RTRIM(o.filename) FROM dbo.sysfiles o WHERE o.groupid = (SELECT u.groupid FROM dbo.sysfilegroups u WHERE u.groupname = N'PRIMARY') and (o.status & 0x40) = 0 );  set @LogPath= (SELECT top 1 RTRIM(filename) FROM sysfiles WHERE (status & 0x40) <> 0); use [master]; ALTER DATABASE [").Append(this.dbName).Append("] SET OFFLINE WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [").Append(this.dbName).Append("] FROM  DISK =@BakPath  WITH  FILE = 1,  RECOVERY ,  REPLACE ,  MOVE @LogName TO @LogPath,  MOVE @DataName TO @DataPath;").ToString()
                    , new { DataName = strDataName, LogName = strLogName, BakPath = bakFilePath }, commandTimeout: Orm.Dapper.DapperConfig.CommandTimeout);

                return bakFilePath;
            });
            /*
--传递的参数
declare  @BakPath nvarchar(260),
@DataName nvarchar(128),
@LogName nvarchar(128)


--临时参数
declare @DataPath nvarchar(260),
@LogPath nvarchar(260),


--获取数据库物理文件路径
use [Lit_Base1]
set @DataPath= (SELECT top 1 RTRIM(o.filename) FROM dbo.sysfiles o WHERE o.groupid = (SELECT u.groupid FROM dbo.sysfilegroups u WHERE u.groupname = N'PRIMARY') and (o.status & 0x40) = 0 )
set @LogPath= (SELECT top 1 RTRIM(filename) FROM sysfiles WHERE (status & 0x40) <> 0)

--还原数据库
use [master]
--立即中断不合格的连接。所有未完成的事务都将回滚。
ALTER DATABASE [Lit_Base1] SET OFFLINE WITH ROLLBACK IMMEDIATE

RESTORE DATABASE [Lit_Base1] FROM  DISK =@BakPath  WITH  FILE = 1,  RECOVERY ,  REPLACE ,  MOVE @LogName TO @LogPath,  MOVE @DataName TO @DataPath

             */
        }
        #endregion




        #region RestoreBak 通过bak远程还原
        /// <summary>
        /// 远程还原数据库   
        /// </summary>
        /// <param name="bakFilePath">数据库备份文件的路径</param>
        /// <param name="sliceMb">文件切片大小。默认：100,单位 MB。若指定小于备份文件大小的正数，则在传递文件到远程时进行分片传递。建议在备份文件过大时使用。</param>
        /// <returns>备份文件的路径</returns>
        public void RestoreBak(string bakFilePath, int sliceMb = 100)
        {
            //(x.1)若数据库不存在，则创建数据库
            CreateDataBase();

            //(x.2)拼接在mdf同文件夹下的备份文件的路径
            var remote_mdfDirectory = Path.GetDirectoryName(GetMdfPath());
            var remote_bakFilePath = Path.Combine(remote_mdfDirectory, "sqler_temp_" + dbName + ".bak");         

            #region    
            try
            {
                #region (x.3)把本地备份文件传递到远程
                var sliceByte = sliceMb * 1024 * 1024;
                if (sliceByte <= 0 || new FileInfo(bakFilePath).Length <= sliceByte)
                {
                    //不分片，直接传递全部文件内容
                    conn.MsSql_WriteFileToDisk(remote_bakFilePath, File.ReadAllBytes(bakFilePath));
                }
                else
                {
                    //分片传递文件
                    using (var file = new FileStream(bakFilePath, FileMode.Open, FileAccess.Read))
                    using (var bin = new BinaryReader(file))
                    {
                        conn.MsSql_WriteFileToDisk(remote_bakFilePath, bin, sliceByte: sliceByte);
                    }
                }
                #endregion


                //(x.4)还原远程数据库   
                RestoreLocalBak(remote_bakFilePath);

            }
            finally
            {
                //远程删除文件
                conn.MsSql_DeleteFileFromDisk(remote_bakFilePath);
            }
            #endregion 

        }
        #endregion









        #region Restore

        /// <summary>
        /// 远程还原数据库   
        /// </summary>
        /// <param name="filePath">数据库备份文件的路径。可为bak文件或zip文件</param>
        /// <param name="sliceMb">文件切片大小。默认：100,单位 MB。若指定小于备份文件大小的正数，则在传递文件到远程时进行分片传递。建议在备份文件过大时使用。</param>
        /// <returns>备份文件的路径</returns>
        public virtual void Restore(string filePath, int sliceMb = 100)
        {
            var fileExtension = Path.GetExtension(filePath).ToLower();

            switch (fileExtension) 
            {
                case ".zip": RestoreSqler(filePath);return;
                case ".bak": RestoreBak(filePath, sliceMb: sliceMb); return;
            }

            throw new NotImplementedException($"不识别的备份文件类型。NotImplementedException from {nameof(Restore)} in {nameof(MsSqlDbMng)}.cs");
        }
        #endregion



        protected override Regex RestoreSqler_SqlSplit 
        {
            get 
            {
                //  GO ，包括空格、制表符、换页符等            
                return new Regex("\\sGO\\s");
            }
        }





        #region  获取备份文件的信息
        /// <summary>
        ///  获取备份文件的信息
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public DataTable BakFile_GetDataInfo(string filePath)
        {
            return Exec((conn) =>
            {
                return conn.ExecuteDataTable("restore filelistonly from disk=@filePath;", new { filePath = filePath });
            });

        }
        #endregion




    }
}
