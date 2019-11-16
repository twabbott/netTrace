using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NetTrace
{
    internal class TraceInfoCache
    {
        private static ConcurrentDictionary<Guid, TraceInfo> _traceStore = new ConcurrentDictionary<Guid, TraceInfo>();

        public static void InitializeTraceInfo(Guid value)
        {
            _traceStore.TryAdd(value, new TraceInfo());
        }

        public static bool TryFinalizeTraceInfo(Guid key, out TraceInfo value)
        {
            return _traceStore.TryRemove(key, out value);
        }

        public static bool TryGetTraceInfo(Guid key, out TraceInfo value)
        {
            return _traceStore.TryGetValue(key, out value);
        }
    }
}
