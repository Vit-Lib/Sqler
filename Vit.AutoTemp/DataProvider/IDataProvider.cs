using Newtonsoft.Json.Linq;

using System.Collections.Generic;

using Vit.Core.Util.ComponentModel.Data;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;

namespace Vit.AutoTemp.DataProvider
{
    public interface IDataProvider
    {
        string template { get; }
        ApiReturn delete(object sender, JObject arg);
        ApiReturn getControllerConfig(object sender);
        ApiReturn getList(object sender, FilterRule filter, IEnumerable<OrderField> sort, PageInfo page, JObject arg);
        ApiReturn getModel(object sender, string id);
        ApiReturn insert(object sender, JObject model);
        ApiReturn update(object sender, JObject model);
    }
}