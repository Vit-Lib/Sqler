using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.Common;
using Vit.AutoTemp.DataProvider;
using Vit.Extensions.Object_Serialize_Extensions;
using Vit.Linq.Filter.ComponentModel;
using Vit.Linq.ComponentModel;
using Vit.Extensions.Linq_Extensions;
using Vit.Extensions.Json_Extensions;
using Vit.Extensions.Object_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;

namespace Vit.AutoTemp.Demo
{
    public class DemoDataProvider : IDataProvider
    {

        #region DataSource
        static List<Model> dataSource = getDataSource();


        static List<Model> getDataSource()
        {
            var list = new List<Model>(1000);
            for (int t = 1; t <= 1000; t++)
            {
                list.Add(new Model
                {
                    id = t,
                    pid = t / 10,
                    name = "name" + t,
                    age = 20,
                    sex = "1",
                    random = "" + CommonHelp.NewGuidLong()
                });
            }

            return list;
        }

        public class Model
        {
            public int id { get; set; }
            public int pid { get; set; }
            public string name { get; set; }
            public int age { get; set; }
            public string sex { get; set; }

            public string random { get; set; }
            public string random2 { get; set; }
            public int? _childrenCount { get; set; }
        }
        #endregion



        public string template { get; set; } = "demo_list";

        public bool isTree = false;

        #region getConfig
        public ApiReturn getControllerConfig(object sender)
        {         

            var data = @"{

                dependency: {
                    css: [],
                    js: []
                },

                /* 添加、修改、查看、删除 等权限,可不指定。 默认值均为true  */
                '//permit':{
                    insert:false,
                    update:false,
                    show:false,
                    delete:false                 
                },

                idField: 'id',               
                " + (isTree?@"treeField: 'name',":"") + @"
                rootPidValue:'0',   

                list:{
                    title:'autoTemp-demo',
                    buttons:[
                            {text:'执行js',    handler:'function(callback){  setTimeout(callback,5000); }'    },
                            {text:'调用接口',  ajax:{ type:'GET',url:'/autoTemp/data/demo_list/getConfig'    }     }
                    ],
                    rowButtons:[
                            {text:'查看id',    handler:'function(callback,id){  callback();alert(id); }'    },
                            {text:'调用接口',  ajax:{ type:'GET',url:'/autoTemp/data/demo_list/getConfig?name={id}'    }     }
                    ]
                },

               
                fields: [                  
                    { field: 'name', title: '<span title=""装修商名称"">装修商</span>', list_width: 200,editable:false },
                    { field: 'sex', title: '性别', list_width: 80,visiable:false },
                    { 'ig-class': 'TextArea', field: 'random', title: 'random', list_width: 150 },
                    { field: 'random2', title: 'random2', list_width: 150 }
                ],
 
                filterFields: [
                    { field: 'name', title: '装修商',filterOpt:'Contains' },
                    { field: 'sex', title: '性别' },
                    { field: 'random', title: 'random' }
                ]
            }";
            return new ApiReturn<JObject>(data.Deserialize<JObject>());   
        }
        #endregion



        #region getList
        public ApiReturn getList(object sender, FilterRule filter, IEnumerable<OrderField> sort, PageInfo page, JObject arg)
        {
            var query = dataSource.AsQueryable();

            var pageData = query.ToPageData(filter, sort, page);


            // childrenCount
            pageData.items.ForEach(m =>
            {
                m._childrenCount = query.Count(child => child.pid == m.id);
            });
        

            return new ApiReturn<PageData<Model>> { data = pageData };
        }
        #endregion


        #region getModel
        public ApiReturn  getModel(object sender, string id)
        {
            if (!int.TryParse(id, out int m_id))
            {
                return new ApiReturn
                {
                    error = new Vit.Core.Util.ComponentModel.SsError.SsError
                    {
                        errorMessage = "数据不存在"
                    }
                };
            }

            var query = dataSource.AsQueryable();
            var model = query.FirstOrDefault(m => m.id == m_id);

            return new ApiReturn<Model>(model);
        }
        #endregion

        #region insert
        public ApiReturn  insert(object sender, JObject model)
        {
            var model_ = model.ConvertBySerialize<Model>();

            model_.id = dataSource[dataSource.Count - 1].id + 1;
            dataSource.Add(model_);

            return new ApiReturn<Model>(model_);
        }
        #endregion


        #region update
        public ApiReturn update(object sender, JObject model)
        {
            var model_ = model.ConvertBySerialize<Model>();

            var model_Data = dataSource.FirstOrDefault(m => m.id == model_.id);

            if (model_Data != null)
            {
                model_Data.CopyNotNullProrertyFrom(model_);

                return new ApiReturn<Model>(model_Data);
            }
            else
            {
                return new Vit.Core.Util.ComponentModel.SsError.SsError
                {
                    errorMessage = "待修改的数据不存在"
                };
            }

        }
        #endregion


        #region delete
        public ApiReturn delete(object sender, JObject arg)
        {
            if (!arg["id"].TryParseIgnore(out int id))
            {
                return new Vit.Core.Util.ComponentModel.SsError.SsError
                {
                    errorMessage = "数据不存在"
                };
            }

            var model_Data = dataSource.FirstOrDefault(m => m.id == id);

            if (model_Data != null)
            {
                dataSource.Remove(model_Data);
                return new ApiReturn();
            }
            else
            {
                return new Vit.Core.Util.ComponentModel.SsError.SsError
                {
                    errorMessage = "待删除的数据不存在"
                };
            }
        }
        #endregion
    }
}
