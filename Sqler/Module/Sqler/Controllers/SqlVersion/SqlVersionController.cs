using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using System.Linq;
using App.Module.AutoTemp.Controllers;
using App.Module.Sqler.Logical.SqlVersion.Entity;
using System;
using static App.Module.Sqler.Logical.SqlVersion.Entity.VersionManage;
using Microsoft.AspNetCore.Http;
using System.Text;
using App.Module.Sqler.Logical.SqlVersion;

namespace App.Module.Sqler.Controllers.SqlVersion
{
    [Route("sqler/SqlVersion")]
    [ApiController]
    public class SqlVersionController : ControllerBase
    {

        #region get templateList
        /// <summary>
        /// 获取模板列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("templateList")]
        public ApiReturn templateList()
        {
            try
            { 
                return new ApiReturn<List<string>> { data= AutoTempController.dataProviderMap.Keys.Where(key=>key.StartsWith("Sqler_SqlVersion_Module_")).ToList() };
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion





        #region 升級
        /// <summary>
        /// 获取模板列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("upgrade")]
        public void Upgrade([FromQuery]string module,[FromQuery]int version)
        {

            Response.ContentType = "text/html;charset=utf-8";

            Action<EMsgType, String> sendMsg = (EMsgType type, String msg) =>
            {
                msg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                switch (type)
                {
                    case EMsgType.Err:
                        {

                            Response.WriteAsync("<br/><font style='color:#f00;font-weight:bold;'>" + msg + "</font>");
                            break;
                        }
                    case EMsgType.Title:
                        {
                            Response.WriteAsync("<br/><font style='color:#005499;font-weight:bold;'>" + msg + "</font>");
                            break;
                        }
                    default:
                        {
                            Response.WriteAsync("<br/>" + msg);
                            break;
                        }
                }
                //Response.Flush();
            };
            VersionManage.UpgradeToVersion(module, version, sendMsg);
        }


        static string Str2XmlStr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        stringBuilder.Append("&quot;");
                        break;
                    case '&':
                        stringBuilder.Append("&amp;");
                        break;
                    case '<':
                        stringBuilder.Append("&lt;");
                        break;
                    case '>':
                        stringBuilder.Append("&gt;");
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }
            }
            return stringBuilder.ToString();
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

            var query = SqlVersionHelp.moduleModels.AsQueryable();
            if (!string.IsNullOrEmpty(module)) 
            {
                query = query.Where(m => m.fileName == module + ".json");
            }

            var repositorys = query.Select(m => m.repository).ToList();

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
