# NetTrace
A trace utility for collecting/tracking log info in multi-threaded code.  

Sifting through log info for a single request in a multi-threaded environment is difficult, because
a typical log file contains output for many different operations that are all happening at the same
time.  The problem is compounded when log info is sent to a separate server, and because of network latency, your output arrives out of order.  

The `TraceContext` class allows you to collect info on a per-thread (or per threading context)
basis, and when the request is complete you can dump all trace info to the log file in one single write operation.

NetTrace was designed with the following goals in mind:
* Optimal performance with minimal overhead.  All write operations are O(1).
* No need to re-instrument code in place.  All logging is done to a static object.
* Log info is gathered on a per-thread basis, and functions as expected when using async / await.


## Getting Started
Basic example code:
```C#
using (new TraceContext(info => Console.WriteLine(info.ToString())))
{
    TraceContext.Log("Hello, world!");

    // Do stuff here...
}
```
Essentially, you wrap an initial block of code that you want to trace with a `using` statement, and then literally anywhere else in your code, you can call `TraceContext.Log()` or `TraceContext.LogException()`.  The `TraceContext` class will keep track of all logging info, regardless of what function you are in, regardless of whether or not an async call has switched threads on you.

You must specify some kind of output handler that will get called when `IDispose` gets called.  You can do this either by passing a function to the constructor, or you can set a global handler using the `TraceContext.FinalizeCallback` event.

Here is a more involved example.  Note that you don't have to pass a `TraceContext` object around.  All we have to do is call the `Log` method, and whatever we log will get collected in the right place:
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
            using (new TraceContext(OutputHandler))
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
    }

    // This can be in a separate file, or even in a separate class library.
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

            TraceContext.Log("finished!");

            // Or try throwing an exception here...
            //throw new ApplicationException("YEEEET!");
        }
    }
}
```

If you like, you may inherit the `TraceContext` object, and override its `OnFinalize` method to provide a general-purpose handler for all traces:
```C#
    class ConsoleTraceContext : TraceContext
    {
        protected override void OnFinalize(TraceInfo info)
        {
            Console.WriteLine(info.ToString());
        }
    }
```

This can make starting a trace somewhat cleaner:
```C#
using (new ConsoleTraceContext())
{
    TraceContext.Log("Hello, world!");

    // Do stuff here...
}
```

Here is some sample output.  Note the thread id got changed at one point:
```
Got some trace info.
No exception was logged.
2019/11/20 13:55:10.794 [001] Program.cs(30) - Program.Main() - App start
2019/11/20 13:55:10.796 [001] Program.cs(23) - Program.Main() - Inside an anonymous function
2019/11/20 13:55:10.797 [001] Program.cs(57) - MyClass.Foo() - starting
2019/11/20 13:55:10.797 [001] Program.cs(68) - MyClass.Bar() - starting
2019/11/20 13:55:10.914 [004] Program.cs(72) - MyClass.Bar() - finished!
2019/11/20 13:55:11.024 [004] Program.cs(63) - MyClass.Foo() - finished
2019/11/20 13:55:11.132 [004] Program.cs(39) - Program.Main() - App end
```

If you log an exception, here is a sample of what you might get:
```
Got some trace info.
An exception was logged.
2019/11/20 13:54:29.167 [001] Program.cs(30) - Program.Main() - App start
2019/11/20 13:54:29.169 [001] Program.cs(23) - Program.Main() - Inside an anonymous function
2019/11/20 13:54:29.170 [001] Program.cs(57) - MyClass.Foo() - starting
2019/11/20 13:54:29.170 [001] Program.cs(68) - MyClass.Bar() - starting
2019/11/20 13:54:29.398 [004] Program.cs(43) - Program.Main() - Man, this was bad...
System.ApplicationException: YEEEET!
   at demo.MyClass.<Bar>d__1.MoveNext() in C:\Git\Projects\netTrace\demo\Program.cs:line 71
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter.GetResult()
   at demo.MyClass.<Foo>d__0.MoveNext() in C:\Git\Projects\netTrace\demo\Program.cs:line 59
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at demo.Program.<Main>d__1.MoveNext() in C:\Git\Projects\netTrace\demo\Program.cs:line 35
```

The `Log` function formats each line of debug info as follows:
```
yyyy/MM/dd HH:mm:ss.fff [ThreadId] Filename(LineNumber) - ClassName.MemberName() - Message
```

# References
It’s important to understand that when you use async/await, every time you await a long-running operation, .NET can (i.e., usually will) switch threads on you.  This means that keeping any kind of thread-local storage will not work.  For this reason, they created the `AsyncLocal<T>` type.  This is how the TraceContext object keeps track of all its trace info.

For more info on getting thread-local behavior with asynchronous code, here are some of the references I found:

[MSDN Documentation on AsyncLocal](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1?view=netframework-4.8):
* The `AsyncLocal<T>` type allows you to declare a static variable that is local to the current thread, and which stays with the current thread even when async/await changes the current thread on you.
* Contains a concrete example that I found the most helpful, which demonstrates how using the `[ThreadLocal]` attribute will get you into trouble when using async/await.

[Implicit Async Context](https://blog.stephencleary.com/2013/04/implicit-async-context-asynclocal.html)
* The `AsyncLocal<T>` type is available only in .NET Framework 4.6 or later.  For earlier versions, you must use `CallContext.LogicalGetData` and `CallContext.LogicalSetData`, which does kind of the same thing.
* This blog explains it all, and gives some good example code.
