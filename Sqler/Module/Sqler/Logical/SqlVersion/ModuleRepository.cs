using System.Collections.Generic;
using System.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using Vit.Core.Util.ConfigurationManager;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Vit.AutoTemp.Repository;

namespace App.Module.Sqler.Logical.SqlVersion
{

    #region SqlVersionModuleRepository
    public class ModuleRepository : IRepository<SqlVersionModuleModel>
    {
        SqlVersionModuleModel[] moduleModels;
        public ModuleRepository(SqlVersionModuleModel[] moduleModels )
        {
            this.moduleModels = moduleModels;
        }


        public ApiReturn Delete(SqlVersionModuleModel m)
        {
            File.Delete(SqlerHelp.GetDataFilePath("SqlVersion", m.fileName));
            SqlVersionHelp.InitEnvironmentAndAutoTemp();
            return true;
        }

        public ApiReturn<PageData<SqlVersionModuleModel>> GetList(List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page)
        {
            var query = moduleModels.AsQueryable();            
 
            return  query.ToPageData(filter, sort,page);   
        }

        public ApiReturn<SqlVersionModuleModel> GetModel(string id)
        {
            var query = moduleModels.AsQueryable(); 
 
            return query.FirstOrDefault(m=>m.id==id);
        }

        public ApiReturn<SqlVersionModuleModel> Insert(SqlVersionModuleModel m)
        {
            new JsonFile(SqlerHelp.GetDataFilePath("SqlVersion", m.fileName)).SaveToFile();

            SqlVersionHelp.InitEnvironmentAndAutoTemp();
            return m;
        }

        public ApiReturn<SqlVersionModuleModel> Update(SqlVersionModuleModel m)
        {
            var query = moduleModels.AsQueryable();

            var mFromDb= query.FirstOrDefault(m_ => m_.id == m.id);
            

            FileInfo fi = new FileInfo(SqlerHelp.GetDataFilePath("SqlVersion", mFromDb.fileName));  
            fi.MoveTo(SqlerHelp.GetDataFilePath("SqlVersion", m.fileName ));

            SqlVersionHelp.InitEnvironmentAndAutoTemp();
            return m;
        }


    
    }
    #endregion


    #region Model
    public class SqlVersionModuleModel
    {
        public SqlVersionModuleModel()
        {
        }

        public SqlVersionModuleModel(SqlCodeRepository rep)
        {
            this.repository = rep;
            //id = ""+this.GetHashCode();
            fileName = rep.fileName;            
        }

        /// <summary>
        /// [fieldIgnore]
        /// </summary>
        public SqlCodeRepository repository { get; private set; }

        /// <summary>
        /// [field:visiable=false]
        /// 
        /// [controller:permit.update=false]
        /// [controller:permit.delete=false]
        /// [controller:permit.show=false]
        /// 
        /// [controller:list.buttons=\x5B
        /// {text:'一键升级',handler:'function(callback){  callback();window.open("/sqler/SqlVersion/oneKeyUpgrade"); }' }  
        /// \x5D]
        /// 
        /// [controller:list.rowButtons=\x5B
        /// {text:'管理',handler:'function(callback,id){  callback();theme.addTab("/autoTemp/Scripts/autoTemp/list.html?apiRoute=/autoTemp/data/Sqler_SqlVersion_Module_"+id+"/{action}","SqlVersion_"+id); }' } 
        /// ,{text:'升级至最新',handler:'function(callback,id){  callback();window.open("/sqler/SqlVersion/upgrade?version=-1&amp;module="+id); }' }  
        /// ,{text:'下载sql',handler:'function(callback,id){  callback();window.open("/sqler/SqlVersion/download?module="+id); }' }  
        /// \x5D]
        /// 
        /// 
        /// </summary>
        [Key]
        public string id { get; set; }  


        /// <summary>
        /// 文件名称 
        /// </summary> 
        public string fileName { get; set; }

        /// <summary>
        /// 最高版本[field:editable=false]
        /// </summary>
        public int lastVersion { get => repository?.lastVersion??-1; }


        /// <summary>
        /// 当前版本[field:editable=false]
        /// </summary>
        public int? curDbVersion { get => repository?.curDbVersion; }

    }

    #endregion


}
