using App.Module.Sqler.Logical.DataEditor.DataProvider;

using Vit.AutoTemp;
using Vit.AutoTemp.DataProvider;
using Vit.Core.Module.Log;
using Vit.Core.Util.ConfigurationManager;
using Vit.Db.Module.Schema;
using Vit.DynamicCompile.EntityGenerate;
using Vit.Extensions;
using Vit.Extensions.Db_Extensions;

using Vitorm;
using Vitorm.Sql;

namespace App.Module.Sqler.Logical.DataEditor
{
    public class DataEditorHelp
    {
        const string entityNamespace = "Sqler.DataEditor";
        #region static Init
        static IEnumerable<IDataProvider> dataProviders = null;

        public static DbContext CreateDbContext() => Data.DataProvider(entityNamespace).CreateDbContext();
        public static SqlDbContext CreateSqlDbContext() => CreateDbContext() as SqlDbContext;
        public static bool Init()
        {

            // #1 unregist DataProvider
            if (dataProviders != null)
            {
                Vit.AutoTemp.AutoTempHelp.UnRegistDataProvider(dataProviders.ToArray());
                dataProviders = null;
            }



            // #2 init config of Vitorm.Data 
            {
                // dataSourceConfig
                var dataSourceConfig = dataEditorConfig.GetByPath<Dictionary<string, object>>("Vitorm");
                dataSourceConfig["namespace"] = entityNamespace;

                var moduleLabel = "configBy_SqlerDataEditor";
                dataSourceConfig[moduleLabel] = true;

                Data.ClearDataSource(provider => provider.dataSourceConfig.ContainsKey(moduleLabel));
                var success = Data.AddDataSource(dataSourceConfig);

                if (!success)
                {
                    return false;
                }
            }



            {
                //DbData
                try
                {
                    var provideArray = CreateDataProviderFromDb();
                    dataProviders = provideArray;
                    Vit.AutoTemp.AutoTempHelp.RegistDataProvider(provideArray);
                }
                catch (System.Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            {
                //DbSchemaDataProvider
                try
                {
                    Vit.AutoTemp.AutoTempHelp.RegistDataProvider(new DbSchemaDataProvider());
                }
                catch (System.Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return true;

        }
        #endregion

        public static readonly JsonFile dataEditorConfig = new JsonFile(SqlerHelp.GetDataFilePath("sqler.DataEditor.json"));



        #region dataProviderMap

        public static void InitDataProvider(string tableName)
        {
            var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider("Sqler_DataEditor_Db_" + tableName) as IDataProvider_Vitorm;
            InitDataProvider(dataProvider);
        }
        public static void InitDataProvider(IDataProvider_Vitorm dataProvider)
        {
            if (dataProvider == null) return;

            var tableInfo = dataProvider.tableSchema;
            #region getComment from json config
            tableInfo.columns.ForEach(
                col =>
                {
                    var column_comment = DataEditorHelp.dataEditorConfig.GetStringByPath("dbComment." + tableInfo.table_name + "." + col.column_name);
                    if (!string.IsNullOrEmpty(column_comment)) col.column_comment = column_comment;
                }
            );
            #endregion


            dataProvider.Init();
        }
        #endregion


        #region CreateDataProviderFromDb
        public static List<TableSchema> schema { get; private set; }
        static IDataProvider[] CreateDataProviderFromDb()
        {
            using var dbContext = CreateSqlDbContext();
            var dbConn = dbContext.dbConnection;
            schema = dbConn.GetSchema();

            return schema.Select((tableSchema) =>
                {
                    var template = "Sqler_DataEditor_Db_" + tableSchema.table_name;
                    var entity = EntityHelp.GenerateEntityBySchema(tableSchema, entityNamespace);
                    var dataProvider = AutoTempHelp.CreateDataProvider(template, entity, CreateDbContext, tableSchema);
                    InitDataProvider(dataProvider);
                    return dataProvider;
                }
            ).ToArray();
        }

        #endregion

    }
}
