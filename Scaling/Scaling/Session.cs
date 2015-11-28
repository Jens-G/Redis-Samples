using Scaling.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scaling
{
    internal class Session
    {
        private static uint LOCK_TTL_MSEC = 15 * 1000;

        private static DateTime lockStart;


        private static bool LockSession(IDatabase db, string sessionID, uint msec, out string lockId)
        {
            RedisKey keyLock = Tools.MakeLockKey(sessionID);

            lockId = Guid.NewGuid().ToString();

            var trans = db.CreateTransaction();
            trans.AddCondition(Condition.KeyNotExists(keyLock));
            trans.StringSetAsync(keyLock, lockId, TimeSpan.FromMilliseconds(msec));
            if( trans.Execute())
            {
                lockStart = DateTime.Now;
                //Console.WriteLine(" Aquired {0} on key {1}", lockId, keyLock);
                return true;
            }

            lockId = String.Empty;
            return false;
        }


        private static bool LockedAction(IDatabase db, string sessionID, string lockId, Action<ITransaction> action)
        {
            RedisKey keyLock = Tools.MakeLockKey(sessionID);
            Debug.Assert(!String.IsNullOrEmpty(lockId));

            var trans = db.CreateTransaction();
            trans.AddCondition(Condition.StringEqual(keyLock, lockId));
            action(trans);
            if (trans.Execute())
                return true;

            // failed 
            var delta = (DateTime.Now - lockStart).TotalMilliseconds;
            Console.WriteLine("Lock {0} on key {1} held for {2} msec expired unexpectedly", lockId, sessionID, delta);
            return false;
        }


        private static bool LockExtend(IDatabase db, string sessionID, string lockId, uint msec)
        {
            RedisKey keyLock = Tools.MakeLockKey(sessionID);
            
            return LockedAction(db, sessionID, lockId, (trans) =>
            {
                trans.KeyExpireAsync(keyLock, TimeSpan.FromMilliseconds(msec));
            });
        }


        private static void UnLockSession(IDatabase db, string sessionID, string lockId)
        {
            RedisKey keyLock = Tools.MakeLockKey(sessionID);

            LockedAction(db, sessionID, lockId, (trans) =>
            {
                trans.KeyDeleteAsync(keyLock);
            });
        }



        internal static RedisValue[] SessionIds(ConnectionMultiplexer Redis)
        {
            return Redis.GetDatabase().HashKeys(Tools.SESSIONLIST);
        }


        internal static InstanceState LockAndRead(ConnectionMultiplexer Redis, InstanceDescriptor instanceDescriptor, out string lockId)
        {
            return Read(Redis, instanceDescriptor, true, out lockId);
        }


        internal static InstanceState ReadOnly(ConnectionMultiplexer Redis, InstanceDescriptor instanceDescriptor)
        {
            string lockId;
            return Read(Redis, instanceDescriptor, false, out lockId);
        }


        private static InstanceState Read(ConnectionMultiplexer Redis, InstanceDescriptor instanceDescriptor, bool forChange, out string lockId)
        {
            var db = Redis.GetDatabase();
            RedisKey keyLock = Tools.MakeLockKey(instanceDescriptor.Id);
            RedisKey keyData = Tools.MakeDataKey(instanceDescriptor.Id);

            lockId = String.Empty;
            if (forChange)
            {
                while (!LockSession(db, instanceDescriptor.Id, LOCK_TTL_MSEC, out lockId))
                {
                    Console.Write(instanceDescriptor.Id);  // waiting for this lock
                    Thread.Sleep(0);
                }
            }

            if (db.KeyExists(keyData))
            {
                RedisValue data = db.StringGet(keyData);
                return Tools.DeSerialize<InstanceState>((byte[])data);
            }
            else
            {
                return new InstanceState()
                {
                    Id = instanceDescriptor.Id,
                    LastResult = 0,
                    Revision = 0
                };
            }
        }


        internal static void WriteAndUnlock(ConnectionMultiplexer Redis, InstanceState state, string lockId)
        {
            var db = Redis.GetDatabase();
            RedisKey keyLock = Tools.MakeLockKey(state.Id);
            RedisKey keyData = Tools.MakeDataKey(state.Id);

            var result = LockedAction(db, state.Id, lockId, (trans) =>
            {
                trans.StringSetAsync(keyData, Tools.Serialize<InstanceState>(state));
                trans.KeyDeleteAsync(keyLock);
            });

            if (result)
            {
                db.HashSet(Tools.SESSIONLIST, state.Id, "1");
                return;
            }
        }
    }
}
