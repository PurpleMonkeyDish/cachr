# Cachr
Cachr is a project for distributed memory caching. It is an implementation of IDistributedCache, that uses a memory cache and push notifications to other nodes.

Cachr was born out of a frustration with the performance of redis under extreme loads. By utilizing spare resources in applications instead, we can get the performance of local caching, but allow the cache to be shared across nodes.

Cachr was built with performance in mind, making heavy use of pooled arrays to prevent excessive GC pressure on the host, and a preload mechanism to prevent cold starts when deploying (The preload mechanism will request a list of keys from other hosts, and then query those keys from random hosts, building up the local cache)


## Peering
The current peer discovery code is a WIP, but the intention is to allow various methods of finding peers, and utilizing them. The first communication bus, will use ASP.NET core middleware to intercept messages intended for cachr. In the future, a raw binary protocol will be introduced.



## Build Results

[![build](https://github.com/jasoncouture/cachr/actions/workflows/build.yml/badge.svg?event=push)](https://github.com/jasoncouture/cachr/actions/workflows/build.yml)
[![test](https://github.com/jasoncouture/cachr/actions/workflows/test.yml/badge.svg?event=push)](https://github.com/jasoncouture/cachr/actions/workflows/test.yml)
[![benchmark](https://github.com/jasoncouture/cachr/actions/workflows/benchmark.yml/badge.svg?event=push)](https://github.com/jasoncouture/cachr/actions/workflows/benchmark.yml)



[![codeql-analysis](https://github.com/jasoncouture/cachr/actions/workflows/codeql-analysis.yml/badge.svg?event=push)](https://github.com/jasoncouture/cachr/actions/workflows/codeql-analysis.yml)
[![test-report](https://github.com/jasoncouture/cachr/actions/workflows/test-report.yml/badge.svg?event=push)](https://github.com/jasoncouture/cachr/actions/workflows/test-report.yml)



