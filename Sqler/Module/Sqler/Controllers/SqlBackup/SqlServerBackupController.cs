using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Linq.Query;
using Vit.Extensions;
using Vit.Core.Util.ComponentModel.SsError;
using System.Linq;
using System;
using App.Module.Sqler.Logical;
using App.Module.Sqler.Logical.SqlBackup.SqlServerBackup;
using Vit.Db.DbMng.MsSql;
using Vit.Db.DbMng;

namespace App.Module.Sqler.Controllers.SqlBackup
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/Sqler_SqlBackup_SqlServerBackup")] 
    [ApiController]
    public class SqlServerBackupController : ControllerBase
    {

        #region BackupFile_GetFileInfos
        List<BackupFileInfo> BackupFile_GetFileInfos()
        {
            List<BackupFileInfo> backupFiles;

            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
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
                            {text:'还原',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/Restore?fileName={id}'    }     },
                            {text:'远程还原',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/RemoteRestore?fileName={id}'    }     }
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
            int processCount=0;
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                dbState =dbMng.GetDataBaseState();
                if(dbState== EDataBaseState.online) processCount = dbMng.GetProcessCount();
            }
            #endregion            



         
            #region (x.2)list.title
            var title = $"Sql Server备份与还原-- 数据库状态：{ dbState }";
            if (dbState == EDataBaseState.online) title += " -- 连接数：" + processCount;
            controllerConfig["list"]["title"] = title;
            #endregion


            #region (x.3)list.buttons
            var buttons =  new JArray();
            controllerConfig["list"]["buttons"] = buttons;

            #region (x.x.1)CreateDataBase
            if (Array.IndexOf(new[] { EDataBaseState.none, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'创建数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/CreateDataBase'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion

            #region (x.x.2)DropDataBase
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'删除数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/DropDataBase'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion

            #region (x.x.3)AttachDataBase
            if (Array.IndexOf(new[] { EDataBaseState.offline, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'附加数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/AttachDataBase'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion


            #region (x.x.4)DetachDataBase
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'分离数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/DetachDataBase'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion

            #region (x.x.5)KillProcess
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'强制关闭数据库连接进程',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/KillProcess'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion


            #region (x.x.6)Backup
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'备份数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/Backup'    }     }";
                buttons.Add(strButton.Deserialize<JObject>());
            }
            #endregion


            #region (x.x.8)RemoteBackup
            if (Array.IndexOf(new[] { EDataBaseState.online, EDataBaseState.unknow }, dbState) >= 0)
            {
                var strButton = "{text:'远程备份数据库',  ajax:{ type:'POST',url:'/sqler/Sqler_SqlBackup_SqlServerBackup/RemoteBackup'    }     }";
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
                var page_ = page.Deserialize<PageInfo>();
                var filter_ = filter.Deserialize<List<DataFilter>>() ?? new List<DataFilter>();
                var sort_ = sort.Deserialize<SortItem[]>();


                List<BackupFileInfo> backupFiles= BackupFile_GetFileInfos();
                
                var queryable = backupFiles.AsQueryable();

                var pageData = queryable.Where(filter_).Sort(sort_).Select(m => m.ConvertBySerialize<JObject>()).ToPageData(page_);

                pageData.rows.ForEach(model =>
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
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                string fileName= model["id"].ConvertToString();
                string newFileName = model["fileName"].ConvertToString();


                string filePathOld = dbMng.BackupFile_GetPathByName(fileName);
                string filePathNew = dbMng.BackupFile_GetPathByName(newFileName);
                global::System.IO.File.Move(filePathOld, filePathNew);
            }
            return true;
        }
        #endregion


        #region (x.6) delete
        /// <summary>
        /// DELETE autoTemp/delete
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete")]
        public ApiReturn delete([FromBody]JObject arg)
        {
            using (var conn = SqlerHelp.SqlServerBackup_CreateDbConnection())
            {
                var dbMng = SqlerHelp.SqlServerBackup_CreateDbMng(conn);
                string fileName = arg["id"].Value<string>();

                var filePath = dbMng.BackupFile_GetPathByName(fileName);

                global::System.IO.File.Delete(filePath);
          
            }
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
            SqlServerLogical.CreateDataBase();
            return new ApiReturn();
        }
        #endregion


        #region (x.2) DropDataBase        

        [HttpPost("DropDataBase")]
        public ApiReturn DropDataBase()
        {
            SqlServerLogical.DropDataBase();
            return new ApiReturn();
        }
        #endregion



        #region (x.3) AttachDataBase        

        [HttpPost("AttachDataBase")]
        public ApiReturn AttachDataBase()
        {
            SqlServerLogical.AttachDataBase();
            return new ApiReturn();
        }
        #endregion

        #region (x.4) DetachDataBase
        [HttpPost("DetachDataBase")]
        public ApiReturn DetachDataBase()
        {
            SqlServerLogical.DetachDataBase();
            return new ApiReturn();
        }
        #endregion


        #region (x.5) KillProcess
        [HttpPost("KillProcess")]
        public ApiReturn KillProcess()
        {
            SqlServerLogical.KillProcess();
            return new ApiReturn();
        }
        #endregion




        #region (x.6) Backup
        [HttpPost("Backup")]
        public ApiReturn Backup()
        {
            SqlServerLogical.Backup();
            return new ApiReturn();
        }
        #endregion


        #region (x.7) Restore
        [HttpPost("Restore")]
        public ApiReturn Restore([FromQuery]string fileName)
        {
            SqlServerLogical.Restore(fileName: fileName);
            return new ApiReturn();
        }
        #endregion



        #region (x.8) RemoteBackup
        [HttpPost("RemoteBackup")]
        public ApiReturn RemoteBackup()
        {
            SqlServerLogical.RemoteBackup();
            return new ApiReturn();
        }
        #endregion



        #region (x.9) RemoteRestore
        [HttpPost("RemoteRestore")]
        public ApiReturn RemoteRestore([FromQuery]string fileName)
        {
            SqlServerLogical.RemoteRestore(fileName: fileName);
            return new ApiReturn();
        }
        #endregion

        #endregion

    }
}
