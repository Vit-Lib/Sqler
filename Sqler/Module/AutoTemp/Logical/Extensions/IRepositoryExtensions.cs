using Sqler.Module.AutoTemp.Logical.Repository;
using System;

namespace Vit.Extensions
{
    public static partial class IRepositoryExtensions
    {

        #region ToDataProvider
        public static RespositoryDataProvider<T> ToDataProvider<T>(this IRepository<T> data, string template, Type entityType = null)
        {
            return new RespositoryDataProvider<T>(data, template, entityType) ;            
        }
        #endregion

 



    }
}
