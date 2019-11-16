using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;

namespace NetTrace
{
    public sealed class TraceContext : IDisposable
    {
        #region private

        private List<TraceContext> _instances = new List<TraceContext>();
        private Guid _id = Guid.NewGuid();
        private static AsyncLocal<TraceContext> _current = new AsyncLocal<TraceContext>();
        private Action<TraceInfo> _finalizeCallback;

        private TraceContext(Action<TraceInfo> finalizeCallback)
        {
            if (_current.Value == null)
            {
                _current.Value = this;
            }
            _current.Value._instances.Add(this);

            _finalizeCallback = finalizeCallback;
            TraceInfoCache.InitializeTraceInfo(_id);
        }


        /// <summary>
        ///     Called by the Trace class to log a single line of text.  This 
        ///     method finds the current trace context (if one exists), and logs
        ///     to it.
        /// </summary>
        /// 
        /// <param name="text">
        ///     Text that the caller wants to log.
        /// </param>
        private static void Log(string text, bool isException = false)
        {
            if (_current.Value == null)
            {
                return;
            }

            // Broadcast to all listeners in the call stack
            _current.Value._instances.ForEach(context =>
            {
                if (TraceInfoCache.TryGetTraceInfo(context._id, out TraceInfo info))
                {
                    info.Lines.Add(text);
                    info.HasExceptionLogged |= isException;
                }
            });
        }

        #endregion


        /// <summary>
        ///     Output handler for all traces, once they are finalized.
        /// </summary>
        public static event Action<TraceInfo> FinalizeCallback;


        /// <summary>
        ///     Causes all trace info to be dumped to the output handler
        /// </summary>
        public void Dispose()
        {
            if (_current.Value == null || _current.Value._instances[_current.Value._instances.Count - 1] != this)
            {
                return;
            }

            // Get our TraceInfo object from the cache
            TraceInfoCache.TryFinalizeTraceInfo(_id, out TraceInfo info);

            // Remove ourselves from the call stack
            _current.Value._instances.RemoveAt(_current.Value._instances.Count - 1);
            if (_current.Value._instances.Count == 0)
            {
                _current.Value = null;
            }

            // Log to all trace listeners
            _finalizeCallback?.Invoke(info);
            FinalizeCallback?.Invoke(info);
        }


        /// <summary>
        ///     Initializes a new TraceContext object, and ensures that only one
        ///     instance can be created at a time, per async context.
        /// </summary>
        public static TraceContext Begin(Action<TraceInfo> finalizeCallback = null)
        {
            return new TraceContext(finalizeCallback);
        }


        /// <summary>
        ///     Logs a line of text
        /// </summary>
        /// <param name="message"></param>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        public static void WriteLine(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            DateTime now = DateTime.Now;
            string fileName = Path.GetFileName(sourceFilePath);

            string line = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId:000}] {fileName}({sourceLineNumber}) {memberName} - {message}";
            TraceContext.Log(line);
        }


        /// <summary>
        ///     Logs an exception that you caught.
        /// </summary>
        /// 
        /// <param name="ex">An Exception object that you want to log.</param>
        /// <param name="message">An optional message.</param>
        public static void LogException(Exception ex, string message = null)
        {
            DateTime now = DateTime.Now;

            if (message != null)
            {
                string line = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId:000}] EXCEPTION - {message}";
                TraceContext.Log(line);
            }

            TraceContext.Log(ex.ToString(), true);
        }
    }
}
