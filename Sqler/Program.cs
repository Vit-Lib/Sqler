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

            //args = new string[] { "SqlRun.Exec"
            //    ,"--quiet"
            //    ,"--sql","SHOW DATABASES WHERE `Database` NOT IN ('information_schema','mysql', 'performance_schema', 'sys');"
            //    ,"--format","Values"
            //    ,"--set","SqlRun.Config.type=mysql"
            //    ,"--set","SqlRun.Config.ConnectionString=Data Source=lanxing.cloud;Port=11052;User Id=root;Password=123456;CharSet=utf8;allowPublicKeyRetrieval=true;"
            //};       

            if (args == null) args = new string[] { };

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
                App.Module.Sqler.Logical.SqlerHelp.InitEnvironment(dataDirectoryPath,args);              
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
                return;
            }






            //(x.4)
            var runAsCmd = (args.Length >= 1 && false == args[0]?.StartsWith("-"));
            if (runAsCmd)
            {
                #region --quiet
                if (args.Any(arg => arg == "--quiet") == true)
                {
                    Vit.ConsoleUtil.ConsoleHelp.Out = (msg) => { Logger.Info(msg); Console.WriteLine(msg); };
                }
                else
                {
                    Vit.ConsoleUtil.ConsoleHelp.Out = (msg) => { Logger.Info(msg); };
                }
                #endregion

                Vit.ConsoleUtil.ConsoleHelp.Log = (msg) => { Logger.Info(msg); };
                Vit.ConsoleUtil.ConsoleHelp.Exec(args);
                return;
            }



            //(x.5)启动http服务
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
