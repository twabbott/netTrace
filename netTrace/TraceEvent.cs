using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTrace
{
    public struct TraceEvent
    {
        /// <summary>
        ///     Time that the event was logged.
        /// </summary>
        public DateTime TimeStamp { get; set; }


        /// <summary>
        ///     Thread id of the thread that logged the event.
        /// </summary>
        public int ThreadId { get; set; }


        /// <summary>
        ///     Filename where the event was logged.
        /// </summary>
        public string Filename { get; set; }

        
        /// <summary>
        ///     Line number where the event was logged.
        /// </summary>
        public int LineNumber { get; set; }


        /// <summary>
        ///     Name of the class where the event was logged.
        /// </summary>
        public string ClassName { get; set; }


        /// <summary>
        ///     Name of the function or property that logged the event.
        /// </summary>
        public string MemberName { get; set; }


        /// <summary>
        ///     A message that was logged with the event
        /// </summary>
        public string Message { get; set; }


        /// <summary>
        ///     An exception that was logged with the event
        /// </summary>
        public Exception Exception { get; set; }


        /// <summary>
        ///     Convert this event object to a string.  If the event has an 
        ///     exception, it will also be converted to a string.<para/>
        ///     <para/>
        ///     Format is:<para/>
        ///     yyyy/MM/dd HH:mm:ss.fff [ThreadId] Filename(LineNumber) - ClassName.MemberName() - Message");
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{TimeStamp:yyyy/MM/dd HH:mm:ss.fff} [{ThreadId:000}] {Path.GetFileName(Filename)}({LineNumber}) - {ClassName}{MemberName}() - {Message}");
            if (Exception != null)
            {
                sb.AppendLine().AppendLine(Exception.ToString());
            }

            return sb.ToString();
        }
    }
}
