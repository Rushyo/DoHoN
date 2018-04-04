using DoHoN;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoHoNTests
{
    //TODO: Non-functional unit tests and a testing DNS service

    [TestClass]
    public class FunctionalClientTests
    {
        private const String TestName = "example.com";
        private const String TestExpectedIPv4 = "93.184.216.34";
        private const String TestExpectedIPv6 = "2606:2800:220:1:248:1893:25c8:1946";

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync()
        {
            using (var client = new DoHClient())
            {
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.A).ToArray();
                Assert.IsTrue(results.Any(r => r.Data == TestExpectedIPv4));
                Assert.IsFalse(results.Any(r => r.TTL < 0));
                Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.A));
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupAsync()
        {
            using (var client = new DoHClient())
            {
                Task<IEnumerable<DNSAnswer>[]> resultsBag =
                    Task.WhenAll(new[] {1, 2, 3}.Select(i => client.LookupAsync(TestName, ResourceRecordType.A)));
                foreach (DNSAnswer[] results in resultsBag.Result.Select(r => r.ToArray()))
                {
                    Assert.IsTrue(results.Any(r => r.Data == TestExpectedIPv4));
                    Assert.IsFalse(results.Any(r => r.TTL < 0));
                    Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.A));
                }
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_RequireDNSSECC_Off()
        {
            using (var client = new DoHClient { RequireDNSSEC = false})
            {
                client.SetEndpoints(new[] {DoHClient.GoogleURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.A).ToArray();
                Assert.IsTrue(results.Any(r => r.Data == TestExpectedIPv4));
                Assert.IsFalse(results.Any(r => r.TTL < 0));
                Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.A));
            }
        }


        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_RequestNoGeolocation_Off()
        {
            using (var client = new DoHClient {RequestNoGeolocation = false})
            {
                client.SetEndpoints(new[] {DoHClient.GoogleURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.A).ToArray();
                Assert.IsTrue(results.Any(r => r.Data == TestExpectedIPv4));
                Assert.IsFalse(results.Any(r => r.TTL < 0));
                Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.A));
            }

        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_AAAA()
        {
            using (var client = new DoHClient())
            {
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.AAAA).ToArray();
                Assert.IsTrue(results.Any(r => r.RecordType == ResourceRecordType.AAAA && r.Data == TestExpectedIPv6));
                Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.AAAA));
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_ALL_Google()
        {
            using (var client = new DoHClient())
            {
                client.SetEndpoints(new[] {DoHClient.GoogleURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.ALL).ToArray();
                Assert.IsTrue(results.Any(r => r.RecordType == ResourceRecordType.A && r.Data == TestExpectedIPv4));
                Assert.IsTrue(results.Any(r => r.RecordType == ResourceRecordType.AAAA && r.Data == TestExpectedIPv6));
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_ALL_Cloudflare()
        {
            using (var client = new DoHClient())
            {
                client.SetEndpoints(new[] {DoHClient.CloudflareURI});
                try
                {
                    client.LookupSync(TestName, ResourceRecordType.ALL);
                }
                catch(AggregateException ex)
                {
                    Assert.IsTrue(ex.GetType() == typeof(AggregateException) && ex.InnerException.GetType() == typeof(DNSLookupException));
                    return;
                }
                catch(DNSLookupException ex)
                {
                    Assert.IsTrue(ex.GetType() == typeof(DNSLookupException));
                    return;
                }
                Assert.Fail("Expected exception to occur");
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_IllegalRR_Cloudflare()
        {
            using (var client = new DoHClient())
            {
                client.SetEndpoints(new[] {DoHClient.CloudflareURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, (ResourceRecordType) 999);
                Assert.IsFalse(results.Any());
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_IllegalRR_Google()
        {
            using (var client = new DoHClient())
            {
                client.SetEndpoints(new[] {DoHClient.GoogleURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, (ResourceRecordType) 999);
                Assert.IsFalse(results.Any());
            }
        }

        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_CloudFlare()
        {
            using (var client = new DoHClient())
            {
                client.SetEndpoints(new[] {DoHClient.CloudflareURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.A).ToArray();
                Assert.IsTrue(results.Any(r => r.RecordType == ResourceRecordType.A && r.Data == TestExpectedIPv4));
                Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.A));
            }
        }

        
        [TestMethod]
        [TestCategory("Non-Deterministic")]
        public void LookupSync_Google()
        {
            using (var client = new DoHClient())
            {
                client.SetEndpoints(new[] {DoHClient.GoogleURI});
                IEnumerable<DNSAnswer> results = client.LookupSync(TestName, ResourceRecordType.A).ToArray();
                Assert.IsTrue(results.Any(r => r.RecordType == ResourceRecordType.A && r.Data == TestExpectedIPv4));
                Assert.IsFalse(results.Any(r => r.RecordType != ResourceRecordType.A));
            }
        }
    }
}
