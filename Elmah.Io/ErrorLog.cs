using System;
using System.Collections;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Elmah.Io
{
    public class ErrorLog : Elmah.ErrorLog
    {
        private readonly string _logId;
        private Uri _url = new Uri("http://elmahio.azurewebsites.net/");
        private readonly IWebClientFactory _webClientFactory;

        public ErrorLog(IDictionary config) : this(config, new DotNetWebClientFactory())
        {
        }

        public ErrorLog(IDictionary config, IWebClientFactory webClientFactory)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (!config.Contains("LogId"))
            {
                throw new ApplicationException("Missing LogId. Please specify a LogId in your web.config like this: <errorLog type=\"Elmah.Io.ErrorLog, Elmah.Io\" LogId=\"98895825-2516-43DE-B514-FFB39EA89A65\" />");
            }

            Guid result;
            if (!Guid.TryParse(config["LogId"].ToString(), out result))
            {
                throw new ApplicationException("Invalid LogId. Please specify a valid LogId in your web.config like this: <errorLog type=\"Elmah.Io.ErrorLog, Elmah.Io\" LogId=\"98895825-2516-43DE-B514-FFB39EA89A65\" />");
            }

            _logId = result.ToString();

            if (config.Contains("Url"))
            {
                Uri uri;
                if (!Uri.TryCreate(config["Url"].ToString(), UriKind.Absolute, out uri))
                {
                    throw new ApplicationException("Invalid URL. Please specify a valid absolute url. In fact you don't even need to specify an url, which will make the error logger use the elmah.io backend.");
                }

                _url = new Uri(config["Url"].ToString());
            }

            _webClientFactory = webClientFactory;
        }

        public override IAsyncResult BeginLog(Error error, AsyncCallback asyncCallback, object asyncState)
        {
            var url = new Uri(_url, string.Format("api/logs?logId={0}", _logId));
            return _webClientFactory.Transact(webClient =>
            {
                webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                var xml = ErrorXml.EncodeString(error);
                return webClient.Post(url, "=" + HttpUtility.UrlEncode(xml));
            });
        }

        public override string EndLog(IAsyncResult asyncResult)
        {
            return EndImpl<string>(asyncResult);
        }

        static T EndImpl<T>(IAsyncResult asyncResult)
        {
            if (asyncResult == null) throw new ArgumentNullException("asyncResult");
            var task = asyncResult as Task<T>;
            if (task == null) throw new ArgumentException(null, "asyncResult");
            return task.Result;
        }

        public override string Log(Error error)
        {
            return EndLog(BeginLog(error, null, null));
        }

        public override IAsyncResult BeginGetError(string id, AsyncCallback asyncCallback, object asyncState)
        {
            var url = new Uri(_url, string.Format("api/logs?id={0}&logId={1}", id, _logId));
            return _webClientFactory.Transact(webClient => webClient.Get(url), (_, response) =>
            {
                dynamic d = JsonConvert.DeserializeObject(response);
                return (ErrorLogEntry) MapErrorLogEntry(d.Id, d.ErrorXml);
            });
        }

        public override ErrorLogEntry EndGetError(IAsyncResult asyncResult)
        {
            return EndImpl<ErrorLogEntry>(asyncResult);
        }

        public override ErrorLogEntry GetError(string id)
        {
            return EndGetError(BeginGetError(id, null, null));
        }

        public override IAsyncResult BeginGetErrors(int pageIndex, int pageSize, IList errorEntryList, AsyncCallback asyncCallback, object asyncState)
        {
            var url = new Uri(_url, string.Format("api/logs?logId={0}&pageindex={1}&pagesize={2}", _logId, pageIndex, pageSize));
            return _webClientFactory.Transact(wc => wc.Get(url), (_, response) =>
            {
                dynamic d = JsonConvert.DeserializeObject(response);
                foreach (var error in d)
                    errorEntryList.Add((ErrorLogEntry) MapErrorLogEntry(error.Id, error.ErrorXml));
                return errorEntryList.Count;
            });
        }

        public override int EndGetErrors(IAsyncResult asyncResult)
        {
            return EndImpl<int>(asyncResult);
        }

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            return EndGetErrors(BeginGetErrors(pageIndex, pageSize, errorEntryList, null, null));
        }

        private ErrorLogEntry MapErrorLogEntry(dynamic id, dynamic xml)
        {
            return new ErrorLogEntry(this, (string)id, ErrorXml.DecodeString((string)xml));
        }
    }
}