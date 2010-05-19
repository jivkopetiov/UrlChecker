using System;
using System.Net;

namespace UrlChecker
{
    public class UrlResult
    {
        public UrlResult(Uri originalUrl, HttpWebResponse response, long elapsedMilliseconds)
        {
            OriginalUrl = originalUrl;
            ResponseUrl = response.ResponseUri;
            Headers = response.Headers;
            Status = response.StatusCode;
            Milliseconds = elapsedMilliseconds;
            ContentLength = response.ContentLength;
            Server = response.Server;
        }

        public long ContentLength { get; private set; }

        public Uri OriginalUrl { get; private set; }

        public Uri ResponseUrl { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public HttpStatusCode Status { get; private set; }

        public string Server { get; private set; }

        public long Milliseconds { get; private set; }
    }
}
