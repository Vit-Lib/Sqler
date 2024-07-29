using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Vit.ConsoleUtil;
using Vit.Core.Module.Log;
using Vit.Core.Module.Serialization;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Core.Util.ConfigurationManager;
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

            //args = new string[] { "SqlRun.Exec"
            //    ,"--quiet"
            //    ,"--sql","SHOW DATABASES WHERE `Database` NOT IN ('information_schema','mysql', 'performance_schema', 'sys');"
            //    ,"--format","Values"
            //    ,"--set","SqlRun.Config.type=mysql"
            //    ,"--set","SqlRun.Config.ConnectionString=Data Source=lanxing.cloud;Port=11052;User Id=root;Password=123456;CharSet=utf8;allowPublicKeyRetrieval=true;"
            //};       

            args ??= Array.Empty<string>();

            #region (x.2) --quiet
            Logger.PrintToTxt = false;
            Logger.PrintToConsole = false;

            if (args.Any(arg => arg == "--quiet") == true)
            {

            }
            else
            {
                //Logger.OnLog = (level, msg) => { Console.WriteLine((level == Level.INFO ? "" : "[" + level + "]") + msg); };
                Logger.log.AddCollector(new Vit.Core.Module.Log.LogCollector.Collector
                {
                    OnLog = (msg) =>
                    {
                        Console.WriteLine((msg.level == Level.info ? "" : "[" + msg.level + "]") + msg.message);
                    }
                });
            }
            #endregion



            // #3 init Sqler
            try
            {
                Logger.Info("[Sqler] version: " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).FileVersion);

                string dataDirectoryPath = ConsoleHelp.GetArg(args, "--DataPath");
                App.Module.Sqler.Logical.SqlerHelp.InitEnvironment(dataDirectoryPath, args);
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
                return;
            }






            // #4
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



            // #5 start http service
            try
            {
                var builder = WebApplication.CreateBuilder(args);


                #region ##1 config WebHost
                {
                    builder.WebHost
                        .AllowAnyOrigin()
                        .UseUrls(Appsettings.json.GetByPath<string[]>("server.urls"))
                        .UseVitConfig()
                        ;
                }
                #endregion

                #region ##2 Add services to the container.
                {
                    builder.Services.AddControllers(options =>
                    {
                        //use custom exception filter
                        options.Filters.Add<ExceptionFilter>();

                        //options.EnableEndpointRouting = false;
                    }).AddJsonOptions(options =>
                    {
                        //Json Serialize config

                        options.JsonSerializerOptions.AddConverter_Newtonsoft();
                        options.JsonSerializerOptions.AddConverter_DateTime();


                        options.JsonSerializerOptions.IncludeFields = true;

                        // JsonNamingPolicy.CamelCase makes the first letter lowercase (default), null leaves case unchanged
                        options.JsonSerializerOptions.PropertyNamingPolicy = null;

                        // set the JSON encoder to allow all Unicode characters, preventing the default behavior of encoding non-ASCII characters.
                        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

                        // Ignore null values
                        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

                        // extra comma at the end of a list of JSON values in an object or array is allowed (and ignored) within the JSON payload being deserialized.
                        options.JsonSerializerOptions.AllowTrailingCommas = true;

                    });

                }
                #endregion


                var app = builder.Build();

                #region ##3 Configure
                {
                    // static file (wwwroot)
                    foreach (var config in Appsettings.json.GetByPath<Vit.WebHost.StaticFilesConfig[]>("server.staticFiles"))
                    {
                        app.UseStaticFiles(config);
                    }

                    #region api for appVersion
                    app.Map("/version", appBuilder =>
                    {
                        appBuilder.Run(async context =>
                        {
                            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).FileVersion;
                            await context.Response.WriteAsync(version);
                        });
                    });
                    #endregion

                    //SqlerHelp
                    Task.Run(App.Module.Sqler.Logical.SqlerHelp.InitAutoTemp);
                }
                #endregion

                //app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex);
            }
        }


        public class ExceptionFilter : Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter
        {
            public void OnException(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext context)
            {
                if (context.ExceptionHandled == false)
                {
                    Logger.Error(context.Exception);
                    SsError error = (SsError)context.Exception;
                    ApiReturn apiRet = error;


                    context.Result = new ContentResult
                    {
                        Content = Json.Serialize(apiRet),
                        StatusCode = StatusCodes.Status200OK,
                        ContentType = "application/json; charset=utf-8"
                    };

                    //context.HttpContext.Response.Headers.Add("responseState", "fail");
                    //context.HttpContext.Response.Headers.Add("responseError_Base64", error?.SerializeToBytes()?.BytesToBase64String());
                }
                context.ExceptionHandled = true;
            }

            public Task OnExceptionAsync(ExceptionContext context)
            {
                OnException(context);
                return Task.CompletedTask;
            }

        }

    }
}
