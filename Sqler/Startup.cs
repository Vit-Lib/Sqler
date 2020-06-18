using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vit.Extensions;

namespace Sqler
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

            //配置跨域
            services.AddCors();


            services.AddMvc(options =>
            {
                //使用自定义异常处理器
                //options.Filters.Add<Sers.Serslot.ExceptionFilter.ExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                //全局配置Json序列化处理

                //忽略循环引用
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

                //不更改元数据的key的大小写
                options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);     
 

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            #region 使用跨域            
            app.UseCors(builder => builder
                       .AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials());
            #endregion


            //配置静态文件
            app.UseStaticFiles(Vit.Core.Util.ConfigurationManager.ConfigurationManager.Instance.GetByPath<Vit.WebHost.StaticFilesConfig>("server.staticFiles"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            } 

          
            //app.UseHttpsRedirection();
            app.UseMvc();

            //SqlerHelp
            Task.Run(Sqler.Module.Sqler.Logical.SqlerHelp.Init);
            //Sqler.Module.Sqler.Logical.SqlerHelp.Init(); 

        }
    }
}
