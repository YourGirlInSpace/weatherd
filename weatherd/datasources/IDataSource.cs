using System.Threading.Tasks;

namespace weatherd.datasources
{
    public interface IDataSourceBase
    {
        /// <summary>
        ///     The internal name of the data source.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The polling interval of the data source in seconds.
        /// </summary>
        int PollingInterval { get; }

        bool Initialized { get; }
        bool Running { get; }
    }

    public interface IDataSource : IDataSourceBase
    {
        bool Initialize();
        bool Start();
        bool Stop();
    }

    public interface IAsyncDataSource : IDataSourceBase
    {
        Task<bool> Initialize();
        Task<bool> Start();
        Task<bool> Stop();
    }
}
