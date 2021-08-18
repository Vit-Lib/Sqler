using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Vit.Core.Module.Log;
using Vit.Extensions;
using Dapper;
using System.Text;
using Vit.Core.Util.ConfigurationManager;
using App.Module.Sqler.Logical.SqlVersion.Entity;
using Sqler.Module.Sqler.Logical.Message;

namespace App.Module.Sqler.Logical.SqlVersion
{   
    public class VersionManage
    {

        static void EnsureTableCreate() 
        {
       
            using (var conn = SqlVersionHelp.CreateOpenedDbConnection()) 
            {
                var dbType = conn.GetDbType();

                var sql = new JsonFile(SqlerHelp.GetDataFilePath("sqler.SqlVersion.table.json")).GetStringByPath("initDb." + dbType);
                if (string.IsNullOrWhiteSpace(sql)) return;

                conn.Execute(sql);
            }
        }



        public static int GetDbCurVersion(string module) 
        {
            try
            {
                using (var scope = SqlVersionHelp.efDbFactory.CreateDbContext(out var db))
                {                   
                    var dbSet = db.GetDbSet<sqler_version>();

                    var versionResult = (from v in dbSet
                                       where v.module == module && v.success==1
                                       orderby v.version descending
                                       select v).FirstOrDefault();
                    return versionResult?.version??0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return 0;
        }



 

        static void ExecVersion(string module,SqlCodeModel versionData, Action<EMsgType, String> sendMsg,DbSet<sqler_version> dbSet, DbContext dbContext)
        {
            #region (x.1)版本检验            
            //int? dbVersioin = GetDbCurVersion(module);
            //if (
            //    (dbVersioin == null && versionData.version.Value != 1)
            //    || (dbVersioin != null && versionData.version.Value != dbVersioin.Value + 1)
            //    ) 
            //{
            //    return new SsError { errorMessage= "无法跨版本执行。" };
            //}
            #endregion

            sendMsg(EMsgType.Title, "执行版本" + versionData.version + "升级语句。");
            sendMsg(EMsgType.Nomal, "comment:" + versionData.comment);

            #region (x.2)对象构建           
            sqler_version versionResult = new sqler_version();

            versionResult.module = module;
            versionResult.version = versionData.version;
            versionResult.code = versionData.code;
            versionResult.exec_time = DateTime.Now;
    
            versionResult.result = "";
            versionResult.remarks = "";
            #endregion


            StringBuilder execResult = new StringBuilder();

            #region (x.3)执行语句函数
            void ExecSql()
            {        
                using (var conn = SqlVersionHelp.CreateOpenedDbConnection())          
                {
                    conn.RunInTransaction((tran) => {

                        int index = 1;
                        //  /*GO*/GO 中间可出现多个空白字符，包括空格、制表符、换页符等          
                        //Regex reg = new Regex("/\\*GO\\*/\\s*GO");
                        Regex reg = new Regex("\\sGO\\s");
                        var sqls = reg.Split(versionResult.code);
                        foreach (String sql in sqls)
                        {
                            if (String.IsNullOrEmpty(sql.Trim()))
                            {
                                sendMsg(EMsgType.Title, $"[{(index++)}/{sqls.Length}]空语句，无需执行.");
                            }
                            else
                            {
                                sendMsg(EMsgType.Title, $"[{(index++)}/{sqls.Length}]执行sql语句：");
                                sendMsg(EMsgType.Nomal, sql);
                                var result = "执行结果:" + conn.Execute(sql, null, tran) + " Lines effected.";
                                execResult.AppendLine(result);
                                sendMsg(EMsgType.Title, result);
                            }
                        }

                    },onException:Logger.Error);
                     
                }
            }
            #endregion


            #region (x.4)执行并入库结果

            try
            {
                versionResult.success = 0;
                ExecSql();
                versionResult.success = 1;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                execResult.AppendLine(ex.GetBaseException().Message);
                throw;
            }
            finally
            {
                try
                {
                    versionResult.result = execResult.ToString();
                    dbSet.Add(versionResult);
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }               
            }
           
            #endregion



            sendMsg(EMsgType.Title, "版本" + versionData.version + " 升级成功。");
            sendMsg(EMsgType.Nomal, "");
            sendMsg(EMsgType.Nomal, "");

        }




        #region 升級
        public static void UpgradeToVersion(string module, Action<EMsgType,String> sendMsg, int descVersion = -1)
        { 
            var repository = SqlVersionHelp.sqlCodeRepositorys.AsQueryable().FirstOrDefault(m => m.moduleName == module);

            sendMsg = 
                ((EMsgType type, String msg) =>
            {
                Logger.log.Log(Level.ApiTrace, msg);
            })
            + sendMsg;

            int curVersion = GetDbCurVersion(module);
            int lastVersion = repository.lastVersion;
            
            if (descVersion < 0) 
            {
                descVersion = lastVersion;
            }

            try
            {            

                if (lastVersion < descVersion) descVersion = lastVersion;


                sendMsg(EMsgType.Title, "升级模块：   " + module);
                sendMsg(EMsgType.Title, "当前版本：   " + curVersion);
                sendMsg(EMsgType.Title, "目标版本：   " + descVersion);
                sendMsg(EMsgType.Title, "最新版本：   " + lastVersion);
         


                if (descVersion < curVersion)
                {
                    sendMsg(EMsgType.Title, "无需执行，当前版本已经比目标版本高。");
                    return;
                }

                if (descVersion == curVersion)
                {
                    sendMsg(EMsgType.Title, "无需执行,当前版本已经为目标版本。");
                    return;
                } 

                sendMsg(EMsgType.Title, "执行数据库升级语句...");
                sendMsg(EMsgType.Nomal, "");
                sendMsg(EMsgType.Nomal, "");



                //确保表 sqler_version 存在
                EnsureTableCreate();


                using (var scope = SqlVersionHelp.efDbFactory.CreateDbContext(out var dbContext))
                {

                  
                    //dbContext.Database.EnsureCreated();
                    //dbContext.SaveChanges();


                    var dbSet = dbContext.GetDbSet<sqler_version>();

                    while (curVersion < descVersion)
                    {
                        var sqlCode = repository.GetModel("" + (curVersion + 1));
                        ExecVersion(module, sqlCode.data, sendMsg, dbSet, dbContext);
                        curVersion++;
                    }
                }

               

                sendMsg(EMsgType.Nomal, "");
                sendMsg(EMsgType.Nomal, "");
                sendMsg(EMsgType.Nomal, "");
                sendMsg(EMsgType.Title, "升级成功。");
                sendMsg(EMsgType.Title, "数据库当前版本：   " + curVersion);
                sendMsg(EMsgType.Title, "数据库目标版本：   " + descVersion);
            }
            catch (Exception e)
            {
                sendMsg(EMsgType.Err, "执行出错，原因：");
                sendMsg(EMsgType.Err, e.GetBaseException().Message);
                try
                {
                    curVersion = GetDbCurVersion(module);                  
                }
                catch { }

                sendMsg(EMsgType.Title, "数据库当前版本：   " + curVersion);
                sendMsg(EMsgType.Title, "数据库目标版本：   " + descVersion);

                Logger.Error("SqlRun执行sql语句出错.出错版本:" + (curVersion+1), e);
            }

            sendMsg(EMsgType.Nomal, "");
            sendMsg(EMsgType.Nomal, "");
            sendMsg(EMsgType.Nomal, "");
        }

        #endregion
       


 


    }
}
