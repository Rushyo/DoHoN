using System;
using System.Linq;

namespace DoHoN
{
    internal class DNSCacheEntry
    {
        public readonly DateTime ExpireTime;
        public readonly DNSAnswer[] Answers;

        public DNSCacheEntry(DNSAnswer[] answers)
        {
            if(answers.Any())
                ExpireTime = DateTime.Now + new TimeSpan(0,0,answers.Min(a => a.TTL));
            else
                ExpireTime = DateTime.Now;
            
            Answers = answers;
        }
    }
}