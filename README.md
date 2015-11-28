# Redis-Samples

Sample code showcasing use cases for the fabulous [Redis](http://redis.io) software, using tha similarly amazing [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) library for C#. 

## Contents

The code samples contain two projects. The [first one](./Features) showcases various Redis features in a more or less isloated manner. The [second project](./Scaling) demonstrates some more features at work in a complex, scalable, cluster-aware application. 

## Environment / contributions

The code is C# focusing on Windows and has not yet been tested with Mono. If you want to use it on Mono, feel free to share your experiences and/or contribute improvements. Highly appreciated! 

## Build dependencies

- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
- [Apache Thrift](https://thrift.apache.org)
 
## Runtime dependencies 

- [Redis](http://redis.io) up and running. For the cluster stuff, you will need Redis 3.0 or better. Anything else should work as well with the latest Redis 2.x version available on your platform.
- some data in CSV format for some of the feature samples (see there for details)

