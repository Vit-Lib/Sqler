using App.Module.Sqler.Logical.SqlVersion;
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
            ConsoleHelp.Log("一键升级数据库...");


            Action<EMsgType, String> sendMsg = (type,msg) =>
            {
                ConsoleHelp.Log(msg); 
            };

            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys)
            {
                VersionManage.UpgradeToVersion(sqlCodeRes.moduleName, sendMsg);
            }             

            ConsoleHelp.Log("操作成功");
        }
        #endregion



    }
}
