using System.Text;
using System.Text.RegularExpressions;

using App.Module.Sqler.Logical.SqlVersion.Entity;

using Sqler.Module.Sqler.Logical.Message;

using Vit.Core.Module.Log;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ConfigurationManager;
using Vit.Extensions;
using Vit.Extensions.Db_Extensions;

using Vitorm;

namespace App.Module.Sqler.Logical.SqlVersion
{
    public class VersionManage
    {
        static bool EnsureTableCreate()
        {
            using (var conn = SqlVersionHelp.CreateOpenedDbConnection())
            {
                var dbType = conn.GetDbType();

                var sql = new JsonFile(SqlerHelp.GetDataFilePath("sqler.SqlVersion.table.json")).GetStringByPath("initDb." + dbType);

                if (!string.IsNullOrWhiteSpace(sql))
                {
                    conn.Execute(sql);
                    return true;
                }
            }

            Data.TryCreateTable<sqler_version>();

            return true;
        }



        public static int GetDbCurVersion(string module)
        {
            try
            {
                var versionResult = (from v in Data.Query<sqler_version>()
                                     where v.module == module && v.success == 1
                                     orderby v.version descending
                                     select v)
                                   .FirstOrDefault();

                return versionResult?.version ?? 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return 0;
        }





        static void ExecVersion(string module, SqlCodeModel versionData, Action<EMsgType, String> sendMsg)
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
                using var conn = SqlVersionHelp.CreateOpenedDbConnection();
                conn.RunInTransaction((tran) =>
                {

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

                }, onException: Logger.Error);
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
                    Data.Add(versionResult);
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




        #region 升级

        /// <summary>
        /// 返回已经升级的版本数量
        /// </summary>
        /// <param name="module"></param>
        /// <param name="sendMsg"></param>
        /// <param name="descVersion"></param>
        /// <returns></returns>
        public static ApiReturn<int> UpgradeToVersion(string module, Action<EMsgType, String> sendMsg, int descVersion = -1)
        {

            ApiReturn<int> apiRet = new ApiReturn<int>(0);


            var repository = SqlVersionHelp.sqlCodeRepositorys.AsQueryable().FirstOrDefault(m => m.moduleName == module);

            sendMsg =
                ((EMsgType type, String msg) =>
            {
                Logger.log.Log(Level.ApiTrace, msg);
            })
            + sendMsg;

            int oriVersion = GetDbCurVersion(module);
            int curVersion = oriVersion;
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
                    return apiRet;
                }

                if (descVersion == curVersion)
                {
                    sendMsg(EMsgType.Title, "无需执行,当前版本已经为目标版本。");
                    return apiRet;
                }




                sendMsg(EMsgType.Title, "执行数据库升级语句...");
                sendMsg(EMsgType.Nomal, "");
                sendMsg(EMsgType.Nomal, "");



                // ensure table sqler_version exist
                EnsureTableCreate();


                while (curVersion < descVersion)
                {
                    var sqlCode = repository.GetModel("" + (curVersion + 1));
                    ExecVersion(module, sqlCode.data, sendMsg);
                    curVersion++;
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

                Logger.Error("SqlRun执行sql语句出错.出错版本:" + (curVersion + 1), e);

                apiRet.success = false;
                apiRet.error = e.GetBaseException();

            }

            sendMsg(EMsgType.Nomal, "");
            sendMsg(EMsgType.Nomal, "");
            sendMsg(EMsgType.Nomal, "");


            apiRet.data = curVersion - oriVersion;
            return apiRet;

        }

        #endregion






    }
}
