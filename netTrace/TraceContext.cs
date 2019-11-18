using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;

namespace NetTrace
{
    /// <summary>
    ///     A class for aggregating debug trace output of a single thread (or 
    ///     threading context) into one collection, so that the info gathered 
    ///     can be logged in one write operation.
    /// </summary>
    public sealed class TraceContext : IDisposable
    {
        #region private

        private static AsyncLocal<TraceInfo> _current = new AsyncLocal<TraceInfo>();
        private Action<TraceInfo> _finalizeCallback;

        private TraceContext(Action<TraceInfo> finalizeCallback)
        {
            // Make a new instance, and push any exsisting instance onto the 
            // stack.
            _current.Value = new TraceInfo(_current.Value);

            _finalizeCallback = finalizeCallback;
        }

        #endregion


        /// <summary>
        ///     Output handler for all traces, once they are finalized.
        /// </summary>
        public static event Action<TraceInfo> FinalizeCallback;


        /// <summary>
        ///     Flushes all trace info to the output handler
        /// </summary>
        public void Dispose()
        {
            // Pop our info off the stack
            TraceInfo info = _current.Value;
            _current.Value = info.Previous;
            info.Previous = null;

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
            if (_current.Value == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            string fileName = Path.GetFileName(sourceFilePath);

            string line = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId:000}] {fileName}({sourceLineNumber}) {memberName} - {message}";
            _current.Value.Log(line, false);
        }


        /// <summary>
        ///     Logs an exception that you caught.
        /// </summary>
        /// 
        /// <param name="ex">An Exception object that you want to log.</param>
        /// <param name="message">An optional message.</param>
        public static void LogException(Exception ex, string message = null)
        {
            if (_current.Value == null)
            {
                return;
            }

            DateTime now = DateTime.Now;

            if (message != null)
            {
                string line = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId:000}] EXCEPTION - {message}";
                _current.Value.Log(line, false);
            }

            _current.Value.Log(ex.ToString(), true);
        }
    }
}
