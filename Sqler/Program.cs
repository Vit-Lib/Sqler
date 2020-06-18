using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;
using Vit.ConsoleUtil;
using Vit.Core.Module.Log;
using Vit.Extensions;

namespace Sqler
{
    public class Program
    {
        public static void Main(string[] args)
        {

            //(x.1) 初始化Sqler
            try
            {
                Logger.Info("[Sqler] version: "+ System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).FileVersion );

                string dataDirectoryPath = ConsoleHelp.GetArg(args, "--DataPath");
                Sqler.Module.Sqler.Logical.SqlerHelp.InitSqlDataPath(dataDirectoryPath);
              
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
                return;
            }



            //var arg = new System.Collections.Generic.List<string>() { "ImportTilesetFile","--entityFromFileName" };
            //arg.AddRange(new[] { "--filePath", @"W:\code\1910赛扬\模型\2020-04-03\处理\宁波阪急（电气）1F_1_3_1.zip" });           
            //args = arg.ToArray();

            if (args != null && args.Length >= 1   &&  false==args[0]?.StartsWith("-") )
            {
                //Logger.OnLog = (level, msg) => { Console.Write("[" + level + "]" + msg); };
                Vit.ConsoleUtil.ConsoleHelp.Log = (msg) => { Console.WriteLine(msg); Logger.Info(msg);  };
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
