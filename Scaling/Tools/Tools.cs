using Scaling.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;

namespace Scaling
{
    internal class Tools
    {
        internal static RedisKey WORKQUEUE = "ClusterTest.Work";
        internal static RedisKey SESSIONLIST = "ClusterTest.Sessions";


        internal static RedisKey MakeLockKey(string sessionId)
        {
            return String.Format("{{ClusterTest:Session:{0}}}:Lock", sessionId);
        }


        internal static RedisKey MakeDataKey(string sessionId)
        {
            return String.Format("{{ClusterTest:Session:{0}}}:Data", sessionId);
        }


        internal static byte[] Serialize<T>(T thriftStruct)
            where T : TBase
        {
            

            var trans = new TMemoryBuffer();
            var prot = new TJSONProtocol(trans);
            thriftStruct.Write(prot);
            return trans.GetBuffer();
        }


        internal static T DeSerialize<T>(byte[] data)
            where T : TBase
        {
            var trans = new TMemoryBuffer(data);
            var prot = new TJSONProtocol(trans);
            var result = Activator.CreateInstance<T>();
            result.Read(prot);
            return result;
        }

    }
}
