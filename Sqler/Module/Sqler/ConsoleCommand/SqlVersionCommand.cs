using App.Module.Sqler.Logical.SqlVersion;
using Sqler.Module.Sqler.Logical.Message;
using System;
using Vit.ConsoleUtil;

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

            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys)
            {
                VersionManage.UpgradeToVersion(sqlCodeRes.moduleName, sendMsg);
            } 
        }
        #endregion


        #region CurrentVersion
        [Command("SqlVersion.CurrentVersion")]
        [Remarks("查看数据库版本。参数说明：")]
        [Remarks("--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认：\"Data\"")]
        [Remarks("示例： SqlVersion.CurrentVersion")]
        public static void CurrentVersion(string[] args)
        {          
            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys)
            {
                var moduleName = sqlCodeRes.moduleName;
                int curVersion = VersionManage.GetDbCurVersion(moduleName);
                int lastVersion = sqlCodeRes.lastVersion;

                ConsoleHelp.Log("模块： " + moduleName+ "\t当前版本： " + curVersion+ "\t最新版本： " + lastVersion);               
            }
            ConsoleHelp.Log("---------------");
        }
        #endregion

    }
}
