# netTrace
Trace utility for collecting/tracking log info for asynchronous code.  

Basic example code:
```C#
using (TraceContext.Begin(info => Console.WriteLine(info.ToString())))
{
    TraceContext.WriteLine("Hello, world!");

    // Do stuff here...
}
```
Essentially, you wrap an initial block of code that you want to trace with a `using` statement, and then literally anywhere else in your code, you can call `TraceContext.WriteLine()` or `TraceContext.LogException()`.  The `TraceContext` class will keep track of all logging info, regardless of what function you are in, regardless of whether or not an async call has switched threads on you.

You must specify some kind of output handler that will get called when `IDispose` gets called.  You can do this either by passing a function to the constructor, or you can set a global handler using the `TraceContext.FinalizeCallback` event.

Here is a more involved sample.  Note that you don't have to pass a `TraceContext` object around.  All we have to do is call the `WriteLine` method, and whatever we log will get collected in the right place:
```C#
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
            using (TraceContext.Begin(OutputHandler))
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

    // This can be in a separate file, or even in a separate class library.
    class MyClass
    {
        public async Task Foo()
        {
            TraceContext.WriteLine("MyClass.Foo - starting");

            await Bar().ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("MyClass.Foo - finished");
        }

        public async Task Bar()
        {
            TraceContext.WriteLine("MyClass.Bar - starting");
            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("MyClass.Bar - finished!");

            // Or try throwing an exception here...
            //throw new ApplicationException("YEEEET!");
        }
    }
}
```

Here is some sample output.  Note the thread id was changed at one point:
```
Got some trace info.
No exception was logged.
2019/10/25 16:54:27.061 [001] Program.cs(25) Main - App start
2019/10/25 16:54:27.063 [001] Program.cs(52) Foo - MyClass.Foo - starting
2019/10/25 16:54:27.063 [001] Program.cs(64) Bar - MyClass.Bar - starting
2019/10/25 16:54:27.179 [004] Program.cs(68) Bar - MyClass.Bar - finished!
2019/10/25 16:54:27.280 [004] Program.cs(58) Foo - MyClass.Foo - finished
2019/10/25 16:54:27.395 [004] Program.cs(32) Main - App end

Press any key...
```

If you log an exception, here is a sample of what you might get:
```
Got some trace info.
An exception was logged.
2019/11/15 17:06:29.037 [001] Program.cs(25) Main - App start
2019/11/15 17:06:29.039 [001] Program.cs(50) Foo - MyClass.Foo - starting
2019/11/15 17:06:29.040 [001] Program.cs(61) Bar - MyClass.Bar - starting
2019/11/15 17:06:29.201 [004] EXCEPTION - Man, this was bad...
System.ApplicationException: YEEEET!
   at demo.MyClass.<Bar>d__1.MoveNext() in C:\Git\Projects\netTrace\demo\Program.cs:line 64
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter.GetResult()
   at demo.MyClass.<Foo>d__0.MoveNext() in C:\Git\Projects\netTrace\demo\Program.cs:line 52
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at demo.Program.<Main>d__1.MoveNext() in C:\Git\Projects\netTrace\demo\Program.cs:line 28

Press any key...
```

Format for each line that the `WriteLine` function generates is:
```
YYYY/MM/DD HH:MM:SS.xxx [thread-id] filename(line-number) FuncName - whatever text you give.
```
