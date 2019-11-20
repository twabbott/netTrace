using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTrace;

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
            Action anonFunc = () =>
            {
                TraceContext.Log("Inside an anonymous function");
            };

            using (new TraceContext(OutputHandler))
            {
                try
                {
                    TraceContext.Log("App start");

                    anonFunc();

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
