using System;

using Vit.AutoTemp.Repository;

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
