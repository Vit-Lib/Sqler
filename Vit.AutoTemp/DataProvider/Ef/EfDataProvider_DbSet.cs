using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Module.Schema;
using Vit.Extensions;
using Vit.Extensions.ObjectExt;
using Vit.Linq.Query;
using Vit.Orm.EntityFramework.Extensions;

namespace Vit.AutoTemp.DataProvider.Ef
{
    public class EfDataProvider<Model, DbContext> : IDataProvider
        where Model : class
        where DbContext : Microsoft.EntityFrameworkCore.DbContext
    {

        public string template { get; private set; }
        public bool isTree => pidField != null;
        public TableSchema tableSchema { get; private set; }


        string idField;
        string pidField;
        Type entityType;
        JObject controllerConfig;
        Func<(IServiceScope, DbContext, DbSet<Model>)> CreateDbContext;

        public EfDataProvider(string template, Type entityType, Func<(IServiceScope, DbContext, DbSet<Model>)> CreateDbContext) :
            this(template, AutoTempHelp.EfEntityToTableSchema(entityType), entityType, CreateDbContext)
        {
        }

        public EfDataProvider(string template, TableSchema tableSchema, Type entityType, Func<(IServiceScope, DbContext, DbSet<Model>)> CreateDbContext)
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
        public ApiReturn getList(object sender, List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page, JObject arg)
        {
            var (scope, dbContext,dbSet) = CreateDbContext();
            using (scope)
            {
                var queryable = dbSet.AsQueryable();

                var pageData = queryable.ToPageData(filter, sort, page, (object ori) => { return ori.ConvertBySerialize<JObject>(); });

                #region _childrenCount       
                if (isTree)
                {
                    var tasks = pageData.rows.Select(async m =>
                    {
                        var filters = new[] { new DataFilter { field = pidField, opt = "=", value = m[idField].GetValue() } };
                        var count = await queryable.IQueryable_Where(filters).Ef_CountAsync();
                        m["_childrenCount"] = count;
                    });
                    Task.WaitAll(tasks.ToArray());
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
                var queryable = dbSet.AsQueryable();

                queryable = queryable.Where(new[] { new DataFilter { field = idField, opt = "=", value = id } });

                return new ApiReturn<object> { data = queryable.FirstOrDefault() };
            }
        }
        #endregion

        #region insert
        public ApiReturn insert(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize(entityType);

            var (scope, dbContext, dbSet) = CreateDbContext();
            using (scope)
            {
                dbContext.Add(userModel);
                dbContext.SaveChanges();
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
                var queryable = dbSet.AsQueryable();

                var entity = queryable
                 .Where(new[] { new DataFilter { field = idField, opt = "=", value = model[idField] } })
                 .FirstOrDefault();

                if (entity == null)
                {
                    return new SsError
                    {
                        errorMessage = "待修改的数据不存在"
                    };
                }

                entity.CopyNotNullProrertyFrom(userModel);

                dbContext.Update(entity);
                dbContext.SaveChanges();
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
                var queryable = dbSet.AsQueryable();

                var entity = queryable
                    .Where(new[] { new DataFilter { field = idField, opt = "=", value = arg["id"].GetValue() } })
                    .FirstOrDefault();

                if (entity == null)
                {
                    return new SsError
                    {
                        errorMessage = "数据不存在"
                    };
                }

                dbContext.Remove(entity);
                dbContext.SaveChanges();
                return new ApiReturn();
            }
        }
        #endregion
    }
}
