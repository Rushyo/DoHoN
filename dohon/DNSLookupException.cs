using System;
using System.Collections.Generic;
using System.Linq;

namespace DoHoN
{
    public class DNSLookupException : Exception
    {
        public DNSLookupException[] InnerExceptions;

        public DNSLookupException(String message) : base(message)
        {
        }

        
        public DNSLookupException(String message, Exception innerException) : base(message, innerException)
        {
        }

        public DNSLookupException(String message, IEnumerable<DNSLookupException> innerExceptions) : this(message)
        {
            InnerExceptions = innerExceptions.ToArray();
        }
    }
}
