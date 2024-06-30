using Vit.Db.Module.Schema;

namespace Vit.AutoTemp.DataProvider
{
    public interface IDataProvider_Vitorm : IDataProvider
    {
        TableSchema tableSchema { get; }
        void Init();
    }
}