using Microsoft.AspNetCore.Mvc;

using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;

namespace App.Module.Sqler.Controllers.DataEditor
{
    [Route("sqler/DataEditor")]
    [ApiController]
    public class DataEditorController : ControllerBase
    {

        #region get templateList
        [HttpGet("templateList")]
        public ApiReturn templateList()
        {
            try
            {
                return new ApiReturn<List<string>> { data = Vit.AutoTemp.AutoTempHelp.dataProviderMap.Keys.ToList() };
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion
    }
}
