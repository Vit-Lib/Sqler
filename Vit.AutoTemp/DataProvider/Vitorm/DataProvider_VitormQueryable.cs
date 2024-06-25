using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using Vit.Core.Module.Serialization;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Module.Schema;
using Vit.Extensions.Json_Extensions;
using Vit.Extensions.Linq_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Extensions.Object_Extensions;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

using Vitorm;

namespace Vit.AutoTemp.DataProvider.VitormProvider
{

    public class DataProvider_VitormQueryable : IDataProvider
    {

        public string template { get; private set; }
        public bool isTree => pidField != null;
        public TableSchema tableSchema { get; private set; }


        string idField;
        string pidField;
        Type entityType;
        JObject controllerConfig;


        Func<(IServiceScope, DbContext)> CreateDbContext;
        public Func<DbContext, IQueryable> GetQueryable;


        public DataProvider_VitormQueryable(string template, Type entityType, Func<(IServiceScope, global::Vitorm.DbContext)> CreateDbContext) :
            this(template, AutoTempHelp.EfEntityToTableSchema(entityType), entityType, CreateDbContext)
        {
        }

        public DataProvider_VitormQueryable(string template, TableSchema tableSchema, Type entityType, Func<(IServiceScope, global::Vitorm.DbContext)> CreateDbContext)
        {
            this.template = template;

            this.tableSchema = tableSchema;
            this.entityType = entityType;

            this.CreateDbContext = CreateDbContext;

            var method = (new Func<DbContext,object>(GetQuery<object>)).Method.GetGenericMethodDefinition().MakeGenericMethod(entityType);

            GetQueryable = dbContext => (IQueryable)method.Invoke(null,new[] { dbContext });

            Init();
        }

        static IQueryable GetQuery<Entity>(global::Vitorm.DbContext dbContext) 
        {
            return dbContext.Query<Entity>();
        }


        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            controllerConfig = AutoTempHelp.BuildControllerConfigByTable(tableSchema);

            idField = controllerConfig["idField"]?.Value<string>();
            pidField = controllerConfig["pidField"]?.Value<string>();
        }


        #region getConfig
        public ApiReturn getControllerConfig(object sender)
        {
            return new ApiReturn<JObject>(controllerConfig);
        }
        #endregion



        #region getList
        public ApiReturn getList(object sender, FilterRule filter, IEnumerable<OrderField> sort, PageInfo page, JObject arg)
        {
            var (scope, dbContext) = CreateDbContext();
            using (scope)
            {
                var query = GetQueryable(dbContext);

                var itemQuery = query.IQueryable_Where(filter);
                var rows = itemQuery.IQueryable_OrderBy(sort).IQueryable_Page(page).IQueryable_ToList();
                var items = Json.Deserialize<List<JObject>>(Json.Serialize(rows));
               
                var pageData = new PageData<JObject>(page) { totalCount = itemQuery.IQueryable_Count(), items= items };

                #region _childrenCount
                if (isTree)
                {
                    pageData.items.ForEach(m =>
                    {
                        var id = m[idField];
                        var filter = new FilterRule { field = pidField, @operator = "=", value = id };
                        var count = query.IQueryable_Where(filter).IQueryable_Count();
                        m["_childrenCount"] = count;
                    });
                }
                #endregion

                return new ApiReturn<object> { data = pageData };
            }
        }
        #endregion


        #region getModel
        public ApiReturn getModel(object sender, string id)
        {
            var (scope, dbContext) = CreateDbContext();
            using (scope)
            { 
                var query = GetQueryable(dbContext);

                query = query.IQueryable_Where(new FilterRule { field = idField, @operator = "=", value = id });

                return new ApiReturn<object> { data = query.IQueryable_FirstOrDefault() };
            }
        }
        #endregion

        #region insert
        public ApiReturn insert(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize(entityType);

            var (scope, dbContext) = CreateDbContext();
            using (scope)
            {
                dbContext.Add(userModel);          
            }
            return new ApiReturn<object>(userModel);
        }
        #endregion


        #region update
        public ApiReturn update(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize(entityType);

            var (scope, dbContext) = CreateDbContext();
            using (scope)
            {
                var query = GetQueryable(dbContext);

                var entity = query
                    .IQueryable_Where(new FilterRule { field = idField, @operator = "=", value = model[idField] })
                    .IQueryable_FirstOrDefault();

                if (entity == null)
                {
                    return new SsError
                    {
                        errorMessage = "待修改的数据不存在"
                    };
                }

                entity.CopyNotNullProrertyFrom(userModel);

                dbContext.Update(entity);
      
                return new ApiReturn<object> { data = entity };
            }
        }
        #endregion


        #region delete
        public ApiReturn delete(object sender, JObject arg)
        {
            var (scope, dbContext) = CreateDbContext();
            using (scope)
            {
                var queryable = GetQueryable(dbContext);

                var entity = queryable
                    .IQueryable_Where(new FilterRule { field = idField, @operator = "=", value = arg["id"].GetValue() })
                    .IQueryable_FirstOrDefault();

                if (entity == null)
                {
                    return new SsError
                    {
                        errorMessage = "数据不存在"
                    };
                }

                var count = dbContext.Delete(entity);
             
                return new ApiReturn();
            }
        }
        #endregion
    }
}
