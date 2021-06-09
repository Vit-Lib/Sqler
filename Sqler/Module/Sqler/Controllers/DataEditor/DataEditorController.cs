using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using System.Linq;
using App.Module.AutoTemp.Controllers;

namespace App.Module.Sqler.Controllers.DataEditor
{
    [Route("sqler/DataEditor")]
    [ApiController]
    public class DataEditorController : ControllerBase
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
                return new ApiReturn<List<string>> { data= AutoTempController.dataProviderMap.Keys.ToList() };
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion
    }
}
