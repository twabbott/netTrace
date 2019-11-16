using System;
using System.Collections.Generic;
using System.Text;

namespace NetTrace
{
    public sealed class TraceInfo
    {
        internal TraceInfo() { }

        /// <summary>
        ///     A list of all lines that were logged to the TraceInfo object.
        /// </summary>
        public List<string> Lines { get; private set; } = new List<string>();


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
