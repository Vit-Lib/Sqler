using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Sqler.Module.AutoTemp.Logical.Repository;

namespace Sqler.Module.Sqler.Logical.DataEditor
{

    #region ConfigRepository
    public class ConfigRepository : IRepository<Model>
    {
        public ConfigRepository()
        {
        }

        public ApiReturn<Model> GetModel(string id)
        {
            var m = DataEditorHelp.dataEditorConfig.GetByPath<Model>("Db") ?? new Model();

            m.menu = SqlerHelp.sqlerConfig.GetByPath<JToken>("menu")?.ToString();

            return m ;
        }


        public ApiReturn<Model> Update(Model m)
        {
            var data = DataEditorHelp.dataEditorConfig.root.JTokenGetByPath("Db");
            data.Replace(m.ConvertBySerialize<JToken>());
            DataEditorHelp.dataEditorConfig.SaveToFile();

            DataEditorHelp.Init();

            SqlerHelp.sqlerConfig.root["menu"]=(m.menu?.ConvertBySerialize<JToken>());
            SqlerHelp.sqlerConfig.SaveToFile();

            return m;
        }

        public ApiReturn Delete(Model m)
        {
            throw new System.NotImplementedException();
        }

        public ApiReturn<PageData<Model>> GetList(List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page)
        {
            throw new System.NotImplementedException();
        }

        public ApiReturn<Model> Insert(Model m)
        {
            throw new System.NotImplementedException();
        }

    }
    #endregion


    #region Model
    public class Model
    {

        /// <summary>
        /// [field:visiable=false]
        /// [controller:permit.delete=false]
        /// [controller:list.title=Config]
        /// </summary>
        [Key]
        [JsonIgnore]
        public int id { get; set; } = 1;


        /// <summary>
        /// 数据库类型
        /// </summary>
        public String type { get; set; }


        /// <summary>
        /// 连接字符串[field:ig-class=TextArea]
        /// </summary>
        public String ConnectionString { get; set; }



        /// <summary>
        /// 自定义菜单[field:ig-class=TextArea]
        /// [field:ig-param={height:400}]
        /// </summary>
        public string menu { get; set; }


     

    }

    #endregion



}
