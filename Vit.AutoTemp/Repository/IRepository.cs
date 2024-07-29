using System.Collections.Generic;

using Vit.Core.Util.ComponentModel.Data;
using Vit.Linq.ComponentModel;
using Vit.Linq.Filter.ComponentModel;


namespace Vit.AutoTemp.Repository
{
    public interface IRepository<T>
    {
        ApiReturn Delete(T m);

        ApiReturn<PageData<T>> GetList(FilterRule filter, IEnumerable<OrderField> sort, PageInfo page);

        ApiReturn<T> GetModel(string id);

        ApiReturn<T> Insert(T m);

        ApiReturn<T> Update(T m);
    }
}
