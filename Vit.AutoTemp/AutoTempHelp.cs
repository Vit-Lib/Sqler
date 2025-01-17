﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using Vit.AutoTemp.DataProvider;
using Vit.Core.Module.Log;
using Vit.Core.Util.XmlComment;
using Vit.Db.Module.Schema;
using Vit.Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Extensions.Serialize_Extensions;

namespace Vit.AutoTemp
{
    public class AutoTempHelp
    {

        #region CreateDataProvider
        public static IDataProvider_Vitorm CreateDataProvider(string template, Type entityType, Func<global::Vitorm.DbContext> CreateDbContext, TableSchema tableSchema = null)
        {
            return CreateDataProviderMethod.MakeGenericMethod(entityType).Invoke(null, new object[] { template, CreateDbContext, tableSchema }) as IDataProvider_Vitorm;
        }

        public static DataProvider_Vitorm<Model, global::Vitorm.DbContext> CreateDataProvider<Model>
            (string template, Func<global::Vitorm.DbContext> CreateDbContext, TableSchema tableSchema = null)
            where Model : class
        {
            return new DataProvider_Vitorm<Model, global::Vitorm.DbContext>(template, CreateDbContext, tableSchema);
        }
        static readonly System.Reflection.MethodInfo CreateDataProviderMethod =
            new Func<string, Func<global::Vitorm.DbContext>, TableSchema, DataProvider_Vitorm<object, global::Vitorm.DbContext>>
            (CreateDataProvider<object>)
            .Method.GetGenericMethodDefinition();

        #endregion


        #region EntityTypeToTableSchema 

        public static TableSchema EntityTypeToTableSchema(Type entityType)
        {
            TableSchema tableSchema = new TableSchema { table_name = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()?.Name, columns = new List<ColumnSchema>() };

            using (var xmlMng = new XmlCommentMng())
            {
                xmlMng.AddBin();
                var xmlHelp = xmlMng.GetXmlHelp(entityType);

                foreach (var field in entityType.GetProperties())
                {
                    tableSchema.columns.Add(new ColumnSchema
                    {
                        column_name = field.Name,
                        primary_key = (field.GetCustomAttribute<KeyAttribute>() != null) ? 1 : 0,
                        column_comment = xmlHelp?.Property_GetSummary(field),
                        column_clr_type = field.ReflectedType
                    });
                }
            }
            return tableSchema;
        }
        #endregion


        #region BuildControllerConfigByTable
        public static JObject BuildControllerConfigByEntityType(Type entityType)
        {
            return BuildControllerConfigByTable(EntityTypeToTableSchema(entityType));
        }
        public static JObject BuildControllerConfigByTable(TableSchema tableInfo)
        {

            #region SplitStringTo2
            void SplitStringTo2(string oriString, string splitString, out string part1, out string part2)
            {
                int splitIndex = oriString.IndexOf(splitString);
                if (splitIndex >= 0)
                {
                    part1 = oriString[..splitIndex];
                    part2 = oriString[(splitIndex + splitString.Length)..];
                }
                else
                {
                    part1 = oriString;
                    part2 = null;
                }
            }
            #endregion


            var controllerConfig = new JObject();

            string idField = null;

            string pidField = null;
            string rootPidValue = "0";
            string treeField = null;


            var fields = new JArray();
            var filterFields = new JArray();


            #region build field       
            Regex ctrlAttribute = new Regex("\\[[^\\[\\]]+?\\]"); //正则匹配 [editable:true] 
            foreach (var column in tableInfo.columns)
            {
                // { field: 'name', title: '装修商', list_width: 200 ,visiable:false,editable:false  }
                if (column.primary_key == 1)
                {
                    idField = column.column_name;
                }

                var field = new JObject();

                field["field"] = column.column_name;
                field["list_width"] = 200;

                if (column.primary_key == 1)
                {
                    field["editable"] = false;
                }

                #region (x.2)从column_comment获取用户配置
                //   [editable:true]
                string comment = column.column_comment;
                if (!string.IsNullOrEmpty(comment))
                {
                    foreach (Match item in ctrlAttribute.Matches(comment))
                    {
                        string key, value;

                        #region (x.x.1)获取key value 用户配置信息
                        var comm = item.Value[1..^1];

                        SplitStringTo2(comm, ":", out key, out value);
                        value = value?.Replace("\\x5B", "[").Replace("\\x5D", "]");
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        #endregion                       

                        //(x.x.2)
                        BuildFieldConfigFromComment(key, value);

                    }
                }

                #endregion

                //fieldIgnore
                if ((comment ?? "").Contains("[fieldIgnore]"))
                {
                    continue;
                }
                fields.Add(field);

                #region (x.3)设置title               
                {
                    var title = field["title"].ConvertToString();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = comment ?? column.column_name;
                    }

                    title = ctrlAttribute.Replace(title, "");
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = column.column_name;
                    }
                    field["title"] = title;
                }
                #endregion


                #region (x.4)设置筛选条件的title          
                {
                    var title = field["title"];
                    filterFields.Where(token => token["field"].EqualIgnore(column.column_name)
                    && string.IsNullOrWhiteSpace(token["title"].ConvertToString())).IEnumerable_ForEach(
                        token =>
                        {
                            token["title"] = title;
                        });
                }
                #endregion



                #region Method BuildFieldConfigFromComment
                void BuildFieldConfigFromComment(string key, string value)
                {
                    if (string.IsNullOrEmpty(key)) return;

                    #region (x.1)手动指定idField
                    if (key == "idField")
                    {
                        idField = column.column_name;
                        return;
                    }
                    #endregion

                    #region (x.2)树形列表配置                   
                    if (key == "pidField")
                    {
                        pidField = column.column_name;
                        return;
                    }
                    if (key == "treeField")
                    {
                        treeField = column.column_name;
                        return;
                    }
                    if (key == "rootPidValue")
                    {
                        rootPidValue = value;
                        return;
                    }
                    #endregion

                    #region (x.3)列表筛选条件     
                    //     [filter:开始时间,>=]   当前列作为筛选条件，筛选条件名称为开始时间，筛选方式为">="
                    // filter:
                    //  { field: 'name', title: '装修商',filterOpt:'=' }
                    if (key == "filter")
                    {
                        var prorerty = value.Split(',');

                        var filterField = new JObject()
                        {
                            ["class"] = "Text",
                            ["field"] = column.column_name,
                            //["title"] = "Text",
                            ["filterOpt"] = "="
                        };
                        filterFields.Add(filterField);

                        #region (x.x.1)筛选方式
                        if (prorerty.Length > 1)
                        {
                            filterField["filterOpt"] = prorerty[1];
                        }
                        #endregion

                        #region (x.x.2)title
                        if (prorerty.Length > 0)
                        {
                            filterField["title"] = prorerty[0];
                        }
                        #endregion
                    }
                    #endregion

                    #region (x.4)设置controller的属性
                    if (key == "controller")
                    {
                        try
                        {
                            SplitStringTo2(value, "=", out var part1, out var part2);
                            object jsonValue;
                            try
                            {
                                jsonValue = part2?.Deserialize<object>();
                            }
                            catch
                            {
                                jsonValue = part2;
                            }
                            controllerConfig.ValueSetByPath(jsonValue, part1.Split('.'));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                        return;
                    }
                    #endregion

                    #region (x.5)直接作为控件属性
                    if (key == "field")
                    {
                        try
                        {
                            SplitStringTo2(value, "=", out var part1, out var part2);
                            object jsonValue;
                            try
                            {
                                jsonValue = part2?.Deserialize<object>();
                            }
                            catch
                            {
                                jsonValue = part2;
                            }
                            field.ValueSetByPath(jsonValue, part1.Split('.'));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                        return;
                    }
                    #endregion                    
                }
                #endregion
            }
            #endregion


            controllerConfig["fields"] = fields;
            controllerConfig["filterFields"] = filterFields;

            controllerConfig["idField"] = idField;
            controllerConfig["treeField"] = treeField;
            controllerConfig["pidField"] = pidField;
            controllerConfig["rootPidValue"] = rootPidValue;

            return controllerConfig;
        }
        #endregion


        #region static dataProviderMap
        public readonly static SortedDictionary<string, IDataProvider> dataProviderMap = new SortedDictionary<string, IDataProvider>();

        public static void RegistDataProvider(params IDataProvider[] dataProviders)
        {
            lock (dataProviderMap)
            {
                foreach (var dataProvider in dataProviders)
                {
                    dataProviderMap[dataProvider.template] = dataProvider;
                }
            }
        }


        public static void UnRegistDataProvider(params IDataProvider[] dataProviders)
        {
            lock (dataProviderMap)
                foreach (var dataProvider in dataProviders)
                {
                    dataProviderMap.Remove(dataProvider.template);
                }
        }
        public static IDataProvider GetDataProvider(string template)
        {
            return dataProviderMap.TryGetValue(template, out var v) ? v : null;
        }

        #endregion
    }
}
