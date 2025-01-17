﻿using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vit.AutoTemp.Repository;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Extensions.Serialize_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace App.Module.Sqler.Logical.SqlVersion
{

    #region ConfigRepository
    public class ConfigRepository : IRepository<Model>
    {
        public ApiReturn<Model> GetModel(string id)
        {
            return SqlerHelp.sqlerConfig.GetByPath<Model>("SqlVersion.Config") ?? new Model();
        }

        public ApiReturn<Model> Update(Model m)
        {
            var data = SqlerHelp.sqlerConfig.root.JTokenGetByPath("SqlVersion", "Config");
            data.Replace(m.ConvertBySerialize<JToken>());
            SqlerHelp.sqlerConfig.SaveToFile();

            SqlVersionHelp.InitEnvironmentAndAutoTemp();

            return m;
        }

        public ApiReturn<PageData<Model>> GetList(FilterRule filter, IEnumerable<OrderField> sort, PageInfo page) => throw new System.NotImplementedException();
        public ApiReturn Delete(Model m) => throw new System.NotImplementedException();
        public ApiReturn<Model> Insert(Model m) => throw new System.NotImplementedException();

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
        public String provider { get; set; }

        /// <summary>
        /// dll文件
        /// </summary>
        public String assemblyFile { get; set; }


        /// <summary>
        /// 连接字符串[field:ig-class=TextArea]
        /// </summary>
        public String connectionString { get; set; }


        /// <summary>
        /// 是否可修改sql
        /// </summary>
        public String sqlCodeEditable { get; set; }


    }

    #endregion



}
