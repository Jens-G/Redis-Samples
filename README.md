# Redis-Samples

Sample code showcasing use cases for the fabulous [Redis](http://redis.io) software, using tha similarly amazing [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) library for C#. 

## Contents

The code samples contain two projects. The [first one](./Features) showcases various Redis features in a more or less isloated manner. The [second project](./Scaling) demonstrates some more features at work in a complex, scalable, cluster-aware application. The [third project](./Streams) showcases the new [Redis Streams](https://redis.io/topics/streams-intro) feature.

## Environment / contributions

The code is C# focusing on Windows and has not yet been tested with Mono. If you want to use it on Mono, feel free to share your experiences and/or contribute improvements. Highly appreciated! 

## Build dependencies

- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
- [Apache Thrift](https://thrift.apache.org)
 
## Runtime dependencies 

- [Redis](http://redis.io) up and running. Streams require Redis 5.0 or better. 
- some data in CSV format for some of the feature samples (see there for details)

Pro Tip: Windows users may consider running Redis in a Docker cntainer, that's the easiest way to set it up quickly.


