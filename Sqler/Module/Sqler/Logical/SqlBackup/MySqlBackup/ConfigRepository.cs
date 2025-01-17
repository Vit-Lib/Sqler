﻿using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vit.AutoTemp.Repository;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Extensions.Serialize_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace App.Module.Sqler.Logical.SqlBackup.MySqlBackup
{

    #region ConfigRepository
    public class ConfigRepository : IRepository<Model>
    {


        public ApiReturn<Model> GetModel(string id)
        {
            var m = SqlerHelp.sqlerConfig.GetByPath<Model>("SqlBackup.MySqlBackup");
            return m;
        }


        public ApiReturn<Model> Update(Model m)
        {
            var data = SqlerHelp.sqlerConfig.root.JTokenGetByPath("SqlBackup", "MySqlBackup");
            data.Replace(m.ConvertBySerialize<JToken>());
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
        /// </summary>
        [Key]
        [JsonIgnore]
        public int id { get; set; } = 1;


        /// <summary>
        /// 连接字符串
        /// [field:title=&lt;span title='数据库连接字符串'&gt;连接字符串&lt;/span&gt;]       
        /// [field:ig-class=TextArea]
        /// </summary>
        public String ConnectionString { get; set; }


        /// <summary>
        /// 数据库备份还原文件的所在文件夹。例：@"F:\\db"。若不指定则为 /Data/MySqlBackup
        /// [field:title=&lt;span title='数据库备份还原文件的文件夹路径。例：@"F:\\db"。若不指定则为 Data/MySqlBackup'&gt;备份路径&lt;/span&gt;]
        /// [field:ig-class=TextArea]
        /// </summary>
        public String BackupPath { get; set; }

    }

    #endregion



}
