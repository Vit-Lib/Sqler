using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Vit.Core.Module.Log;
using Vit.Extensions;

namespace Sqler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Sqler.Module.Sqler.Logical.SqlerHelp.InitSqlDataPath(args);
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
            }
           
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseSerslot()
            .UseUrls(Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<string[]>("server.urls"))
            .UseStartup<Startup>()
            .UseVitConfig()
            ;
    }
}
