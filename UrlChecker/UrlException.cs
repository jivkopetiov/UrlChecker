using System;

namespace UrlChecker
{
    public class UrlException : Exception
    {
        public UrlException(string message)
            : base(message)
        {
        }
    }
}
