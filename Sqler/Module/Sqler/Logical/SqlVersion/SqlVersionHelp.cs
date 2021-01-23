using System.IO;
using System.Linq;
using App.Module.AutoTemp.Logical.Repository;
using Microsoft.EntityFrameworkCore;
using Vit.Orm.EntityFramework;
using App.Module.AutoTemp.Controllers;
using Vit.Extensions;
using System;
using App.Module.Sqler.AutoTemp.Logical;
using App.Module.AutoTemp.Logical;
using System.Collections.Generic;

namespace App.Module.Sqler.Logical.SqlVersion
{
    public class SqlVersionHelp
    {
        public static SqlCodeRepository[] sqlCodeRepositorys { get; private set; }
 

        static List<IDataProvider> dataProviders = new List<IDataProvider>();

        #region static Init
        public static void InitEnvironment()
        {            

            #region (x.1)初始化 DbFactory
            {
                efDbFactory = new DbContextFactory<VersionResultDbContext>().Init(SqlerHelp.sqlerConfig.GetByPath<ConnectionInfo>("SqlVersion.Config"));


                var ConnectionCreator = Vit.Orm.Dapper.ConnectionFactory.GetConnectionCreator(SqlerHelp.sqlerConfig.GetByPath<Vit.Orm.Dapper.ConnectionInfo>("SqlVersion.Config"));

                CreateOpenedDbConnection =
                () =>
                {
                    var conn = ConnectionCreator();
                    conn?.Open();
                    return conn;
                };
            }
            #endregion


            #region (x.2)初始化 moduleNames
            DirectoryInfo dir = new DirectoryInfo(SqlerHelp.GetDataFilePath("SqlVersion"));
            if (dir.Exists)
            {
                sqlCodeRepositorys = dir.GetFiles("*.json")
                    .Select(file => Path.GetFileNameWithoutExtension(file.Name))
                    .Select(name => new SqlCodeRepository(name))
                    .ToArray();
            }
            else 
            {
                sqlCodeRepositorys = new SqlCodeRepository[0];
            }
            #endregion
        }



        public static void InitAutoTemp()
        {
            //(x.1)取消注册
            if (dataProviders.Count>0)
            {
                global::App.Module.AutoTemp.Controllers.AutoTempController.UnRegistDataProvider(dataProviders.ToArray());
                dataProviders.Clear();
            }           

            //(x.2)注册 SqlVersion Config
            global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                new global::App.Module.Sqler.Logical.SqlVersion.ConfigRepository().ToDataProvider("Sqler_SqlVersion_Config"));

            //(x.3)注册 ModuleMng            
            SqlVersionModuleModel[] moduleModels = sqlCodeRepositorys.AsQueryable()
                    .Select(rep => new SqlVersionModuleModel(rep) { id = Path.GetFileNameWithoutExtension(rep.fileName) }).ToArray();

            global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                                new ModuleRepository(moduleModels).ToDataProvider("Sqler_SqlVersion_Module"));


            #region (x.4)创建并注册 sqlCodeDataProviders（VersionMng list）
            {
                var sqlCodeDataProviders = sqlCodeRepositorys.Select(repository =>
                           repository.ToDataProvider("Sqler_SqlVersion_Module_" + repository.moduleName))
                            .ToArray();

                dataProviders.AddRange(sqlCodeDataProviders);

                global::App.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(sqlCodeDataProviders);
            }
            #endregion
      
         

            #region (x.6)注册 VersionResult( from database)
            {
                EfDataProvider.DelCreateDbContext CreateDbContext = (out DbContext context) =>
                {
                    var scope = efDbFactory.CreateDbContext(out var dbContext);
                    context = dbContext;
                    return scope;
                };


                //#region 确保表 sqler_version 存在
                //{
                //    using (var scope = CreateDbContext(out var dbContext))
                //    {
                //        //dbContext.AddEntityType(typeof(Entity.sqler_version));
                //        dbContext.Database.EnsureCreated();
                //    }
                //}
                //#endregion


                var template = "Sqler_SqlVersion_VersionInfo";
                var dataProvider = new EfDataProvider(template, typeof(Entity.sqler_version), CreateDbContext);
                dataProvider.Init();

                AutoTempController.RegistDataProvider(dataProvider);

            }
            #endregion

        }


        public static void InitEnvironmentAndAutoTemp()
        {
            InitEnvironment();
            InitAutoTemp();
        }

        #endregion




        #region DbFactory
        public static DbContextFactory<VersionResultDbContext> efDbFactory { get; private set; }
        public static Func<System.Data.IDbConnection> CreateOpenedDbConnection { get; private set; }

        #endregion




        #region VersionResult




        public class VersionResultDbContext : DbContext
        {
            public VersionResultDbContext(DbContextOptions<VersionResultDbContext> options)
                : base(options)
            {
            }


            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                //(x.1)
                base.OnModelCreating(modelBuilder);

                modelBuilder.Model.AddEntityType(typeof(Entity.sqler_version));

            }

        }
        #endregion







    }
}
