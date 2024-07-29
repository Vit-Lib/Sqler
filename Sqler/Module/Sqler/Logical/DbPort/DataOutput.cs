
using System.Data;

using Sqler.Module.Sqler.Logical.Message;

using Vit.Core.Module.Log;
using Vit.Db.Util.Data;
using Vit.Extensions;
using Vit.Extensions.Db_Extensions;
using Vit.Extensions.Serialize_Extensions;

namespace Sqler.Module.Sqler.Logical.DbPort
{

    public class DataOutput
    {
        public Action<EMsgType, string> SendMsg;

        /// <summary>
        /// mssql、mysql、sqlite
        /// </summary>
        public string type;
        public string connectionString { get; set; } //数据库连接字符串。亦可从配置文件获取，如 sqler.config:SqlBackup.SqlServerBackup.ConnectionString
        public bool createTable;
        public bool delete;
        public bool truncate;

        public List<TableInfo> tableInfos;
        public int sourceSumRowCount = 0;

        public Func<string, int, DataTableReader> GetDataTableReader;


        public int importedSumRowCount { get; private set; } = 0;
        int sourceRowCount = 0;



        #region WriteProcess
        void WriteProcess(int importedRowCount)
        {
            SendMsg(EMsgType.Nomal, "");

            if (sourceRowCount > 0)
            {
                var process = (((float)importedRowCount) / sourceRowCount * 100).ToString("f2");
                SendMsg(EMsgType.Nomal, $"           cur: [{process}%] {importedRowCount}/{sourceRowCount}");
            }
            else
            {
                SendMsg(EMsgType.Nomal, $"           cur: {importedRowCount}");
            }

            if (sourceSumRowCount > 0)
            {
                var process = (((float)importedSumRowCount) / sourceSumRowCount * 100).ToString("f2");
                SendMsg(EMsgType.Nomal, $"           sum: [{process}%] {importedSumRowCount}/{sourceSumRowCount}");
            }
            else
            {
                SendMsg(EMsgType.Nomal, $"           sum: {importedSumRowCount}");
            }

        }
        #endregion






        #region Output

        public void Output()
        {
            using var conn = ConnectionFactory.GetConnection(new Vit.Db.Util.Data.ConnectionInfo { type = type, connectionString = connectionString });
            BatchImport(conn);
        }


        public void BatchImport(IDbConnection conn)
        {
            SendMsg(EMsgType.Title, "   Output to database " + conn.Database);
            SendMsg(EMsgType.Title, "   sum row count: " + sourceSumRowCount);
            SendMsg(EMsgType.Title, "   table count: " + tableInfos.Count);
            SendMsg(EMsgType.Title, "   table name: " + tableInfos.Select(m => m.tableName).Serialize());


            foreach (var tableInfo in tableInfos)
            {
                var tableName = tableInfo.tableName;

                sourceRowCount = tableInfo.rowCount;

                var curTbIndex = tableInfo.tableIndex;

                using var tableReader = GetDataTableReader(tableName, curTbIndex);
                int importedRowCount = 0;

                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, "");
                SendMsg(EMsgType.Title, $"       [{(curTbIndex + 1)}/{tableInfos.Count}]start import table " + tableName + ",sourceRowCount:" + sourceRowCount);


                #region (x.1)数据源为DataReader
                if (tableReader.GetDataReader != null)
                {

                    //(x.x.1)read data
                    SendMsg(EMsgType.Nomal, " ");
                    SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                    var dr = tableReader.GetDataReader();


                    //(x.x.2)
                    if (createTable)
                    {
                        SendMsg(EMsgType.Title, "           [x.x.2]create table ");
                        try
                        {
                            conn.CreateTable(dr, tableName);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }

                    //(x.x.3)
                    if (delete)
                    {
                        SendMsg(EMsgType.Title, "           [x.x.3]delete table ");
                        try
                        {
                            conn.Execute("delete from  " + conn.Quote(tableName));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }

                    //(x.x.4)
                    if (truncate)
                    {
                        SendMsg(EMsgType.Title, "           [x.x.4]truncate table ");
                        try
                        {
                            conn.Execute("truncate table " + conn.Quote(tableName));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }


                    //(x.x.5)import data   
                    SendMsg(EMsgType.Nomal, "           [x.x.5]write data");
                    try
                    {

                        conn.BulkImport(dr, tableName
                            , batchRowCount: DbPortLogical.batchRowCount
                            , onProcess: (rowCount, sumRowCount) =>
                            {
                                importedRowCount += rowCount;
                                importedSumRowCount += rowCount;

                                WriteProcess(importedRowCount);
                            }
                            , commandTimeout: DbPortLogical.commandTimeout);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                    }

                }
                #endregion


                #region (x.2)数据源为DataTable
                if (tableReader.GetDataTable != null)
                {
                    //(x.x.1)read data
                    SendMsg(EMsgType.Nomal, " ");
                    SendMsg(EMsgType.Nomal, "           [x.x.1]read data ");
                    var dt = tableReader.GetDataTable();
                    dt.TableName = tableName;

                    //(x.x.2)
                    if (createTable)
                    {
                        SendMsg(EMsgType.Title, "           [x.x.2]create table ");
                        try
                        {
                            conn.CreateTable(dt);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }

                    //(x.x.3)
                    if (delete)
                    {
                        SendMsg(EMsgType.Title, "           [x.x.3]delete table ");
                        try
                        {
                            conn.Execute("delete from  " + conn.Quote(tableName));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }

                    //(x.x.4)
                    if (truncate)
                    {
                        SendMsg(EMsgType.Title, "           [x.x.4]truncate table ");
                        try
                        {
                            conn.Execute("truncate table " + conn.Quote(tableName));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                        }
                    }


                    //(x.x.5)import data   
                    SendMsg(EMsgType.Nomal, "           [x.x.5]write data,row count:" + dt.Rows.Count);
                    try
                    {
                        while (true)
                        {


                            dt.TableName = tableName;
                            conn.BulkImport(dt);

                            int rowCount = dt.Rows.Count;
                            importedRowCount += dt.Rows.Count;
                            importedSumRowCount += dt.Rows.Count;


                            WriteProcess(importedRowCount);


                            SendMsg(EMsgType.Nomal, " ");
                            SendMsg(EMsgType.Nomal, "           [x.x.x.1]read data ");
                            dt = tableReader.GetDataTable();
                            if (dt == null)
                            {
                                break;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        SendMsg(EMsgType.Err, "出错。" + ex.GetBaseException().Message);
                    }

                }
                #endregion


                SendMsg(EMsgType.Title, "                    import table " + tableName + " success,row count:" + importedRowCount);
            }
        }
        #endregion





    }
}
