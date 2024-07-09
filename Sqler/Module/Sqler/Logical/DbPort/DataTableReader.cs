using System.Data;

namespace Sqler.Module.Sqler.Logical.DbPort
{
    public class DataTableReader : IDisposable
    {
        public Action OnDispose = null;
        public void Dispose()
        {
            OnDispose?.Invoke();
        }
        public Func<IDataReader> GetDataReader = null;
        public Func<DataTable> GetDataTable = null;

    }
}
