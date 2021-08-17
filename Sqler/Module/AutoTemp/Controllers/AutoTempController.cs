using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Linq.Query;
using Vit.Extensions;
using Vit.Core.Util.ComponentModel.SsError;
using App.Module.AutoTemp.Logical;
using App.Module.AutoTemp.Demo;

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

            dataProvider = new Demo.DemoDataProvider { isTree = true, template = "demo_tree" };
            RegistDataProvider(dataProvider);


            dataProvider = new Demo.DemoDataProvider { isTree = false, template = "demo_list" };
            RegistDataProvider(dataProvider);


 
            dataProvider = new  DemoRepository().ToDataProvider("demo_repository_list");
            RegistDataProvider(dataProvider);

            #endregion

        }


        #region static GetDataProvider
        public static readonly SortedDictionary<string, IDataProvider> dataProviderMap = new SortedDictionary<string, IDataProvider>();

        public static void RegistDataProvider(params IDataProvider[] dataProviders)
        {
            lock (dataProviderMap)
            {
                foreach (var dataProvider in dataProviders)
                {
                    dataProviderMap[dataProvider.template] = dataProvider;
                }
            }
        }


        public static void UnRegistDataProvider(params IDataProvider[] dataProviders)
        {
            lock (dataProviderMap)
                foreach (var dataProvider in dataProviders)
                {
                    dataProviderMap.Remove(dataProvider.template);
                }
        }
        public static IDataProvider GetDataProvider(string template)
        {
            return dataProviderMap.TryGetValue(template, out var v) ? v : null;
        }

        #endregion


        #region (x.1) getConfig
        /// <summary>
        /// GET autoTemp/getConfig
        /// </summary>
        /// <returns></returns>
        [HttpGet("{template}/getConfig")]
        public ApiReturn getConfig(string template)
        {
            try
            {
                var dataProvider = GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.getControllerConfig(this);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }

        #endregion

        #region (x.2) getList
        /// <summary>
        /// GET autoTemp/getList
        /// </summary>
        /// <returns></returns>
        [HttpGet("{template}/getList")]
        public ApiReturn getList(string template,  [FromQuery]string filter, [FromQuery]string sort, [FromQuery]string page, [FromQuery]string arg)
        {
            try
            {
                var page_ = page.Deserialize<PageInfo>();
                var filter_ = filter.Deserialize<List<DataFilter>>() ?? new List<DataFilter>();
                var sort_ = sort.Deserialize<SortItem[]>();
                //{ isRoot: true,pid: 5}
                var arg_ = arg.Deserialize<JObject>() ?? new JObject();

                var dataProvider = GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.getList(this, filter_, sort_, page_, arg_);
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
        [HttpGet("{template}/getModel")]
        public ApiReturn getModel(string template, [FromQuery]string id)
        {
            try
            {
                var dataProvider = GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.getModel(this, id);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion



        #region (x.4) insert
        /// <summary>
        /// POST autoTemp/insert
        /// </summary>
        /// <returns></returns>
        [HttpPost("{template}/insert")]
        public ApiReturn insert(string template, [FromBody]JObject model)
        {
            try
            {
                var dataProvider = GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.insert(this, model);
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
        [HttpPut("{template}/update")]
        public ApiReturn update(string template, [FromBody]JObject model)
        {
            try
            {
                var dataProvider = GetDataProvider(template);
                if (dataProvider == null) return new SsError { errorMessage = "模板不存在" };

                return dataProvider.update(this, model);
            }
            catch (System.Exception ex)
            {
                return (SsError)ex.GetBaseException();
            }
        }
        #endregion


        #region (x.6) delete
        /// <summary>
        /// DELETE autoTemp/delete
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{template}/delete")]
        public ApiReturn delete(string template, [FromBody]JObject arg)
        {
            try
            {
                var dataProvider = GetDataProvider(template);
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
