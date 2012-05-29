using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Cache;

namespace UrlChecker
{
    public class Program
    {
        private const int _defaultTimeout = 10; // in seconds

        static void Main(string[] args)
        {
            args = new[] { "http://google.bg" };

            if (args == null || args.Length == 0)
            {
                PrintUsage();
                return;
            }

            try
            {
                Uri url;
                int timeout;
                ParseArguments(args, out url, out timeout);
                var results = CollectResults(url, timeout);
                PrintResults(results);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private static void ParseArguments(string[] args, out Uri url, out int timeout)
        {
            string urlString = args[0];
            if (!StartsWithValidUriScheme(urlString))
            {
                urlString = "http://" + urlString;
            }

            if (!Uri.TryCreate(urlString, UriKind.Absolute, out url))
                throw new UrlException("Url is not well formed: " + urlString);

            if (url.Scheme != "http")
                throw new UrlException(string.Format("Url scheme '{0}' is not supported", url.Scheme));

            timeout = _defaultTimeout;
            if (args.Length > 1)
            {
                int.TryParse(args[1], out timeout);
            }
        }

        private static UrlResult CollectResults(Uri url, int timeout)
        {
            Stopwatch stopwatch = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = timeout * 1000;
                request.ReadWriteTimeout = request.Timeout;
                request.UserAgent = "UrlChecker v1.0";
                request.AllowAutoRedirect = false;
                request.Method = "HEAD";

                var noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                request.CachePolicy = noCachePolicy;

                request.Headers[HttpRequestHeader.Pragma] = "no-cache";
                request.Headers[HttpRequestHeader.CacheControl] = "no-cache";

                stopwatch = Stopwatch.StartNew();
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    stopwatch.Stop();
                    return new UrlResult(url, response, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    if (ex.Status == WebExceptionStatus.NameResolutionFailure)
                        throw new UrlException("DNS failed to resolve the url: " + url);
                    else
                        throw;
                }

                var response = ex.Response as HttpWebResponse;

                if (response == null)
                    throw;

                stopwatch.Stop();
                return new UrlResult(url, response, stopwatch.ElapsedMilliseconds);
            }
        }

        private static void HandleError(Exception ex)
        {
            if (ex is UrlException)
            {
                PrintError(ex.Message);
            }
            else if (ex is WebException)
            {
                var webEx = (WebException)ex;

                var httpResponse = webEx.Response as HttpWebResponse;

                if (httpResponse == null)
                {
                    if (webEx.Status == WebExceptionStatus.Timeout)
                        PrintError("Timeout");
                    else
                        PrintError(ex.ToString());
                }
                else
                {
                    PrintError(httpResponse.StatusCode + " :: " + httpResponse.StatusDescription + " :: " + ex.Message);
                }
            }
            else
            {
                PrintError(ex.ToString());
            }
        }

        private static void PrintResults(UrlResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Elapsed: {0} milliseconds", result.Milliseconds);
            Console.WriteLine("Response url: " + result.ResponseUrl.AbsoluteUri);
            Console.WriteLine("Status: {0} {1}", (int)result.Status, result.Status);
            Console.WriteLine("Content Length: " + result.ContentLength);
            Console.WriteLine("Is From Cache: " + result.IsFromCache);
            Console.WriteLine();
            foreach (var key in result.Headers.AllKeys)
            {
                Console.WriteLine("{0}: {1}", key, result.Headers[key]);
            }
            Console.WriteLine();
        }

        private static void PrintError(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("urlchecker is a command-line utility that checks if a url is valid and what HTTP headers it returns.`");
            Console.WriteLine("Usage: urlchecker.exe <AbsoluteUrl> [Timeout]");
            Console.WriteLine("\t- <AbsoluteUrl> - required - the url to be checked");
            Console.WriteLine("\t- [Timeout] - optional - the amount of time (in seconds) to wait for the request to complete (not including DNS resolution)");
        }

        private static bool StartsWithValidUriScheme(string urlString)
        {
            return Regex.IsMatch(
                urlString,
                "(http|https|ftp|file|gopher|nntp|news|mailto|uuid|telnet|ldap|net.tcp|net.pipe|vsmacros)://",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
