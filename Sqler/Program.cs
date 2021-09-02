using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using Vit.ConsoleUtil;
using Vit.Core.Module.Log;
using Vit.Extensions;
using System.Linq;
using App.Module.Sqler.Logical;

namespace App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var arg = new System.Collections.Generic.List<string>() {"SqlVersion.CurrentVersion" };
            //arg.AddRange(new[] { "--DataPath", @"W:\code\Data" });
            //args = arg.ToArray();
            if (args == null) args = new string[] { };

            args = new string[] { "SqlRun.Exec"
                ,"--quiet"
                ,"--sql","SHOW DATABASES WHERE `Database` NOT IN ('information_schema','mysql', 'performance_schema', 'sys');"
                ,"--format","Values"
                ,"--set","SqlRun.Config.type=mysql"
                ,"--set","SqlRun.Config.ConnectionString=Data Source=lanxing.cloud;Port=11052;User Id=root;Password=123456;CharSet=utf8;allowPublicKeyRetrieval=true;"
            };



            #region (x.2) --quiet
            if (args.Any(arg => arg == "--quiet") == true)
            {
                Logger.OnLog = (level, msg) => { };
            }
            else
            {
                Logger.OnLog = (level, msg) => { Console.WriteLine((level == Level.INFO ? "" : "[" + level + "]") + msg); };
                //Logger.OnLog = (level, msg) => { Console.WriteLine("[" + level.ToString().ToLower() + "]" + msg); };
            }
            #endregion





            //(x.3) 初始化Sqler
            try
            { 
                Logger.Info("[Sqler] version: "+ System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).FileVersion );

                string dataDirectoryPath = ConsoleHelp.GetArg(args, "--DataPath");
                App.Module.Sqler.Logical.SqlerHelp.InitEnvironment(dataDirectoryPath);              
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
                return;
            }




            #region (x.4)--set path=value
            {
                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i - 1] == "--set")
                    {
                        try
                        {
                            var str = args[i];
                            var ei = str?.IndexOf('=') ?? -1;
                            if (ei < 1) continue;

                            var path = str.Substring(0, ei);
                            var value = str.Substring(ei + 1);

                            SqlerHelp.sqlerConfig.root.ValueSetByPath(value, path.Split('.'));
                        }
                        catch { }
                    }
                }
            }
            #endregion


            //(x.5)
            var runAsCmd = (args.Length >= 1 && false == args[0]?.StartsWith("-"));
            if (runAsCmd)
            {
                Vit.ConsoleUtil.ConsoleHelp.Log = (msg) => { Logger.Info(msg);  };
                Vit.ConsoleUtil.ConsoleHelp.Exec(args);
                return;
            }



            //(x.6)启动http服务
            try
            {
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
            }


        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .AllowAnyOrigin()
            .UseUrls(Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<string[]>("server.urls"))
            .UseStartup<Startup>()
            .UseVitConfig()
            ;
    }
}
