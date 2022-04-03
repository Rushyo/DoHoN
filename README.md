# DNS over HTTPS on .NET

![Supported](https://img.shields.io/badge/supported-yes%20(2022)-brightgreen)
[![GitHub stars](https://img.shields.io/github/stars/Rushyo/DoHon.svg?style=social&label=Star&maxAge=2592000)](https://GitHub.com/Rushyo/DoHon/stargazers/)

DoHoN (*stylised ドホン*) is a [DNS over HTTPS](https://developers.cloudflare.com/1.1.1.1/dns-over-https/) client for .NET. It's simple, clean, fast, and supports both synchronous and asynchronous usage with caching (and respects TTLs!). It uses .NET Core 2.0 to provide cross-compatibility across multiple OSes. It uses the `application/dns-json` format.

Currently it allows the use of any DNS services, but defaults to trying Cloudflare ([1.1.1.1](https://developers.cloudflare.com/1.1.1.1/what-is-1.1.1.1/)) then Google ([8.8.8.8](https://developers.google.com/speed/public-dns/docs/using)). It has secure defaults, requiring DNSSEC verified responses and using consistent padded lengths to minimise snooping. It also requests services do not attempt to geolocate you by default, although you can flip the `RequestNoGeolocation` field to change this.

## Usage

Usage is simple:

```csharp
using (var client = new DoHClient())
{
    //Get example.com IPv4 record
    String ipv4 = client.LookupSync("example.com", ResourceRecordType.A).First().Data;
    ...
}
```

It throws `DNSLookupException` exceptions for common errors (like not being able to find a viable DNS server, or an invalid lookup).

You can set your own DNS over HTTPS servers like so:

```csharp
client.SetEndpoints(new[] {"https://example.com/resolve", "https://2.example.com/resolve"});
```

## Licensing

The source code is made available under the [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.en.html). It's like the GPL, except if you modify the source code and use that modified code on your server you need to make your derivative source code available to anyone using your network server. If your server is public, you need to make the derived code public. If you use the library without modification, you don't need to do anything. You are not contractual bound to release any source code that uses this code merely as a library. Consider this a linking exception if you prefer to think of it that way; nobody is going to snarf *your* source code, just make sure you give back any improvements to DoHoN back to the community.

## Bugs? Requests?

Drop an issue and I'll take a look when I have time.

## Why make this?

I was bored one evening and wanted to try out a new protocol. That's it, really. Enjoy!
