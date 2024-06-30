using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json.Linq;

using Vit.AutoTemp.DataProvider;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Extensions;
using Vit.Extensions.Object_Serialize_Extensions;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace App.Module.AutoTemp.Controllers
{
    // apiRoute=/autoTemp/data/{template}/{action}
    [Route("autoTemp/data")]
    [ApiController]
    public class AutoTempController : ControllerBase
    {


        static AutoTempController()
        {
            #region init demo

            IDataProvider dataProvider;

            dataProvider = new Vit.AutoTemp.Demo.DemoDataProvider { isTree = true, template = "demo_tree" };
            Vit.AutoTemp.AutoTempHelp.RegistDataProvider(dataProvider);


            dataProvider = new Vit.AutoTemp.Demo.DemoDataProvider { isTree = false, template = "demo_list" };
            Vit.AutoTemp.AutoTempHelp.RegistDataProvider(dataProvider);


            dataProvider = new Vit.AutoTemp.Demo.DemoRepository().ToDataProvider("demo_repository_list");
            Vit.AutoTemp.AutoTempHelp.RegistDataProvider(dataProvider);

            #endregion

        }





        #region #1 getConfig
        /// <summary>
        /// GET autoTemp/getConfig
        /// </summary>
        /// <returns></returns>
        [HttpGet("{template}/getConfig")]
        public ApiReturn getConfig(string template)
        {
            try
            {
                var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.getControllerConfig(this);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion

        #region #2 getList
        /// <summary>
        /// GET autoTemp/getList
        /// </summary>
        /// <returns></returns>
        [HttpGet("{template}/getList")]
        public ApiReturn getList(string template, [FromQuery] string filter, [FromQuery] string sort, [FromQuery] string page, [FromQuery] string arg)
        {
            try
            {
                var page_ = page?.Deserialize<PageInfo>();
                var filter_ = filter.Deserialize<FilterRule>();
                var sort_ = sort.Deserialize<OrderField[]>();
                //{ isRoot: true,pid: 5}
                var arg_ = arg?.Deserialize<JObject>() ?? new JObject();

                var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.getList(this, filter_, sort_, page_, arg_);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion

        #region #3 getModel
        /// <summary>
        /// GET autoTemp/getModel
        /// </summary>
        /// <returns></returns>
        [HttpGet("{template}/getModel")]
        public ApiReturn getModel(string template, [FromQuery] string id)
        {
            try
            {
                var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.getModel(this, id);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion



        #region #4 insert
        /// <summary>
        /// POST autoTemp/insert
        /// </summary>
        /// <returns></returns>
        [HttpPost("{template}/insert")]
        public ApiReturn insert(string template, [FromBody] JObject model)
        {
            try
            {
                var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.insert(this, model);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion



        #region #5 update
        /// <summary>
        /// PUT autoTemp/update
        /// </summary>
        /// <returns></returns>
        [HttpPut("{template}/update")]
        public ApiReturn update(string template, [FromBody] JObject model)
        {
            try
            {
                var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.update(this, model);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion


        #region #6 delete
        /// <summary>
        /// DELETE autoTemp/delete
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{template}/delete")]
        public ApiReturn delete(string template, [FromBody] JObject arg)
        {
            try
            {
                var dataProvider = Vit.AutoTemp.AutoTempHelp.GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.delete(this, arg);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion 
    }
}
