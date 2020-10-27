using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using App.Module.AutoTemp.Logical.Repository;

namespace App.Module.Sqler.Logical.SqlVersion
{

    #region ConfigRepository
    public class ConfigRepository : IRepository<Model>
    {   

       
        public ApiReturn<Model> GetModel(string id)
        {
            return SqlerHelp.sqlerConfig.GetByPath<Model>("SqlVersion.Config")?? new Model();           
        
        }


        public ApiReturn<Model> Update(Model m)
        {
            var data = SqlerHelp.sqlerConfig.root.JTokenGetByPath("SqlVersion", "Config");
            data.Replace(m.ConvertBySerialize<JToken>());
            SqlerHelp.sqlerConfig.SaveToFile();

            SqlVersionHelp.InitEnvironmentAndAutoTemp();

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
        /// 是否可修改sql语句
        /// </summary>
        public String SqlCodeEditable { get; set; }


    }

    #endregion



}
