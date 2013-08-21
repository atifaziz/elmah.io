using System;
using System.Net;
using System.Threading.Tasks;

namespace Elmah.Io
{
    public interface IWebClient : IDisposable
    {
        WebHeaderCollection Headers { get; set; }

        Task<string> Post(Uri address, string data);

        Task<string> Get(Uri address);
    }
}