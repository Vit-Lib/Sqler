using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Linq.Query;
using Vit.Extensions;
using System;

namespace App.Module.AutoTemp.Logical.Repository
{
    public class RespositoryDataProvider<T>:IDataProvider
    {     

        public IRepository<T> respository { get; private set; }

        public RespositoryDataProvider(IRepository<T> respository, string template, Type entityType = null) 
        {
            this.template = template;
            this.respository = respository;

            controllerConfig = AutoTempHelp.BuildControllerConfigByType(entityType ?? typeof(T));
        }

        JObject controllerConfig;

        public string template { get;private set; }
        public ApiReturn getControllerConfig(object sender)
        {
            return new ApiReturn<JObject>(controllerConfig);
        }


        public ApiReturn delete(object sender, JObject arg)
        { 
            return respository.Delete(respository.GetModel(arg["id"].Value<string>()).data);
        }
      
        public ApiReturn getList(object sender, List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page, JObject arg)
        {
            return respository.GetList(filter, sort, page);
        }
        public ApiReturn getModel(object sender, string id)
        {           
            return respository.GetModel(id);           
        }
        public ApiReturn insert(object sender, JObject model)
        { 
            return respository.Insert(model.ConvertBySerialize<T>());
        }
        public ApiReturn update(object sender, JObject model)
        {  
            return respository.Update(model.ConvertBySerialize<T>());
        }
    }
}