using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netTrace;

namespace demo
{
    class Program
    {
        private static void OutputHandler(TraceInfo info)
        {
            Console.WriteLine("Got some trace info.");
            Console.WriteLine(info.HasExceptionLogged ? "An exception was logged." : "No exception was logged.");
            Console.WriteLine(info.ToString());
        }

        static async Task Main(string[] args)
        {
            using (new TraceContext(OutputHandler))
            {
                try
                {
                    TraceContext.WriteLine("App start");

                    MyClass mc = new MyClass();
                    await mc.Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("App end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "Man, this was bad...");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey(false);
        }
    }

    class MyClass
    {
        public async Task Foo()
        {
            using (new TraceContext(info => Console.WriteLine("This should never get called.")))
            {
                TraceContext.WriteLine("MyClass.Foo - starting");

                await Bar().ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);

                TraceContext.WriteLine("MyClass.Foo - finished");
            }
        }

        public async Task Bar()
        {
            TraceContext.WriteLine("MyClass.Bar - starting");
            await Task.Delay(100).ConfigureAwait(false);

            //throw new ApplicationException("YEEEET!");
            TraceContext.WriteLine("MyClass.Bar - finished!");
        }
    }
}
