using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace netTrace
{
    public class TraceContext : IDisposable
    {
        private int _refCount = 1;
        private Guid _id = Guid.NewGuid();
        private static AsyncLocal<TraceContext> _current = new AsyncLocal<TraceContext>();
        private Action<TraceInfo> _finalizeCallback;

        /// <summary>
        ///     Output handler for all traces, once they are finalized.
        /// </summary>
        public static event Action<TraceInfo> FinalizeCallback;


        /// <summary>
        ///     Initializes a new TraceContext object, and ensures that only one
        ///     instance can be created at a time, per async context.
        /// </summary>
        public TraceContext(Action<TraceInfo> finalizeCallback = null)
        {
            if (_current.Value != null)
            {
                // Looks like someone else created a trace context (oops).
                _current.Value._refCount++;
                return;
            }

            _finalizeCallback = finalizeCallback;
            _current.Value = this;
            TraceInfoCache.InitializeTraceInfo(_id);
        }


        /// <summary>
        ///     Causes all trace info to be dumped to the output handler
        /// </summary>
        public void Dispose()
        {
            if (_current.Value == null || 
                --_current.Value._refCount > 0 ||
                !TraceInfoCache.TryFinalizeTraceInfo(_id, out TraceInfo info))
            {
                return;
            }

            _finalizeCallback?.Invoke(info);
            FinalizeCallback?.Invoke(info);

            _current.Value = null;
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
            if (_current.Value == null || !TraceInfoCache.TryGetTraceInfo(_current.Value._id, out TraceInfo info))
            {
                return;
            }

            info.Lines.Add(text);
            info.HasExceptionLogged |= isException;
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
