using App.Module.Sqler.Logical.SqlVersion;
using Sqler.Module.Sqler.Logical.Message;
using System;
using Vit.ConsoleUtil;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Extensions;

namespace App.Module.Sqler.ConsoleCommand
{
    public class SqlVersionCommand
    {

        #region OneKeyUpgrade
        [Command("SqlVersion.OneKeyUpgrade")]
        [Remarks("一键升级数据库（所有模块）。参数说明：")]     
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlVersion.OneKeyUpgrade")]
        public static void OneKeyUpgrade(string[] args)
        {
            Action<EMsgType, String> sendMsg = (type,msg) =>
            {
                ConsoleHelp.Log(msg); 
            };


            ApiReturn<int> apiRet = new ApiReturn<int>(0);

            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys)
            {
                var curRet = VersionManage.UpgradeToVersion(sqlCodeRes.moduleName, sendMsg);

                apiRet.data += curRet.data;
                if (!curRet.success) 
                {
                    apiRet.error = curRet.error;
                    apiRet.success = false;
                }
            }
            ConsoleHelp.Log("一键升级数据库结果：");
            ConsoleHelp.Out(apiRet.Serialize());
        }
        #endregion


        #region CurrentVersion
        [Command("SqlVersion.CurrentVersion")]
        [Remarks("查看数据库版本。参数说明：")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlVersion.CurrentVersion")]
        public static void CurrentVersion(string[] args)
        {
            ConsoleHelp.Log("---------------");
            ConsoleHelp.Log("数据库版本信息： ");
            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys)
            {
                var moduleName = sqlCodeRes.moduleName;
                int curVersion = VersionManage.GetDbCurVersion(moduleName);
                int lastVersion = sqlCodeRes.lastVersion;

                ConsoleHelp.Out("模块： " + moduleName+ "\t当前版本： " + curVersion+ "\t最新版本： " + lastVersion);               
            }
            ConsoleHelp.Log("---------------");
        }
        #endregion





        #region NewVersionCount
        [Command("SqlVersion.NewVersionCount")]
        [Remarks("查看可升级版本的数量（\"0\"代表无需升级，否则返回可升级的数量）。参数说明：")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlVersion.NewVersionCount")]
        public static void NewVersionCount(string[] args)
        {
            int newVersionCount = 0;
            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys)
            {
                var moduleName = sqlCodeRes.moduleName;
                int curVersion = VersionManage.GetDbCurVersion(moduleName);
                int lastVersion = sqlCodeRes.lastVersion;
                if (lastVersion > curVersion)
                    newVersionCount += lastVersion - curVersion;
            }

            ConsoleHelp.Log("可升级版本的数量： ");
            ConsoleHelp.Out("" + newVersionCount);
        }
        #endregion

    }
}
