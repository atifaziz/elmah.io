using System;
using System.Threading.Tasks;

namespace Elmah.Io
{
    public interface IWebClientFactory
    {
        IWebClient Create();
    }

    static class WebClientFactoryExtensions
    {
        public static Task<T> Transact<T>(this IWebClientFactory factory, Func<IWebClient, Task<T>> func)
        {
            return factory.Transact(func, (_, r) => r);
        }

        public static Task<TResult> Transact<T, TResult>(this IWebClientFactory factory, Func<IWebClient, Task<T>> func, Func<IWebClient, T, TResult> selector)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (func == null) throw new ArgumentNullException("func");

            var wc = factory.Create();
            IDisposable disposable = wc;
            try
            {
                var task = func(wc).ContinueWith(t =>
                                    {
                                        TResult result;
                                        try
                                        {
                                            result = selector(wc, t.Result);
                                        }
                                        finally
                                        {
                                            wc.Dispose();
                                        }
                                        return result;
                                    },
                                    TaskContinuationOptions.ExecuteSynchronously);
                disposable = null;
                return task;
            }
            finally
            {
                if (disposable != null)
                    disposable.Dispose();
            }
        }
    }
}