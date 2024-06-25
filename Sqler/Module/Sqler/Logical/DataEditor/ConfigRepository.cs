using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.ComponentModel.DataAnnotations;

using Vit.AutoTemp.Repository;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Extensions.Json_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace App.Module.Sqler.Logical.DataEditor
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

        public ApiReturn<PageData<Model>> GetList(FilterRule filter, IEnumerable<OrderField> sort, PageInfo page)
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
