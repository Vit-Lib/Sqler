using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vit.Core.Module.Log;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Extensions;

namespace App
{

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                //使用自定义异常处理器
                options.Filters.Add<ExceptionFilter>();

#if NETCOREAPP3_0_OR_GREATER
                options.EnableEndpointRouting = false;
#endif
            })
            .AddJsonOptions(options =>
            {
                //Json序列化全局配置
#if NETCOREAPP3_0_OR_GREATER

                options.JsonSerializerOptions.AddConverter_Newtonsoft();
                options.JsonSerializerOptions.AddConverter_DateTime();


                options.JsonSerializerOptions.IncludeFields = true;

                //JsonNamingPolicy.CamelCase首字母小写（默认）,null则为不改变大小写
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                //取消Unicode编码 
                options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);               
                //忽略空值
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                //options.JsonSerializerOptions.IgnoreNullValues = true;
                //允许额外符号
                options.JsonSerializerOptions.AllowTrailingCommas = true;

#else

                //忽略循环引用
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

                //不更改元数据的key的大小写
                options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
#endif


            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            //配置静态文件
            foreach (var config in Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<Vit.WebHost.StaticFilesConfig[]>("server.staticFiles"))
            {
                app.UseStaticFiles(config);
            }


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
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



            //app.UseHttpsRedirection();
            app.UseMvc();


            //SqlerHelp
            Task.Run(App.Module.Sqler.Logical.SqlerHelp.InitAutoTemp);

        }
    }






    #region ExceptionFilter
    /// <summary>
    /// 
    /// </summary>
    public class ExceptionFilter : Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter
    {
        /// <summary>
        /// 发生异常时进入
        /// </summary>
        /// <param name="context"></param>
        public void OnException(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext context)
        {
            if (context.ExceptionHandled == false)
            {
                Logger.Error(context.Exception);
                SsError error = (SsError)context.Exception;
                ApiReturn apiRet = error;


                context.Result = new ContentResult
                {
                    Content = apiRet.Serialize(),//这里是把异常抛出。也可以不抛出。
                    StatusCode = StatusCodes.Status200OK,
                    ContentType = "application/json; charset=utf-8"
                };

                //context.HttpContext.Response.Headers.Add("responseState", "fail");
                //context.HttpContext.Response.Headers.Add("responseError_Base64", error?.SerializeToBytes()?.BytesToBase64String());
            }
            context.ExceptionHandled = true;
        }

        /// <summary>
        /// 异步发生异常时进入
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task OnExceptionAsync(ExceptionContext context)
        {
            OnException(context);
            return Task.CompletedTask;
        }

    }
    #endregion
}
