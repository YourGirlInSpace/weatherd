using System.Threading.Tasks;
using Amazon.TimestreamWrite.Model;

namespace weatherd.io
{
    public interface ITimestreamClient
    {
        void Connect();
        
        Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request);
        Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request);
        Task<WriteRecordsResponse> WriteRecordsAsync(WriteRecordsRequest request);
    }
}
