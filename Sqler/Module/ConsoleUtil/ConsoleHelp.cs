#region << 版本注释-v2 >>
/*
 * ========================================================================
 * 版本：v2
 * 时间：2021-09-03
 * 作者：lith
 * 邮箱：serset@yeah.net
 * 说明： 
 * ========================================================================
*/
#endregion

using System;
using System.Linq;
using System.Reflection;

namespace Vit.ConsoleUtil
{
    #region ConsoleHelp
    public class ConsoleHelp
    {
        public static Action<string> Log = (msg) => { Console.WriteLine(msg); };
        public static Action<string> Out = (msg) => { Console.WriteLine(msg); };


        #region GetArg
        /// <summary>
        ///  null: 未指定参数
        ///  ""  : 指定了参数，但未指定值
        ///  其他: 指定了参数，其为参数的值
        /// </summary>
        /// <param name="args"> </param>
        /// <param name="argName">参数名 如 "-createTable"</param>
        /// <returns></returns>
        public static string GetArg(string[] args, string argName)
        {
            var index = Array.IndexOf(args, argName);
            if (index < 0) return null;

            if (index + 1 == args.Length)
            {
                return "";
            }

            var value = args[index + 1];
            if (value.StartsWith('-')) return "";
            return value;
        }
        #endregion



        #region Exec
        /// <summary> 
        /// 查找CommandAttribute特性的静态函数并按参数指定调用
        /// </summary>
        /// <param name="args"></param>
        public static void Exec(string[] args)
        {

            //var arg = new List<string>() { "un7z" };
            //arg.AddRange(new[] { "-i", "T:\\temp\\tileset.7z.001" });
            //arg.AddRange(new[] { "-o", "T:\\temp\\un7z" });
            //args = arg.ToArray();

            #region (x.1)通过反射获取所有命令            
            var cmdMap =
                //获取所有type
                Assembly.GetEntryAssembly().GetTypes()
                //获取所有静态函数
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                //获取指定CommandAttribute的函数
                .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
                //按照 命令名称 和 Method 构建Dictionary 
                .ToDictionary(
                  m => (m.GetCustomAttribute<CommandAttribute>().Value ?? m.Name)
                  , m => m
                );
            #endregion


            #region (x.2)若未指定命令名称，则输出帮助文档            
            if (args == null || args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                #region 输出命令帮助文档
                ConsoleHelp.Log("命令帮助文档：");
                foreach (var cmd in cmdMap)
                {
                    ConsoleHelp.Log("---------------");
                    ConsoleHelp.Log(cmd.Key);
                    cmd.Value.GetCustomAttributes<RemarksAttribute>()?.Select(m => m.Value).ToList().ForEach(ConsoleHelp.Log);
                }
                ConsoleHelp.Log("---------------");
                ConsoleHelp.Log("");
                ConsoleHelp.Log("");
                #endregion
                return;
            }
            #endregion


            #region (x.3)通过第一个参数查找命令并调用            
            try
            {
                cmdMap.TryGetValue(args[0], out var method);

                if (method == null)
                {
                    throw new Exception($"命令 { args[0] } 不存在！（help命令可查看命令说明）");                 
                }
                ConsoleHelp.Log("------------------------------");        
                ConsoleHelp.Log($"开始执行命令 { args[0] } ...");
                ConsoleHelp.Log("---------------");

                method.Invoke(null, new object[] { args });
            }
            catch (Exception ex)
            {
                ex = ex.GetBaseException();
                ConsoleHelp.Log("出错：" + ex.Message);
                ConsoleHelp.Log("出错：" + ex.StackTrace);

                exitCode = 1;             
            }
            #endregion

            ConsoleHelp.Log("结束！！");

            Exit();
            return;
        }




        #endregion


        #region Exit
        public static int exitCode = 0;
        public static void Exit() 
        {
            //退出当前进程以及当前进程开启的所有进程
            System.Environment.Exit(exitCode);
        }
        #endregion


        #region command help
        [Command("help")]
        [Remarks("命令说明：")]
        [Remarks("-c[--command] 要查询的命令。若不指定则返回所有命令的说明。如 help ")]
        [Remarks("示例： help -c help")]
        public static void Help(string[] args)
        {

            string cmdName = ConsoleHelp.GetArg(args, "-c") ?? ConsoleHelp.GetArg(args, "--command");

            #region (x.1)通过反射获取所有命令            
            var cmdMap =
                //获取所有type
                Assembly.GetEntryAssembly().GetTypes()
                //获取所有静态函数
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                //获取指定CommandAttribute的函数
                .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
                //按照 命令名称 和 Method 构建Dictionary 
                .ToDictionary(
                  m => (m.GetCustomAttribute<CommandAttribute>().Value ?? m.Name)
                  , m => m
                );
            #endregion


            #region (x.2)筛选指定命令
            if (!string.IsNullOrEmpty(cmdName))
            {
                cmdMap.TryGetValue(cmdName, out var cmdMethod);

                cmdMap = new System.Collections.Generic.Dictionary<string, MethodInfo>();
                if (cmdMethod != null)
                {
                    cmdMap[cmdName] = cmdMethod;
                }
            }
            #endregion

            #region (x.3)输出命令说明：
            ConsoleHelp.Log("命令说明：");
            foreach (var cmd in cmdMap)
            {
                ConsoleHelp.Log("---------------");
                ConsoleHelp.Log(cmd.Key);
                cmd.Value.GetCustomAttributes<RemarksAttribute>()?.Select(m => m.Value).ToList().ForEach(ConsoleHelp.Log);
            }
            ConsoleHelp.Log("---------------");
            ConsoleHelp.Log("");
            ConsoleHelp.Log("");
            #endregion

        }
        #endregion

    }
    #endregion
}
