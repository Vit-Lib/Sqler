using Microsoft.AspNetCore.Mvc;

using Sqler.Module.Sqler.Logical.DbPort;
using Sqler.Module.Sqler.Logical.Message;

using Vit.Core.Util.Common;
using Vit.Core.Util.ComponentModel.Model;

namespace App.Module.Sqler.Controllers.DbPort
{
    /// <summary>
    /// 
    /// </summary>
    [Route("sqler/DbPort")]
    [ApiController]
    public class DbPortController : ControllerBase
    {
        #region Util
        void SendMsg(EMsgType type, String msg)
        {
            MessageHelp.SendMsg(Response, type, msg);
        }
        #endregion





        #region (x.1) Export

        [HttpPost("Export")]
        [HttpGet("Export")]
        public void Export
           ([FromForm] string type,
            [FromForm] string connectionString,
            [FromForm, SsDescription("sqlite/sqlite-NoMemoryCache/excel/csv/txt")] string exportFileType)
        {
            Response.ContentType = "text/html;charset=utf-8";

            DbPortLogical.Export(SendMsg,
                type, connectionString,
                exportFileType
                );
        }
        #endregion


        #region (x.2) Import


        [HttpPost("Import")]
        [DisableRequestSizeLimit]
        public void Import(
            [FromForm] IList<IFormFile> files,
            [FromForm] string type,
            [FromForm] string connectionString,
            [FromForm, SsDescription("on代表true")] string createTable,
            [FromForm] string delete,
            [FromForm] string truncate
            )
        {
            Response.ContentType = "text/html;charset=utf-8";



            //(x.2)连接字符串
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                SendMsg(EMsgType.Err, "import error - invalid arg conn.");
                return;
            }


            #region (x.3)检验文件是否合法
            if (files == null || files.Count != 1)
            {
                SendMsg(EMsgType.Err, "请指定合法的文件");
                return;
            }
            #endregion


            #region (x.4)文件保存至本地
            var file = files[0];
            string filePath = CommonHelp.GetAbsPath("wwwroot", "temp", "Import", DateTime.Now.ToString("yyyyMMdd_HHmmss_") + file.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                file.CopyTo(fs);
            }
            #endregion


            //(x.5)导入数据
            DbPortLogical.Import(SendMsg, filePath, type, connectionString, createTable == "on", delete == "on", truncate == "on");

        }

        #endregion



        #region (x.3) DataTransfer

        [HttpPost("DataTransfer")]
        [DisableRequestSizeLimit]
        public void DataTransfer(

            [FromForm] string from_type,
            [FromForm] string from_ConnectionString,
            [FromForm] string from_sql,
            [FromForm] string to_type,
            [FromForm] string to_ConnectionString,

            [FromForm, SsDescription("on代表true")] string createTable,
            [FromForm] string delete,
            [FromForm] string truncate
            )
        {
            Response.ContentType = "text/html;charset=utf-8";

            DbPortLogical.DataTransfer(SendMsg,
                from_type, from_ConnectionString, from_sql,
                to_type, to_ConnectionString,
                createTable == "on", delete == "on", truncate == "on");
        }

        #endregion




    }
}
