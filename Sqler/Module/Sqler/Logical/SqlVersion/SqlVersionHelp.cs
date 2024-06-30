using Vit.Extensions;
using Vit.AutoTemp.DataProvider;
using Vitorm;
using App.Module.Sqler.Logical.SqlVersion.Entity;
using System.Data;
using Vit.AutoTemp;
using Vitorm.Sql;

namespace App.Module.Sqler.Logical.SqlVersion
{
    public class SqlVersionHelp
    {
        public static SqlCodeRepository[] sqlCodeRepositorys { get; private set; }
 

        static List<IDataProvider> dataProviders = new List<IDataProvider>();

        #region static Init
        public static SqlDbContext CreateDbContext()
        {
            return Data.DataProvider<sqler_version>().CreateSqlDbContext();
        }

        public static IDbConnection CreateOpenedDbConnection()
        {
            var conn = Data.DataProvider<sqler_version>().CreateSqlDbContext()?.dbConnection;
            conn?.Open();
            return conn;
        }



        public static void InitEnvironment()
        {

            #region (x.1) init dataSource
            {
                var dataSourceConfig = SqlerHelp.sqlerConfig.GetByPath<Dictionary<string, object>>("SqlVersion.Config");
                dataSourceConfig["namespace"] = typeof(sqler_version).Namespace;

                var moduleLabel = "configBy_SqlerSqlVersion";
                dataSourceConfig[moduleLabel] = true;

                Data.ClearDataSource(provider => provider.dataSourceConfig.ContainsKey(moduleLabel));
                var success = Data.AddDataSource(dataSourceConfig);
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
                Vit.AutoTemp.AutoTempHelp.UnRegistDataProvider(dataProviders.ToArray());
                dataProviders.Clear();
            }

            //(x.2)注册 SqlVersion Config
            Vit.AutoTemp.AutoTempHelp.RegistDataProvider(
                new global::App.Module.Sqler.Logical.SqlVersion.ConfigRepository().ToDataProvider("Sqler_SqlVersion_Config"));

            //(x.3)注册 ModuleMng
            SqlVersionModuleModel[] moduleModels = sqlCodeRepositorys.AsQueryable()
                    .Select(rep => new SqlVersionModuleModel(rep) { id = Path.GetFileNameWithoutExtension(rep.fileName) }).ToArray();

            Vit.AutoTemp.AutoTempHelp.RegistDataProvider(
                                new ModuleRepository(moduleModels).ToDataProvider("Sqler_SqlVersion_Module"));


            #region (x.4)创建并注册 sqlCodeDataProviders（VersionMng list）
            {
                var sqlCodeDataProviders = sqlCodeRepositorys.Select(repository =>
                           repository.ToDataProvider("Sqler_SqlVersion_Module_" + repository.moduleName))
                            .ToArray();

                dataProviders.AddRange(sqlCodeDataProviders);

                Vit.AutoTemp.AutoTempHelp.RegistDataProvider(sqlCodeDataProviders);
            }
            #endregion



            #region (x.6)注册 VersionResult( from database)
            {
                var template = "Sqler_SqlVersion_VersionInfo";
                var dataProvider = AutoTempHelp.CreateDataProvider<sqler_version>(template, CreateDbContext);

                Vit.AutoTemp.AutoTempHelp.RegistDataProvider(dataProvider);
            }
            #endregion

        }


        public static void InitEnvironmentAndAutoTemp()
        {
            InitEnvironment();
            InitAutoTemp();
        }

        #endregion





    }
}
