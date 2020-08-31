using Microsoft.AspNetCore.Builder;
using Sqler.Module.FileMng.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vit.Core.Module.Log;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Extensions;
using Vit.Http.ChunkUpload;

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



            app.UseChunkUpload(new UploadChunk
            {
                apiRoute = "/fileMng/uploadChunkFile",
                expireTime=TimeSpan.FromMinutes(1),
                onUploadedFile = (fileContent, fileName, content) => {

                    var id = content.Request.Form["id"].ToString();

                    if (FileMngRepository.GetFileModel(id)?.data?.type != "文件夹")
                    {
                        return new SsError { errorMessage = "只能上传文件到文件夹内" };
                    }

                    var dirPath = FileMngRepository.GetFilePathById(id);

                    string filePath = Path.Combine(dirPath, fileName);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }


                    //File.WriteAllBytesAsync(filePath, fileContent);
                    using (var  file = new FileStream(filePath,FileMode.CreateNew))
                    {
                        foreach (var item in fileContent)
                        {                           
                            file.Write(item);
                        }
                    }
                    return true;
                    //ApiReturn apiRet = (ApiReturn<string>)("/file/" + fileName);
                    //return apiRet;
                }
            });




            Logger.Info("[FileMng]init succeed!");

        }
        #endregion

    }
}
