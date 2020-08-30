using Microsoft.AspNetCore.Builder;
using Sqler.Module.FileMng.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vit.Core.Module.Log;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Extensions;

namespace Sqler.Module.FileMng
{
    public class FileMngHelp
    {

        #region InitAutoTemp
        public static void InitAutoTemp(IApplicationBuilder app)
        {
            Logger.Info("[FileMng]init ...");

            AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                           new FileMngRepository().ToDataProvider("FileMng_FileMng"));



            app.UseChunkUpload("/fileMng/uploadChunkFile", (fileContent, fileName, content) => {

                var dirPath = FileMngRepository.GetFilePathById(content.Request.Form["id"].ToString());

                string filePath=Path.Combine(dirPath,fileName);

                if (File.Exists(filePath)) 
                {
                    File.Delete(filePath);
                }

                File.WriteAllBytesAsync(filePath, fileContent);
                return true;
                //ApiReturn apiRet = (ApiReturn<string>)("/file/" + fileName);
                //return apiRet;
            });




            Logger.Info("[FileMng]init succeed!");

        }
        #endregion

    }
}
