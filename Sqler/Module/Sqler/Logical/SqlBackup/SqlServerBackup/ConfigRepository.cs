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

namespace App.Module.Sqler.Logical.SqlBackup.SqlServerBackup
{

    #region ConfigRepository
    public class ConfigRepository : IRepository<Model>
    {   

       
        public ApiReturn<Model> GetModel(string id)
        {
            var m = SqlerHelp.sqlerConfig.GetByPath<Model>("SqlBackup.SqlServerBackup");            
            return m;
        }


        public ApiReturn<Model> Update(Model m)
        {
            var data = SqlerHelp.sqlerConfig.root.JTokenGetByPath("SqlBackup", "SqlServerBackup");
            data.Replace(m.ConvertBySerialize<JToken>());
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
        /// </summary>
        [Key]
        [JsonIgnore]
        public int id { get; set; } = 1;


        /// <summary>
        /// Ms连接字符串
        /// [field:title=&lt;span title='SqlServer数据库连接字符串'&gt;Ms连接字符串&lt;/span&gt;]       
        /// [field:ig-class=TextArea]
        /// </summary>
        public String ConnectionString { get; set; }


        /// <summary>
        /// SqlServer数据库备份还原文件的所在文件夹。例：@"F:\\db"。若不指定则为 /Data/SqlServerBackup
        /// [field:title=&lt;span title='SqlServer数据库备份还原文件的文件夹路径。例：@"F:\\db"。若不指定则为 Data/SqlServerBackup'&gt;Ms备份路径&lt;/span&gt;]
        /// [field:ig-class=TextArea]
        /// </summary>
        public String BackupPath { get; set; }


        /// <summary>
        /// SqlServer数据库文件所在文件夹。例："..\DataBaseFile" 、 "C:\Program Files (x86)\Microsoft SQL Server\MSSQL\data"。若不指定则为系统默认路径
        /// 
        /// [field:title=&lt;span title='SqlServer数据库文件所在文件夹。例："..\DataBaseFile" 、 "C:\Program Files (x86)\Microsoft SQL Server\MSSQL\data"。若不指定则为系统默认路径'&gt;Ms数据库路径&lt;/span&gt;]
        /// [field:ig-class=TextArea]
        /// </summary>
        public String MdfPath { get; set; }

    }

    #endregion



}
