using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using App.Module.Sqler.Logical.SqlVersion;
using Sqler.Module.Sqler.Logical.MessageWrite;

namespace App.Module.Sqler.Controllers.SqlVersion
{
    [Route("sqler/SqlVersion")]
    [ApiController]
    public class SqlVersionController : ControllerBase
    {


        #region Util
        void SendMsg(EMsgType type, String msg)
        {
            MessageWriteHelp.SendMsg(Response, type, msg, false);
        }
        #endregion


        #region 升級

        /// <summary>
        /// 升级
        /// </summary>
        /// <returns></returns>
        [HttpGet("upgrade")]
        public void Upgrade([FromQuery]string module,[FromQuery]int version)
        {

            Response.ContentType = "text/html;charset=utf-8";
    
            VersionManage.UpgradeToVersion(module, SendMsg, version);
        }



        /// <summary>
        /// 一键升级
        /// </summary>
        /// <returns></returns>
        [HttpGet("oneKeyUpgrade")]
        public void OneKeyUpgrade()
        {

            Response.ContentType = "text/html;charset=utf-8";           

           
            foreach (var sqlCodeRes in SqlVersionHelp.sqlCodeRepositorys) 
            {          
                VersionManage.UpgradeToVersion(sqlCodeRes.moduleName, SendMsg);
            } 
        }
 
        #endregion






        #region DownloadSql
        /// <summary>
        /// 获取模板列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("download")]
        public void DownloadSql([FromQuery]string module)
        {

            Response.ContentType = "text/file;charset=utf-8";
            //Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(responseFileName, Encoding.UTF8));
            Response.Headers.Add("Content-Disposition", "attachment;filename=sqler("+DateTime.Now.ToString("yyyy-MM-dd")+").sql");

            Action<String> sendMsg = (String msg) =>
            {
                Response.WriteAsync(msg + "\r\n");
            };

            var query = SqlVersionHelp.sqlCodeRepositorys.AsQueryable();           
        
            if (!string.IsNullOrEmpty(module)) 
            {
                query = query.Where(m => m.moduleName == module);
            }
 
            var repositorys = query.ToList();

            foreach (var repository in repositorys)
            {
                sendMsg("---------------------------------------------------------------------------");

                sendMsg("-- module: "+ repository.moduleName );

                var codes = (repository.dataSource.GetByPath<List<SqlCodeModel>>("data"));
                if (codes == null) continue;

                #region foreach

                foreach (var code in codes)
                {
                    sendMsg("---------------------------------");
                    sendMsg("-- version: " + code.version +"  time:" + code.time );
                    if (!String.IsNullOrWhiteSpace(code.comment))
                    {
                        sendMsg("\r\n/* ");
                        sendMsg(code.comment);
                        sendMsg("*/\r\n");
                    }
                    sendMsg(code.code?.Replace("\n", "\r\n"));
                }
                #endregion
            } 
        }
        #endregion



    }
}
