using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NetTrace
{
    /// <summary>
    ///     Contains lines logged during a TraceContext trace session.
    /// </summary>
    public sealed class TraceInfo
    {
        #region internal

        /// <summary>
        ///     Makes a new TraceInfo object.
        /// </summary>
        /// 
        /// <param name="previous">
        ///     A reference to a previous TraceInfo object created on this 
        ///     thread, if there was one.
        /// </param>
        internal TraceInfo(TraceInfo previous = null)
        {
            Previous = previous;
        }


        /// <summary>
        ///     Back-link to the previous TraceInfo instance.
        /// </summary>
        internal TraceInfo Previous { get; set; }


        /// <summary>
        ///     Logs a line to this instance, and all previous instances.
        /// </summary>
        /// 
        /// <param name="text">
        ///     A line of text to log (minus the CR/LF)
        /// </param>
        /// <param name="isException">
        ///     A flag indicating whether the information being logged 
        ///     represents an exception stack trace or other error info.
        /// </param>
        internal void Log(string text, bool isException)
        {
            Lines.Add(text);
            HasExceptionLogged |= isException;

            if (Previous != null)
            {
                Previous.Log(text, isException);
            }
        }

        #endregion


        /// <summary>
        ///     A list of all lines that were logged to the TraceInfo object.
        /// </summary>
        public List<string> Lines { get; private set; } = new List<string>(100);


        /// <summary>
        ///     A flag you can check to see if TraceContext.LogException was 
        ///     ever called.
        /// </summary>
        public bool HasExceptionLogged { get; set; } = false;


        /// <summary>
        ///     Dumps the contents of the Lines collection to a string.
        /// </summary>
        /// 
        /// <returns>
        ///     A string that has all the lines.
        /// </returns>
        public override string ToString()
        {
            return string.Join("\r\n", Lines);
        }
    }
}
