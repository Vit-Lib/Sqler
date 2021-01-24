using Dapper;
using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Vit.Core.Module.Log;
using Vit.Extensions;
using Vit.Orm.Dapper;

namespace Vit.Db.DbMng.SqlerFile
{
    class SqlerBackuper
    {

        public int importedRowCount = 0;
        public int importedTableCount = 0;


        public string dirPath;
        public IDbConnection conn;
        public Action<string> Log;
        public Func<IDataReader, string, int> BulkImport;

        public Regex sqlSplit = null;

        public BackupInfo backupInfo;

        public void ReadBackupInfo() 
        {
            // info.json
            var filePath = Path.Combine(dirPath, "info.json");

            backupInfo = null;

            if (File.Exists(filePath))
                backupInfo = File.ReadAllText(filePath, System.Text.Encoding.UTF8)?.Deserialize<BackupInfo>();

            if (backupInfo == null)
            {
                backupInfo = new BackupInfo
                {
                    cmd = BackupInfo.defaultCmd
                };
            }

            if (!string.IsNullOrEmpty(backupInfo.sqlSplit))
            {
                sqlSplit = new Regex(backupInfo.sqlSplit);
            }

            Log("     数据库类型：" + backupInfo.type);
            Log("     数据库版本：" + backupInfo.version);
            Log("     备份时间  ：" + backupInfo.backupTime);
            Log("     sqlSplit  ：" + sqlSplit?.ToString());            
        }


        public void ExecCmd(string[]cmd,int commandTimeout) 
        {
            switch (cmd[0]) 
            {
                case "execSqlFile":
                    #region execSqlFile
                    {
                        Log("    执行脚本文件 " + cmd[1]);

                        var filePath = Path.Combine(dirPath, cmd[1]);                                 

                        var sqlText = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                        
                        //确保conn打开
                        conn.MakeSureOpen(() =>
                        {
                            conn.RunInTransaction((tran)=> {

                                if (sqlSplit == null)
                                {
                                    conn.Execute(sqlText, transaction: tran,commandTimeout: commandTimeout);
                                }
                                else
                                {
                                    var sqls = sqlSplit.Split(sqlText);
                                    foreach (String sql in sqls)
                                    {
                                        if (!String.IsNullOrEmpty(sql.Trim()))
                                        {
                                            conn.Execute(sql, transaction: tran, commandTimeout: commandTimeout);
                                        }
                                    }
                                }

                            },printErrorToLog:true);

                           
                        });

                        Log("");
                        Log("    成功");
                    }
                    #endregion

                    break;
                case "importSqlite":
                    #region importSqlite
                    {
                        Log("    导入sqlite文件 " + cmd[1]);

                        var filePath = Path.Combine(dirPath, cmd[1]);            
                    

                        int sumRowCount = 0;
                        int sumTableCount;
 
                        using (var connSqlite = ConnectionFactory.Sqlite_GetOpenConnectionByFilePath(filePath, Version: 3))
                        {
                            var tableNames = connSqlite.Sqlite_GetAllTableName();
                            sumTableCount = tableNames.Count;
                            int tbIndex = 0;

                            foreach (var tableName in tableNames)
                            {
                                tbIndex++;

                                Log("");
                                Log($" ----[{tbIndex}/{sumTableCount}]import table " + tableName);

                                int rowCount=0;
                                if (0<connSqlite.ExecuteScalar<int>("select count(*) from " + connSqlite.Quote(tableName), commandTimeout: commandTimeout))
                                {
                                    using (var dr = connSqlite.ExecuteReader("select * from " + connSqlite.Quote(tableName), commandTimeout: commandTimeout))
                                    {
                                        rowCount = BulkImport(dr, tableName);
                                        sumRowCount += rowCount;
                                    }
                                }
                                Log($"     table imported. row count: " + rowCount + ",  sum: " + sumRowCount);

                                importedRowCount += rowCount;
                                importedTableCount++;
                            }
                        }
                        Log("");
                        Log("    成功,table count: " + sumTableCount + ",  row count: " + sumRowCount);
                    }
                    #endregion
                    break;
            }
        }


    }
}
