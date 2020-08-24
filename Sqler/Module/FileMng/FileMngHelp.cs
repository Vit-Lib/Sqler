using Sqler.Module.FileMng.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vit.Core.Module.Log;
using Vit.Extensions;

namespace Sqler.Module.FileMng
{
    public class FileMngHelp
    {

        #region InitAutoTemp
        public static void InitAutoTemp()
        {
            Logger.Info("[FileMng]init ...");



            AutoTemp.Controllers.AutoTempController.RegistDataProvider(
                           new FileMngRepository().ToDataProvider("FileMng_FileMng"));         


            Logger.Info("[FileMng]init succeed!");

        }
        #endregion

    }
}
