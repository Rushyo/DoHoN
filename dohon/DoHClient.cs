using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.String;

namespace DoHoN
{
    public class DoHClient : IDisposable
    {
        public const String CloudflareURI = "https://cloudflare-dns.com/dns-query";
        public const String GoogleURI = "https://dns.google.com/resolve";
        public Boolean UseRandomPadding = true;
        public Boolean RequireDNSSEC = true;
        public Boolean RequestNoGeolocation = true;
        private const String JsonContentType = "application/dns-json";
        private readonly Random _random;
        private readonly HttpClient _client;
        private readonly ConcurrentDictionary<DNSQueryParameters, DNSCacheEntry> _answersCache = new ConcurrentDictionary<DNSQueryParameters, DNSCacheEntry>();

        private static readonly Dictionary<Int32, String> DNSCodes = new Dictionary<Int32, String>
        {
            {1, "Format Error"},
            {2, "Server Failure"},
            { 3, "Non-Existent Domain" },
            { 4, "Not Implemented" },
            { 5, "Query Refused" },
            { 6, "Name Exists when it should not" },
            { 7, "RR Set Exists when it should not" },
            { 8, "RR Set that should exist does not" },
            { 9, "Server Not Authoritative for zone" },
            { 9, "Not Authorized" },
            { 10, "Name not contained in zone" },
            { 16, "Bad OPT Version / TSIG Signature Failure" },
            { 17, "Key not recognized" },
            { 18, "Signature out of time window" },
            { 19, "Bad TKEY Mode" },
            { 20, "Duplicate key name" },
            { 21, "Algorithm not supported" },
            { 22, "Bad Truncation" },
            { 23, "Bad/missing Server Cookie" }
        };
        private String[] _endpointList = 
        {
            CloudflareURI, GoogleURI
        };

        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        public void SetEndpoints(String[] serverList)
        {
            if(serverList.Any(s => !s.StartsWith("https://")))
                throw new ArgumentException("Server URI not https", nameof(serverList));
            _endpointList = serverList.ToArray();
        }

        public DoHClient()
        {
            _random = GenerateCryptoSeededRandom();
            _client = new HttpClient();
        }

        private static Random GenerateCryptoSeededRandom()
        {
            var seed = new Byte[4];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(seed);
            var random = new Random(BitConverter.ToInt32(seed, 0));
            return random;
        }

        public void ClearCache()
        {
            _answersCache.Clear();
        }


        public IEnumerable<DNSAnswer> LookupSync(String name, ResourceRecordType recordType)
        {
            return LookupAsync(name, recordType).Result;
        }

        public async Task<IEnumerable<DNSAnswer>> LookupAsync(String name, ResourceRecordType recordType)
        {
            var queryParams = new DNSQueryParameters(name, recordType);
            if (_answersCache.ContainsKey(queryParams))
            {
                DNSCacheEntry hit = _answersCache[queryParams];
                if (hit.ExpireTime <= DateTime.Now)
                    _answersCache.TryRemove(queryParams, out DNSCacheEntry entry);
                else
                    return hit.Answers;
            }

            var storedExceptions = new List<DNSLookupException>();
            foreach (String endpoint in _endpointList)
            {
                try
                {
                    Task<IEnumerable<DNSAnswer>> lookupTask = SingleLookup(name, recordType, endpoint);
                    DNSAnswer[] answers = (await lookupTask).ToArray();
                    if(answers.Any())
                        _answersCache.TryAdd(queryParams, new DNSCacheEntry(answers));
                    return answers;
                }
                catch (DNSLookupException ex)
                {
                    storedExceptions.Add(ex);
                }
            }
            throw new DNSLookupException("Unable to perform DNS lookup due to lookup errors", storedExceptions);
        }

        private async Task<IEnumerable<DNSAnswer>> SingleLookup(String name, ResourceRecordType recordType, String serverURI)
        {
            String uri = GenerateQuery(name, serverURI, recordType);

            HttpResponseMessage response = await _client.GetAsync(uri);
            if(!response.IsSuccessStatusCode)
                throw new DNSLookupException($"Error contacting DNS server (HTTP {response.StatusCode} {response.ReasonPhrase})");

            String content = await response.Content.ReadAsStringAsync();
            return HandleJSONResponse(content, RequireDNSSEC);
        }

        private static IEnumerable<DNSAnswer> HandleJSONResponse(String content, Boolean requireVerified)
        {
            JObject json;
            try
            {
                json = JObject.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                throw new DNSLookupException("Unable to parse JSON when retrieving DNS entry", ex);
            }


            String comment = null;
            if (json.ContainsKey("Comment"))
                comment = json["Comment"].ToString();

            Int32 status = Convert.ToInt32(json["Status"]);
            if (status != 0)
                HandleDNSError(status, comment);

            Boolean? truncatedBit  = null;
            if(json.ContainsKey("TC"))
                truncatedBit = Convert.ToBoolean(json["TC"]);

            Boolean? recursiveDesiredBit = null;
            if(json.ContainsKey("RD"))
                recursiveDesiredBit = Convert.ToBoolean(json["RD"]);

            Boolean? recursionAvailableBit = null;
            if(json.ContainsKey("RA"))
                recursionAvailableBit = Convert.ToBoolean(json["RA"]);

            Boolean? verifiedAnswers = null;
            if (json.ContainsKey("AD"))
                verifiedAnswers = Convert.ToBoolean(json["AD"]);
            if(requireVerified && (!verifiedAnswers.HasValue || !verifiedAnswers.Value))
                throw new DNSLookupException("DNS lookup could not be verified as using DNSSEC but DNSSEC was required");

            var questions = (JArray) json["Question"];

            var answers = (JArray) json["Answer"];
            return answers?.OfType<JObject>().Select(DNSAnswer.FromJSON) ?? new DNSAnswer[] {};
        }

        private String GenerateQuery(String name, String serverURI, ResourceRecordType queryType)
        {
            var fields = new Dictionary<String, String>()
            {
                {"name", name},
                {"type", queryType.ToString()},
                {"ct", JsonContentType},
                {"cd", RequireDNSSEC ? "false" : "true"},
            };

            if (RequestNoGeolocation)
                fields.Add("edns_client_subnet", "0.0.0.0/0");

            const Int32 padtoLength = 250;

            String uri = $"{serverURI}?{Join("&", fields.Select(f => f.Key + "=" + f.Value))}";
            if (uri.Length-16 < padtoLength && UseRandomPadding)
                uri += $"&random_padding={GeneratePadding(padtoLength-uri.Length-16)}";
            return uri;
        }

        private static void HandleDNSError(Int32 statusCode, String comment)
        {
            String commentText = comment != null ? $"{Environment.NewLine}Server Comment: ({comment})" : "";
            if(DNSCodes.ContainsKey(statusCode))
                throw new InvalidOperationException($"Received DNS RCode {statusCode} when performing lookup: {DNSCodes[statusCode]}{commentText}");

            throw new InvalidOperationException($"Received DNS RCode {statusCode} when performing lookup{commentText}");
        }

        private String GeneratePadding(Int32 paddingLength)
        {
            const String paddingChars = "abcddefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVXYZ012456789-._~";
            var randomPadding = new StringBuilder();
            for (var i = 0; i < paddingLength; i++)
                randomPadding.Append(paddingChars[_random.Next(paddingChars.Length)]);
            return randomPadding.ToString();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
