using System;
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
                options.Filters.Add<Sers.Serslot.ExceptionFilter.ExceptionFilter>();
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
       



            #region 配置Swagger
            //定义一个和多个Swagger 文档
            //services.AddSwaggerGen(options =>
            //{
            //    options.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info { Title = "SwaggerDoc", Version = "v1" });

            //    // 设置SWAGER JSON和UI的注释路径。
            //    try
            //    {
            //        foreach (System.IO.FileInfo fi in new System.IO.DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.xml"))
            //        {
            //            options.IncludeXmlComments(fi.FullName);
            //        }
            //    }
            //    catch { }
            //});
            #endregion

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

            #region 启用Swagger
            //地址为 /swagger/index.html
            //启用中间件服务生成Swagger作为JSON终结点
            //app.UseSwagger();
            ////启用中间件服务对swagger-ui，指定Swagger JSON终结点
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            //});
            #endregion

          
            //app.UseHttpsRedirection();
            app.UseMvc();


            //SqlerHelp
            Sqler.Module.Sqler.Logical.SqlerHelp.Init();

        }
    }
}
