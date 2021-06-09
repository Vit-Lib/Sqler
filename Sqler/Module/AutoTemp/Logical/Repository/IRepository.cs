using System.Collections.Generic;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Linq.Query;

namespace App.Module.AutoTemp.Logical.Repository
{
    public interface IRepository<T>
    {
        ApiReturn Delete(T m);

        ApiReturn<PageData<T>> GetList(List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page);

        ApiReturn<T> GetModel(string id);

        ApiReturn<T> Insert(T m);

        ApiReturn<T> Update(T m);
    }
}
