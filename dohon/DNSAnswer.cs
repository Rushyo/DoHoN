using System;
using Newtonsoft.Json.Linq;

namespace DoHoN
{
    public class DNSAnswer
    {
        public String Name;
        public ResourceRecordType RecordType;
        public Int32 TTL;
        public String Data;

        internal static DNSAnswer FromJSON(JObject jsonAnswer)
        {
            String name = jsonAnswer["name"].ToString();
            var recordType = (ResourceRecordType)Convert.ToInt32(jsonAnswer["type"]);
            Int32 ttl = Convert.ToInt32(jsonAnswer["TTL"]);
            String data = jsonAnswer["data"].ToString();
            return new DNSAnswer {Name = name, RecordType = recordType, Data = data, TTL = ttl};
        }
    }
}
