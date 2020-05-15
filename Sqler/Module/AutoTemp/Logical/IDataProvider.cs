using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Linq.Query;

namespace Sqler.Module.AutoTemp.Logical
{
    public interface IDataProvider
    {
        string template { get; }
        ApiReturn delete(object sender,JObject arg);
        ApiReturn getControllerConfig(object sender);
        ApiReturn getList(object sender, List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page, JObject arg);
        ApiReturn getModel(object sender, string id);
        ApiReturn insert(object sender, JObject model);
        ApiReturn update(object sender, JObject model);
    }
}