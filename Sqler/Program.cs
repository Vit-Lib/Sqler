using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using Vit.ConsoleUtil;
using Vit.Core.Module.Log;
using Vit.Extensions;

namespace App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var arg = new System.Collections.Generic.List<string>() {"SqlVersion.CurrentVersion" };
            //arg.AddRange(new[] { "--DataPath", @"W:\code\Data" });
            //args = arg.ToArray();

            var runAsCmd = (args != null && args.Length >= 1 && false == args[0]?.StartsWith("-"));

            if (!runAsCmd)
            {
                Logger.OnLog = (level, msg) => { Console.WriteLine((level == Level.INFO ? "" : "[" + level + "]") + msg); };
            }


            //(x.1) 初始化Sqler
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

         

            if (runAsCmd)
            {
                Logger.OnLog = (level, msg) => { Console.WriteLine((level == Level.INFO ? "" : "[" + level + "]") + msg); };

                Vit.ConsoleUtil.ConsoleHelp.Log = (msg) => { Logger.Info(msg);  };
                Vit.ConsoleUtil.ConsoleHelp.Exec(args);
                return;
            }



            //(x.3)启动http服务
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
            //.UseSerslot()
            .UseUrls(Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<string[]>("server.urls"))
            .UseStartup<Startup>()
            .UseVitConfig()
            ;
    }
}
