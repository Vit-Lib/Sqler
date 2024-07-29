using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using Vit.Core.Module.Serialization;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Module.Schema;
using Vit.Extensions.Serialize_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Extensions.Object_Extensions;
using Vit.Linq;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace Vit.AutoTemp.DataProvider
{
    public class DataProvider_Vitorm<Model, DbContext> : IDataProvider_Vitorm
        where Model : class
        where DbContext : Vitorm.DbContext
    {

        public string template { get; private set; }
        public bool isTree => pidField != null;
        public TableSchema tableSchema { get; private set; }


        string idField;
        string pidField;
        readonly Type entityType;
        JObject controllerConfig;
        readonly Func<DbContext> CreateDbContext;


        public DataProvider_Vitorm(string template, Func<DbContext> CreateDbContext, TableSchema tableSchema = null)
        {
            this.template = template;


            entityType = typeof(Model);

            tableSchema ??= AutoTempHelp.EntityTypeToTableSchema(entityType);
            this.tableSchema = tableSchema;



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
            using var dbContext = CreateDbContext();

            var query = dbContext.Query<Model>();

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
        #endregion


        #region getModel
        public ApiReturn getModel(object sender, string id)
        {
            using var dbContext = CreateDbContext();
            var entity = dbContext.Get<Model>(id);

            return new ApiReturn<object> { data = entity };
        }
        #endregion

        #region insert
        public ApiReturn insert(object sender, JObject model)
        {
            var userModel = model.ConvertBySerialize<Model>();

            using (var dbContext = CreateDbContext())
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

            using var dbContext = CreateDbContext();

            var id = model[idField].GetValue();
            var entity = dbContext.Get<Model>(id);

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
        #endregion


        #region delete
        public ApiReturn delete(object sender, JObject arg)
        {
            using var dbContext = CreateDbContext();
            var id = arg["id"].GetValue();
            var count = dbContext.DeleteByKey<Model>(id);

            if (count == 0)
            {
                return new SsError
                {
                    errorMessage = "数据不存在"
                };
            }

            return new ApiReturn();
        }
        #endregion
    }
}
