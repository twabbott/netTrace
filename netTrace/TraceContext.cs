using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NetTrace
{
    /// <summary>
    ///     A class for aggregating debug trace output of a single thread (or 
    ///     threading context) into one collection, so that the info gathered 
    ///     can be logged in one write operation.
    /// </summary>
    public class TraceContext : IDisposable
    {
        #region private

        private static AsyncLocal<TraceInfo> _current = new AsyncLocal<TraceInfo>();
        private Action<TraceInfo> _finalizeCallback;

        #endregion

        /// <summary>
        ///     Creates a new TraceContext
        /// </summary>
        /// 
        /// <param name="finalizeCallback">
        ///     An optional callback method to handle the trace info, once the 
        ///     trace is complete.
        /// </param>
        public TraceContext(Action<TraceInfo> finalizeCallback = null)
        {
            // Make a new instance, and push any exsisting instance onto the 
            // stack.
            _current.Value = new TraceInfo(_current.Value);

            _finalizeCallback = finalizeCallback;
        }


        /// <summary>
        ///     Flushes all trace info to the output handler
        /// </summary>
        public void Dispose()
        {
            // Pop our info off the stack
            TraceInfo info = _current.Value;
            _current.Value = info.Previous;
            info.Previous = null;

            _finalizeCallback?.Invoke(info);
            OnFinalize(info);
        }


        /// <summary>
        ///     Override this method to provide a general handler for all 
        ///     traces.  Do not call this method.  This method is called by IDispose.
        /// </summary>
        protected virtual void OnFinalize(TraceInfo info)
        {
            // Do nothing
        }


        /// <summary>
        ///     Logs an event
        /// </summary>
        /// 
        /// <param name="message">
        ///     A message that you want to log
        /// </param>
        /// <param name="exception">
        ///     An optional exception that you want to log
        /// </param>
        /// <param name="memberName">
        ///     Parameter value supplied for you by .NET
        /// </param>
        /// <param name="sourceFilePath">
        ///     Parameter value supplied for you by .NET
        /// </param>
        /// <param name="sourceLineNumber">
        ///     Parameter value supplied for you by .NET
        /// </param>
        public static void Log(
            string message,
            Exception exception = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (_current.Value == null)
            {
                return;
            }

            var stackTrace = new StackTrace(0, true);
            var frame = stackTrace.GetFrame(1);

            var method = frame.GetMethod();
            string className = method.ReflectedType?.DeclaringType?.Name;
            className = className == null ? "<no-class>" : className + ".";

            DateTime now = DateTime.Now;
            string fileName = Path.GetFileName(sourceFilePath);

            _current.Value.Log(
                DateTime.Now,
                Thread.CurrentThread.ManagedThreadId,
                sourceFilePath,
                sourceLineNumber,
                className,
                memberName,
                message,
                exception);
        }
    }
}
