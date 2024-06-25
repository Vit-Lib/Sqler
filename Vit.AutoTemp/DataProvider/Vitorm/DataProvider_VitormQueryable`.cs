using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Module.Schema;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;
using Vit.Extensions.Linq_Extensions;
using Vitorm;
using Vit.Extensions.Json_Extensions;
using Vit.Core.Module.Serialization;
using Vit.Extensions.Object_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;

namespace Vit.AutoTemp.DataProvider.Ef
{
    public class DataProvider_VitormQueryable<Model, DbContext> : IDataProvider
        where Model : class
        where DbContext : Vitorm.DbContext
    {

        public string template { get; private set; }
        public bool isTree => pidField != null;
        public TableSchema tableSchema { get; private set; }


        string idField;
        string pidField;
        Type entityType;
        JObject controllerConfig;
        Func<(IServiceScope, DbContext, DbSet<Model>)> CreateDbContext;

        public DataProvider_VitormQueryable(string template, Type entityType, Func<(IServiceScope, DbContext, DbSet<Model>)> CreateDbContext) :
            this(template, AutoTempHelp.EfEntityToTableSchema(entityType), entityType, CreateDbContext)
        {
        }

        public DataProvider_VitormQueryable(string template, TableSchema tableSchema, Type entityType, Func<(IServiceScope, DbContext, DbSet<Model>)> CreateDbContext)
        {
            this.template = template;

            this.tableSchema = tableSchema;
            this.entityType = entityType;

            this.CreateDbContext = CreateDbContext;

            Init();
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
            var (scope, dbContext, dbSet) = CreateDbContext();
            using (scope)
            { 
                var query = dbSet.Query();

                var data = query.ToPageData(filter, sort, page);
                var pageData = Json.Deserialize<PageData<JObject>>(Json.Serialize(data));

                #region _childrenCount
                if (isTree)
                {
                    pageData.items.ForEach(m =>
                    {
                        var id = m[idField];
                        var filter = new FilterRule { field = pidField, @operator = "=", value = id };
                        var count = query.Where(filter).Count();
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
            var (scope, dbContext, dbSet) = CreateDbContext();
            using (scope)
            { 
                var entity = dbSet.Get(id); 

                return new ApiReturn<object> { data = entity };
            }
        }
        #endregion

        #region insert
        public ApiReturn insert(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize<Model>();

            var (scope, dbContext, dbSet) = CreateDbContext();
            using (scope)
            {
                dbSet.Add(userModel);
            }
            return new ApiReturn<object>(userModel);
        }
        #endregion


        #region update
        public ApiReturn update(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize(entityType);

            var (scope, dbContext, dbSet) = CreateDbContext();
            using (scope)
            {
                var id = model[idField];
                var entity = dbSet.Get(id);

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
            var (scope, dbContext, dbSet) = CreateDbContext();
            using (scope)
            {
                var id = arg["id"].GetValue();
                var count = dbSet.DeleteByKey(id);

                if (count == 0)
                {
                    return new SsError
                    {
                        errorMessage = "数据不存在"
                    };
                }

                return new ApiReturn();
            }
        }
        #endregion
    }
}
