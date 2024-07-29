using System.Data;

using Newtonsoft.Json.Linq;

using Vit.AutoTemp.DataProvider;
using Vit.Core.Module.Log;
using Vit.Core.Module.Serialization;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Db.Module.Schema;
using Vit.Extensions;
using Vit.Extensions.Serialize_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Linq;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace App.Module.Sqler.Logical.DataEditor.DataProvider
{
    public class DbSchemaDataProvider : IDataProvider
    {

        #region DataSource
        readonly List<Model> dataSource = getDataSource();
        static List<Model> getDataSource()
        {
            List<TableSchema> schema = DataEditorHelp.schema;

            #region (x.1)从数据库获取表结构
            var models = schema.SelectMany(table =>
            {
                return table.columns.Select(col =>
                {
                    var m = col.ConvertBySerialize<Model>();
                    m.name = col.column_name;
                    m.id = table.table_name + "." + col.column_name;
                    m.pid = table.table_name;
                    return m;
                })
                .Append(new Model
                {
                    name = table.table_name,
                    id = table.table_name,
                    pid = "0"
                });
            }).ToList();
            #endregion

            #region (x.2)autoTemp.json获取表字段描述
            foreach (var table in DataEditorHelp.dataEditorConfig?.Get<JObject>("dbComment") ?? new JObject())
            {
                try
                {
                    foreach (var column in table.Value.Value<JObject>())
                    {
                        var model = models.FirstOrDefault(m => m.id == table.Key + "." + column.Key);
                        if (model != null)
                            model.user_comment = column.Value.ConvertToString();
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            #endregion

            return models;
        }

        public class Model
        {

            public string id { get; set; }
            public string pid { get; set; }

            public string name { get; set; }

            /// <summary>
            /// 是否为主键(1：是,   其他：不是)
            /// </summary>
            public int? primary_key { get; set; }

            /// <summary>
            /// 是否为自增列(1：是,   其他：不是)
            /// </summary>
            public int? autoincrement { get; set; }

            /// <summary>
            /// 备注
            /// </summary>
            public string column_comment { get; set; }


            /// <summary>
            /// 备注
            /// </summary>
            public string user_comment { get; set; }

            /// <summary>
            /// db Type
            /// </summary>
            public string column_type { get; set; }
            public int _childrenCount { get; set; }
        }
        #endregion



        public string template { get; set; } = "Sqler_DataEditor_schema";


        #region getConfig
        public ApiReturn getControllerConfig(object sender)
        {
            var data = @"{
                idField: 'id',
                pidField: 'pid',
                treeField: 'name',

                dependency: {
                    css: [],
                    js: []
                },
 
                fields: [
                    {  field: 'name', title: 'name', list_width: 200,editable:false },
                    {  field: 'primary_key', title: 'primary_key', list_width: 80,editable:false },
                    {  field: 'autoincrement', title: 'autoincrement', list_width: 100,editable:false },
                    {  'ig-class':'TextArea', field: 'column_comment', title: 'column_comment', list_width: 150,editable:false },
                    {  'ig-class':'TextArea', field: 'user_comment', title: 'user_comment', list_width: 150 },
                    {  field: 'column_type', title: 'column_type', list_width: 100,editable:false }
                ],
 
                 filterFields: [
                    { field: 'name', title: 'name',filterOpt:'Contains' }
                ]
            }";
            return new ApiReturn<JObject>(Json.Deserialize<JObject>(data));
        }
        #endregion



        #region getList
        public ApiReturn getList(object sender, FilterRule filter, IEnumerable<OrderField> sort, PageInfo page, JObject arg)
        {

            var query = dataSource.AsQueryable();

            var pageData = query.ToPageData(filter, sort, page);


            // _childrenCount
            pageData.items.ForEach(m =>
            {
                m._childrenCount = query.Count(child => child.pid == m.id);
            });

            return new ApiReturn<object> { data = pageData };
        }
        #endregion


        #region getModel
        public ApiReturn getModel(object sender, string id)
        {
            var query = dataSource.AsQueryable();
            var model = query.FirstOrDefault(m => m.id == id);

            return new ApiReturn<Model>(model);
        }
        #endregion

        #region insert
        public ApiReturn insert(object sender, JObject model)
        {
            return new SsError
            {
                errorMessage = "不可新增"
            };
        }
        #endregion


        #region update
        public ApiReturn update(object sender, JObject model)
        {
            var model_ = model.ConvertBySerialize<Model>();

            var model_Data = dataSource.FirstOrDefault(m => m.id == model_.id);

            if (model_Data != null)
            {
                #region (x.1)保存
                model_Data.user_comment = model_.user_comment;
                DataEditorHelp.dataEditorConfig.SetByPath(model_.user_comment, "dbComment." + model_Data.id);
                DataEditorHelp.dataEditorConfig.SaveToFile();
                #endregion


                #region (x.2)重新初始化模板
                DataEditorHelp.InitDataProvider(model_Data.pid);
                #endregion


                return new ApiReturn<Model>(model_Data);
            }
            else
            {
                return new SsError
                {
                    errorMessage = "待修改的数据不存在"
                };
            }

        }
        #endregion


        #region delete
        public ApiReturn delete(object sender, JObject arg)
        {
            return new SsError
            {
                errorMessage = "不可删除"
            };
        }
        #endregion
    }
}
