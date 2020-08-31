using System.IO;
using System.Linq;
using Sqler.Module.AutoTemp.Logical.Repository;
using Sqler.Module.AutoTemp.Logical;
using Microsoft.EntityFrameworkCore;
using Vit.Orm.EntityFramework;
using Sqler.Module.AutoTemp.Controllers;
using Vit.Extensions;
using System;
using Sqler.Module.Sqler.AutoTemp.Logical;

namespace Sqler.Module.Sqler.Logical.SqlVersion
{
    public class SqlVersionHelp
    {

        public static SqlVersionModuleModel[] moduleModels;

        static RespositoryDataProvider<SqlCodeModel>[] sqlCodeDataProviders = null;

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
                    conn.Open();
                    return conn;
                };
            }
            #endregion


        }



        public static void InitAutoTemp()
        {
            //(x.1)取消注册
            if (sqlCodeDataProviders != null)
            {
                global::Sqler.Module.AutoTemp.Controllers.AutoTempController.UnRegistDataProvider(sqlCodeDataProviders);
                sqlCodeDataProviders = null;
            }

            #region (x.2)创建 moduleModels sqlCodeDataProviders
            {
                DirectoryInfo dir = new DirectoryInfo(SqlerHelp.GetDataFilePath("SqlVersion"));
                if (dir.Exists)
                {
                    var sqlCodeRepositorys = dir.GetFiles("*.json")
                        .Select(file => Path.GetFileNameWithoutExtension(file.Name))
                        //.Select(file => file.Name)
                        .Select(name => new global::Sqler.Module.Sqler.Logical.SqlVersion.SqlCodeRepository(name))
                                .ToArray();

                    sqlCodeDataProviders = sqlCodeRepositorys.Select(repository =>
                               repository.ToDataProvider("Sqler_SqlVersion_Module_" + repository.moduleName))
                                .ToArray();

                    moduleModels = sqlCodeRepositorys.AsQueryable()
                        .Select(rep => new SqlVersionModuleModel(rep) { id = Path.GetFileNameWithoutExtension(rep.fileName) }).ToArray();
                }
            }
            #endregion

            //(x.3)注册config           
            global::Sqler.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                new global::Sqler.Module.Sqler.Logical.SqlVersion.ConfigRepository().ToDataProvider("Sqler_SqlVersion_Config"));

            //(x.4)注册 ModuleMng            
            global::Sqler.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                                new ModuleRepository().ToDataProvider("Sqler_SqlVersion_Module"));

            //(x.5)注册 VersionMng list 
            global::Sqler.Module.AutoTemp.Controllers.AutoTempController.RegistDataProvider(sqlCodeDataProviders);

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
