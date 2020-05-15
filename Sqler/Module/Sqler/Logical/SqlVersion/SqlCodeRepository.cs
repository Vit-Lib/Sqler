using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using Vit.Core.Util.ConfigurationManager;
using System;
using System.ComponentModel.DataAnnotations;
using Sqler.Module.Sqler.Logical.SqlVersion.Entity;
using Sqler.Module.AutoTemp.Logical.Repository;

namespace Sqler.Module.Sqler.Logical.SqlVersion
{

    #region SqlVersionRepository
    public class SqlCodeRepository : IRepository<SqlCodeModel>
    {
        public readonly string moduleName;
        public readonly string fileName;
        public JsonFile dataSource { get; private set; }
        public SqlCodeRepository(string moduleName)
        {
            this.moduleName = moduleName;
            fileName = moduleName + ".json";
            dataSource = new JsonFile(SqlerHelp.GetDataFilePath("SqlVersion", fileName));    
        }

        /// <summary>
        /// 最高版本
        /// </summary>
        public int lastVersion
        {
            get
            {
                return dataSource.root["data"]?.Value<JArray>()?.Count??0;
            }
        }

        /// <summary>
        /// 数据库当前版本
        /// </summary>
        public int curDbVersion { get=> VersionManage.GetDbCurVersion(moduleName);  }


        public ApiReturn Delete(SqlCodeModel m)
        {
            throw new System.NotImplementedException();
        }

        public ApiReturn<PageData<SqlCodeModel>> GetList(List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page)
        {
            var queryable=(dataSource.GetByPath<List<SqlCodeModel>>("data")??new List<SqlCodeModel>()).AsQueryable();
            return  queryable.ToPageData(filter, sort,page);   
        }

        public ApiReturn<SqlCodeModel> GetModel(string id)
        {
            var data = dataSource.root["data"].Value<JArray>();
            var m= data[int.Parse(id) - 1].Deserialize<SqlCodeModel>();
            return m;
        }

        public ApiReturn<SqlCodeModel> Insert(SqlCodeModel m)
        {
            var data = dataSource.root["data"]?.Value<JArray>();
            if (data == null)
            {
                dataSource.root["data"] = data = new JArray();
            }

            m.version = data.Count+1;
            m.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            data.Add(m.ConvertBySerialize<JToken>());
            dataSource.SaveToFile();
            return m;
        }

        public ApiReturn<SqlCodeModel> Update(SqlCodeModel m)
        {
            var data = dataSource.root["data"].Value<JArray>();
            var modelFromDb = data[m.version.Value - 1];

            modelFromDb.Replace(m.ConvertBySerialize<JToken>());

            dataSource.SaveToFile();

            return m;
        }
    }
    #endregion


    #region Model
    public class SqlCodeModel
    {

        /// <summary>
        /// 版本号[field:list_width=80] 
        /// [controller:permit.delete=false]    
        /// 
        /// [controller:list.rowButtons=\x5B
        ///   {text:'升级至此版本',handler:'function(callback,id){  callback();window.open("/sqler/SqlVersion/upgrade?version="+id+"&amp;module="+document.url_GetCurArg("apiRoute").slice(39,-9)); }' }  
        /// \x5D]
        /// 
        /// 
        /// 
        /// </summary>
        [Key]
        public int? version { get; set; }


        /// <summary>
        /// 作者[field:list_width=80] 
        /// </summary>
        public String author { get; set; }

        /// <summary>
        /// sql代码
        /// [field:ig-class=TextArea] 
        /// [field:ig-param={height:200}]
        /// </summary>
        public String code { get; set; }

        /// <summary>
        /// 说明
        /// [field:ig-class=TextArea]       
        /// [field:ig-param={height:50}]
        /// </summary>
        public String comment { get; set; }


        /// <summary>
        /// 添加时间[field:editable=false]
        /// </summary>
        public string time { get; set; } 


        /// <summary>
        /// 类型。（表结构、表字段、函数、存储过程、视图、触发器、表数据）
        /// [field:title=类型]
        /// </summary>
        public String attr_type { get; set; }
        /// <summary>
        /// 操作。语句所做操作。（增加、删除、修改、重命名） 
        /// [field:title=操作]
        /// </summary>
        public String attr_Opt { get; set; }

        /// <summary>
        /// 处理对象。例如： T_TreeData
        /// [field:title=处理对象]
        /// </summary>
        public String attr_obj { get; set; }
        /// <summary>
        /// 处理对象2。例如： T_TreeData
        /// [field:title=处理对象2]
        /// </summary>
        public String attr_obj2 { get; set; }

        /// <summary>
        /// 是否为系统语句。"是":是。其他(尤指"否")：否。
        /// [field:title=系统语句]
        /// </summary>
        public String attr_isSys { get; set; }

        /// <summary>
        /// 额外说明
        /// </summary>
        public String attr_ext { get; set; }
    }

    #endregion


}
