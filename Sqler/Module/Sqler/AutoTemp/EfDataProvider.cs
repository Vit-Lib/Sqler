using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using Vit.Extensions.ObjectExt;
using Vit.Orm.EntityFramework.Extensions;
using System;
using Vit.Core.Util.ComponentModel.SsError;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vit.Orm.Dapper.Schema;
using Sqler.Module.AutoTemp.Logical;

namespace Sqler.Module.Sqler.AutoTemp.Logical
{

    public class EfDataProvider : IDataProvider 
    {

        public delegate IServiceScope DelCreateDbContext(out DbContext context);

        DelCreateDbContext CreateDbContext;
 
        public string template { get; private set; }

        JObject controllerConfig;

        string tableName;

        string idField;
        string pidField;



        public bool isTree => pidField != null;

        Type entityType;

        public TableSchema tableSchema { get; private set; }


        public EfDataProvider(string template, Type entityType, DelCreateDbContext CreateDbContext) :
            this(template,AutoTempHelp.EfEntityToTableSchema(entityType), entityType, CreateDbContext)
        {
        }

        public EfDataProvider(string template, TableSchema tableSchema, Type entityType, DelCreateDbContext CreateDbContext)
        {
            this.template = template;

            this.tableSchema = tableSchema;
            this.entityType = entityType;

            this.CreateDbContext = CreateDbContext;

            //Init();
        }

        
        /// <summary>
        /// 需要手动调用
        /// </summary>
        public void Init()
        {
            tableName = tableSchema.table_name;

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
            using (var scope = CreateDbContext(out var db))
            {
                
                var queryable = db.GetQueryableByTableName(tableName);

                var pageData = queryable.Ef_ToPageData(filter, sort, page, (object ori) => { return ori.ConvertBySerialize<JObject>(); });

                #region _childrenCount       
                if (isTree)
                {
                    var task_count = pageData.rows.Select(async m =>
                    {
                        var filters = new[] { new DataFilter { field = pidField, opt = "=", value = m[idField].GetValue() } };
                        var count = await queryable.IQueryable_Where(filters).Ef_CountAsync();
                        m["_childrenCount"] = count;
                    });
                    Task.WaitAll(task_count.ToArray());
                }
                #endregion

                return new ApiReturn<object> { data = pageData };
            }
        }
        #endregion


        #region getModel
        public ApiReturn getModel(object sender, string id)
        {
            using (var scope = CreateDbContext(out var db))
            {
                var queryable = db.GetQueryableByTableName(tableName);

                queryable = queryable.IQueryable_Where(new DataFilter { field = idField, opt = "=", value = id });

                return new ApiReturn<object> { data = queryable.Ef_FirstOrDefault() };
            }
        }
        #endregion

        #region insert
        public ApiReturn insert(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize(entityType);

            using (var scope = CreateDbContext(out var db))
            {
                db.Add(userModel);
                db.SaveChanges();
            }
            return new ApiReturn<object>(userModel);
        }
        #endregion


        #region update
        public ApiReturn update(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize(entityType);

            using (var scope = CreateDbContext(out var db))
            {
                var queryable = db.GetQueryableByTableName(tableName);

                var entity = queryable
                    .IQueryable_Where(new DataFilter { field = idField, opt = "=", value = model[idField] })
                    .Ef_FirstOrDefault();

                if (entity == null)
                {
                    return new SsError
                    {
                        errorMessage = "待修改的数据不存在"
                    };
                }

                entity.CopyNotNullProrertyFrom(userModel);

                db.Update(entity);
                db.SaveChanges();
                return new ApiReturn<object> { data = entity };
            }
        }
        #endregion


        #region delete
        public ApiReturn delete(object sender, JObject arg)
        {
            using (var scope = CreateDbContext(out var db))
            {
                var queryable = db.GetQueryableByTableName(tableName);

                var entity = queryable
                    .IQueryable_Where(new DataFilter { field = idField, opt = "=", value = arg["id"].GetValue() })
                    .Ef_FirstOrDefault();

                if (entity == null)
                {
                    return new SsError
                    {
                        errorMessage = "数据不存在"
                    };
                }

                db.Remove(entity);
                db.SaveChanges();
                return new ApiReturn();
            }
        }
        #endregion
    }
}
