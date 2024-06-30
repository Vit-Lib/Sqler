using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using App.Module.Sqler.Logical;
using Sqler.Module.Sqler.Logical.SqlBackup.MySqlBackup;
using Vit.Db.DbMng;
using Vit.Extensions.Object_Serialize_Extensions;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;
using Vit.Core.Module.Serialization;
using System.Linq;
using Vit.Extensions.Linq_Extensions;
using Vit.Extensions.Json_Extensions;
using Vit.Extensions.Newtonsoft_Extensions;

namespace App.Module.Sqler.Controllers.SqlBackup
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/Sqler_SqlBackup_MySqlBackup")] 
    [ApiController]
    public class MySqlBackupController : ControllerBase
    {

        #region BackupFile_GetFileInfos
        List<BackupFileInfo> BackupFile_GetFileInfos() 
        {
            List<BackupFileInfo> backupFiles;

            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);
                backupFiles = dbMng.BackupFile_GetFileInfos();
            } 
            return backupFiles;
        }

        #endregion



        #region autoTemp      

        #region (x.1) getControllerConfig
        /// <summary>
        /// GET autoTemp/getConfig
        /// </summary>
        /// <returns></returns>
        [HttpGet("getConfig")]
        public ApiReturn getControllerConfig()
        {

            var data = @"{
                dependency: {
                    css: [],
                    js: []
                },       

                 idField: 'id',   

                /* 添加、修改、查看、删除 等权限,可不指定。 默认值均为true  */
                'permit':{
                    insert:false,
                    update:true,
                    show:false,
                    delete:true
                },

                list:{
                    rowButtons:[
                            {text:'还原',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_MySqlBackup/Restore?fileName={id}'    }     }
                    ]
                },

                fields: [                  
                    {  'ig-class': 'TextArea', field: 'fileName', title: '文件名', list_width: 400,editable:true },
                    { field: 'size', title: '大小(MB)', list_width: 80,editable:false },
                    { field: 'size_kb', title: '大小(KB)', list_width: 80,editable:false },
                    { field: 'createTime', title: '创建时间', list_width: 150,editable:false }
                ],
 
                filterFields: [
                    { field: 'fileName', title: '文件名',filterOpt:'Contains' }
                ]
            }";
            var controllerConfig = data.Deserialize<JObject>();



            #region (x.1)获取数据库状态
            EDataBaseState dbState;
            int processCount = 0;
            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);
                dbState = dbMng.GetDataBaseState();
                if (dbState == EDataBaseState.online) processCount = dbMng.GetProcessCount();
            }
            #endregion 
      

            #region (x.2)list.title
            var title = $"MySql备份与还原-- 数据库状态：{ dbState }";
            if (dbState == EDataBaseState.online) title += " -- 连接数：" + processCount;
            controllerConfig["list"]["title"] = title;
            #endregion


            #region (x.3)list.buttons
            var buttons =  new JArray();
            controllerConfig["list"]["buttons"] = buttons;

            #region (x.x.1)CreateDataBase
            if (Array.IndexOf(new[] { EDataBaseState.none, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'创建数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_MySqlBackup/CreateDataBase'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion

            #region (x.x.2)DropDataBase
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'删除数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_MySqlBackup/DropDataBase'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion




            #region (x.x.8)BackupSqler
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'Sqler备份',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_MySqlBackup/BackupSqler'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion

            #region (x.x.9)BackupSqler_NoMemoryCache
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'Sqler备份-NoMemoryCache',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_MySqlBackup/BackupSqler_NoMemoryCache'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion


            #region (x.x.10)导入导出
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'导入导出', handler: \"function(callback){ callback(); theme.popDialog('/sqler/DbBackup/MySqlPort.html', '导入导出'); }\"   }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion

            #region (x.x.11)下载建库脚本
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'下载建库脚本', handler: \"function(callback){ callback(); window.open('/sqler/Sqler_SqlBackup_MySqlBackup/CreateDataBaseSql'); }\"   }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion


            #endregion


            return new ApiReturn<JObject>(controllerConfig);
        }

        #endregion


        #region (x.2) getList
        /// <summary>
        /// GET autoTemp/getList
        /// </summary>
        /// <returns></returns>
        [HttpGet("getList")]
        public ApiReturn getList([FromQuery]string page, [FromQuery]string filter, [FromQuery]string sort, [FromQuery]string arg)
        {
            try
            {
                //(x.1)获取所有backupFiles
                List<BackupFileInfo> backupFiles= BackupFile_GetFileInfos(); 


                //(x.2)条件筛选
                var page_ = Json.Deserialize<PageInfo>(page);
                var filter_ = Json.Deserialize<FilterRule>(filter)  ;
                var sort_ = Json.Deserialize<OrderField[]>(sort);

                var queryable = backupFiles.AsQueryable();

                var pageData = queryable.Where(filter_).OrderBy(sort_).Select(m => m.ConvertBySerialize<JObject>()).ToPageData(page_);

                pageData.items.ForEach(model =>
                {
                    model["id"] = model["fileName"];
                    float size = model["size"].Value<float>();
                    model["size_kb"] = (size * 1024).ToString("f2");
                    model["size"] = size.ToString("f2");
                });

                return new ApiReturn<object> { data = pageData };
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion


        #region (x.3) getModel
        /// <summary>
        /// GET autoTemp/getModel
        /// </summary>
        /// <returns></returns>
        [HttpGet("getModel")]
        public ApiReturn<object> getModel([FromQuery]string id)
        {
            try
            {
                List<BackupFileInfo> backupFiles = BackupFile_GetFileInfos();

                var model = backupFiles.AsQueryable().Where(m=>m.fileName==id).FirstOrDefault()?.ConvertBySerialize<JObject>();
                model["id"] = model["fileName"];
                float size = model["size"].Value<float>();
                model["size_kb"] = (size * 1024).ToString("f2");
                model["size"] = size.ToString("f2");
                return model;
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion

        #region (x.5) update
        /// <summary>
        /// PUT autoTemp/update
        /// </summary>
        /// <returns></returns>
        [HttpPut("update")]
        public ApiReturn update([FromBody]JObject model)
        {
            string sourceFileName = Path.Combine(SqlerHelp.MySqlBackup_BackupPath, model["id"].ConvertToString());
            string destFileName = Path.Combine(SqlerHelp.MySqlBackup_BackupPath, model["fileName"].ConvertToString());

            global::System.IO.File.Move(sourceFileName, destFileName);

          
            return true;
        }
        #endregion


        #region (x.6) delete
        /// <summary>
        /// DELETE autoTemp/delete
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete")]
        public ApiReturn delete([FromBody]JObject model)
        {
            string sourceFileName = Path.Combine(SqlerHelp.MySqlBackup_BackupPath, model["id"].ConvertToString());
            global::System.IO.File.Delete(sourceFileName);            
            return true;
        }
        #endregion


        #endregion


        #region opt
        ///  所有操作： 创建、删除
        ///             附加、分离
        ///             强关所有连接
        ///             备份、还原
        ///             
        #region (x.1) CreateDataBase        

        [HttpPost("CreateDataBase")]
        public ApiReturn CreateDataBase()
        {
            MySqlLogical.CreateDataBase();
            return new ApiReturn();
        }
        #endregion


        #region (x.2) DropDataBase        

        [HttpPost("DropDataBase")]
        public ApiReturn DropDataBase()
        {
            MySqlLogical.DropDataBase();
            return new ApiReturn();
        }
        #endregion





        #region (x.7) BackupSqler
        [HttpPost("BackupSqler")]
        public ApiReturn BackupSqler()
        {
            MySqlLogical.BackupSqler(useMemoryCache:true);
            return new ApiReturn();
        }
        #endregion





        #region (x.8) BackupSqler_NoMemoryCache
        [HttpPost("BackupSqler_NoMemoryCache")]
        public ApiReturn BackupSqler_NoMemoryCache()
        {
            MySqlLogical.BackupSqler(useMemoryCache: false);
            return new ApiReturn();
        }
        #endregion



        #region (x.9) Restore
        [HttpPost("Restore")]
        public ApiReturn Restore([FromQuery]string fileName)
        {
            MySqlLogical.Restore(fileName: fileName);
            return new ApiReturn();
        }
        #endregion



        #region (x.11) 下载建库脚本
        [HttpGet("CreateDataBaseSql")]
        public IActionResult CreateDataBaseSql()
        {
            var sql = "";

            using (var conn = SqlerHelp.MySqlBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.MySqlBackup_CreateDbMng(conn);

                sql=dbMng.BuildCreateDataBaseSql();              
            }
            var bytes = sql.StringToBytes();
            return File(bytes, "text/plain", "CreateDataBase.sql");
        }
        #endregion


        #endregion

    }
}
