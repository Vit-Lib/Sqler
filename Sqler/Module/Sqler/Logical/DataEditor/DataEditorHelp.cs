﻿using Microsoft.EntityFrameworkCore;
using App.Module.AutoTemp.Controllers;
using App.Module.Sqler.AutoTemp.Logical;
using App.Module.Sqler.Logical.DataEditor.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using Vit.Core.Module.Log;
using Vit.Core.Util.ConfigurationManager;
using Vit.Extensions;
using Vit.Orm.EntityFramework;
using Vit.Orm.EntityFramework.Dynamic;
using Vit.Db.Module.Schema;

namespace App.Module.Sqler.Logical.DataEditor
{
    public class DataEditorHelp
    {

        #region static Init
        public static bool Init()
        {
            //(x.1) init conn
            {
                var connInfo = dataEditorConfig.GetByPath<ConnectionInfo>("Db");


                if (connInfo == null || string.IsNullOrEmpty(connInfo.type) || string.IsNullOrEmpty(connInfo.ConnectionString))
                {
                    return false;
                }

                efDbFactory =
                new DbContextFactory<AutoMapDbContext>().Init(connInfo);
            }



            {
                //DbData
                try
                {
                    AutoTempController.RegistDataProvider(CreateEfDataProviderFromDb());
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
                    AutoTempController.RegistDataProvider(new DbSchemaDataProvider());
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


        public static DbContextFactory<AutoMapDbContext> efDbFactory { get; private set; }


        #region dataProviderMap       

        public static void InitDataProvider(string tableName)
        {
            var dataProvider = AutoTempController.GetDataProvider("Sqler_DataEditor_Db_" + tableName) as EfDataProvider;
            InitDataProvider(dataProvider);
        }
        public static void InitDataProvider(EfDataProvider dataProvider)
        {           
            if (dataProvider == null) return;

            var tableInfo = dataProvider.tableSchema;
            #region getComment from json config
            tableInfo.columns.ForEach(col => {

                var column_comment = DataEditorHelp.dataEditorConfig.GetStringByPath("dbComment." + tableInfo.table_name + "." + col.column_name);
                if (!string.IsNullOrEmpty(column_comment)) col.column_comment = column_comment;
            });
            #endregion


            dataProvider.Init();         
        }
        #endregion


        #region CreateEfDataProviderFromDb

        static EfDataProvider[] CreateEfDataProviderFromDb()
        {
            List<TableSchema> schema;
            Dictionary<string, Type> entityMap;
            using (var scope = DataEditorHelp.efDbFactory.CreateDbContext(out var db))
            {
                //先调用，确保已经映射实体
                entityMap = db.GetEntityTypeMap();

                schema = db.AutoGeneratedEntity_schema;
            }


            EfDataProvider.DelCreateDbContext CreateDbContext  =(out DbContext context) => {
                var scope= DataEditorHelp.efDbFactory.CreateDbContext(out var con);
                context = con;
                return scope;
            };

            return schema.Select((tableSchema) =>
            {
                var template = "Sqler_DataEditor_Db_" + tableSchema.table_name;
                var dataProvider= new EfDataProvider(template, tableSchema, entityMap[tableSchema.table_name], CreateDbContext);
                InitDataProvider(dataProvider);
                return dataProvider;
            }
            ).ToArray();
        }

        #endregion

    }
}
