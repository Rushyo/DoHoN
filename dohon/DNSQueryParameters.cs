using System;

namespace DoHoN
{
    internal class DNSQueryParameters : IEquatable<DNSQueryParameters>
    {
        private readonly String _name;
        private readonly ResourceRecordType _recordType;

        public DNSQueryParameters(String name, ResourceRecordType recordType)
        {
            _recordType = recordType;
            _name = name;
        }

        public override Int32 GetHashCode()
        {
            unchecked
            {
                return ((_name != null ? _name.GetHashCode() : 0) * 397) ^ (Int32) _recordType;
            }
        }


        public Boolean Equals(DNSQueryParameters other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return String.Equals(_name, other._name) && _recordType == other._recordType;
        }

        public override Boolean Equals(Object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((DNSQueryParameters) obj);
        }
    }
}