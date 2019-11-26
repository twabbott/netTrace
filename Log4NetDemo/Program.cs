using log4net;
using log4net.Repository.Hierarchy;
using log4net.Core;
using log4net.Appender;
using log4net.Layout;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Config;
using NetTrace;
using log4net.Util;

namespace Log4NetDemo
{
    class Program
    {
        private class MockAppender : AppenderSkeleton
        {
            public LoggingEvent LastEvent { get; private set; }

            protected override void Append(LoggingEvent loggingEvent)
            {
                LastEvent = loggingEvent;

                Console.WriteLine("==== Event Info ==================================================================");
                Console.WriteLine(loggingEvent.RenderedMessage);
                Console.WriteLine("");
                Console.WriteLine(loggingEvent.Properties["trace-context"]);
            }
        }

        private class Log4NetTrace : TraceContext
        {
            const string LOGGER_NAME = "Log4NetDemo";

            protected override void OnFinalize(TraceInfo info)
            {
                ILog logger = LogManager.GetLogger(LOGGER_NAME);

                PropertiesDictionary logProps = new PropertiesDictionary();
                logProps["trace-context"] = info.ToString();

                TraceEvent exEvent = info.Events.FindLast(ev => ev.Exception != null);
                logger.Logger.Log(new LoggingEvent(new LoggingEventData
                {
                    LoggerName = LOGGER_NAME,
                    Level = exEvent == null ? Level.Info : Level.Error,
                    Message = exEvent == null ? "Trace was successful" : "Trace caught a failure",
                    ExceptionString = exEvent?.ToString(),
                    Properties = logProps
                }));
            }
        }

        static async Task Main(string[] args)
        {
            // Configure log4net.  Usually this will be done in app.config / web.config
            Hierarchy hierarchy = (Hierarchy)log4net.LogManager.GetRepository();

            hierarchy.Root.RemoveAllAppenders();
            hierarchy.Root.AddAppender(new MockAppender());
            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;

            BasicConfigurator.Configure(hierarchy);

            // Do a trace
            using (new Log4NetTrace())
            {
                try
                {
                    TraceContext.Log("App start");

                    MyClass mc = new MyClass();
                    await mc.Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("App end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("Man, this was bad...", ex);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey(false);

        }

        class MyClass
        {
            public async Task Foo()
            {
                TraceContext.Log("starting");

                await Bar().ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);

                TraceContext.Log("finished");
            }

            public async Task Bar()
            {
                TraceContext.Log("starting");
                await Task.Delay(100).ConfigureAwait(false);

                //throw new ApplicationException("YEEEET!");
                TraceContext.Log("finished!");
            }
        }
    }
}
