using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTrace;
using FluentAssertions;
using System.Xml;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace NetTraceTests
{
    class MyTraceContext : TraceContext
    {
        public Action<TraceInfo> _inheritedCallback;

        public MyTraceContext(Action<TraceInfo> instCallback = null, Action<TraceInfo> inheritedCallback = null) : base(instCallback)
        {
            _inheritedCallback = inheritedCallback;
        }

        protected override void OnFinalize(TraceInfo info)
        {
            _inheritedCallback?.Invoke(info);
        }
    }


    [TestClass]
    public class TraceTests
    {
        public void ValidateTrace(TraceInfo info, bool hasException, params string[] lines)
        {
            int i = 0;
            info.Events.ForEach(actualLine => actualLine.ToString().Should().Contain(lines[i++]));
            i.Should().Equals(lines.Length); // Make sure we got all the way through the list.

            info.HasExceptionLogged.Should().Equals(hasException);
        }

        [TestMethod]
        public async Task HappyPath()
        {
            TraceInfo info = null;
            using (new TraceContext(ti => info = ti))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(6);
            ValidateTrace(
                info,
                false,
                "Test-start",
                "HappyPath_Foo-start",
                "HappyPath_Bar-start",
                "HappyPath_Bar-end",
                "HappyPath_Foo-end",
                "Test-end"
            );
        }

        public async Task HappyPath_Foo()
        {
            TraceContext.Log("HappyPath_Foo-start");

            await HappyPath_Bar().ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.Log("HappyPath_Foo-end");
        }

        public async Task HappyPath_Bar()
        {
            TraceContext.Log("HappyPath_Bar-start");
            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.Log("HappyPath_Bar-end");
        }

        [TestMethod]
        public async Task CallWriteLineWithoutTraceContext()
        {
            TraceContext.Log("This should not get logged.");

            TraceInfo info = null;
            using (new TraceContext(ti => info = ti))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            TraceContext.Log("This should not get logged.");

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(6);
            ValidateTrace(
                info,
                false,
                "Test-start",
                "HappyPath_Foo-start",
                "HappyPath_Bar-start",
                "HappyPath_Bar-end",
                "HappyPath_Foo-end",
                "Test-end"
            );
        }

        [TestMethod]
        public async Task TraceContextWithNoLogs()
        {
            TraceInfo info = null;
            using (new TraceContext(ti => info = ti))
            {
                // Do fake work...
                await Task.Delay(100).ConfigureAwait(false);
            }

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(0);
            ValidateTrace(
                info,
                false
            );
        }

        [TestMethod]
        public async Task HappyPathToString()
        {
            TraceInfo info = null;
            using (new TraceContext(ti => info = ti))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            string lines = info.ToString();
            lines.IndexOf("Test-start").Should().BeGreaterThan(0);
            lines.IndexOf("HappyPath_Foo-start").Should().BeGreaterThan(0);
            lines.IndexOf("HappyPath_Bar-start").Should().BeGreaterThan(0);
            lines.IndexOf("HappyPath_Bar-end").Should().BeGreaterThan(0);
            lines.IndexOf("HappyPath_Foo-end").Should().BeGreaterThan(0);
            lines.IndexOf("Test-end").Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task Exception()
        {
            TraceInfo info = null;
            using (new TraceContext(ti => info = ti))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await Exception_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(4);
            ValidateTrace(
                info,
                true,
                "Test-start",
                "Exception_Foo-start",
                "Exception_Bar-start",
                "error",
                "Yeeeet!"
            );
        }

        public async Task Exception_Foo()
        {
            TraceContext.Log("Exception_Foo-start");

            await Exception_Bar().ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.Log("Exception_Foo-end");
        }

        public async Task Exception_Bar()
        {
            TraceContext.Log("Exception_Bar-start");
            await Task.Delay(100).ConfigureAwait(false);

            throw new Exception("Yeeeet!");
        }

        [TestMethod]
        public async Task NestedContexts()
        {
            TraceInfo info1 = null, info2 = null;
            using (new TraceContext(ti => info1 = ti))
            {
                try
                {
                    TraceContext.Log("Outer-start");

                    await Nested_Foo(ti => info2 = ti);

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Outer-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            Debug.WriteLine(info1.ToString());
            info1.Events.Should().HaveCount(10);
            ValidateTrace(
                info1,
                true,
                "Outer-start",
                "Nested_Foo-start",
                "Nested_NewContext-outer-start",
                "Nested_NewContext-start",
                "Nested_Bar-start",
                "Nested_Bar-end",
                "Nested_NewContext-end",
                "Nested_NewContext-outer-end",
                "Nested_Foo-end",
                "Outer-end"
            );

            Debug.WriteLine(info2.ToString());
            info2.Events.Should().HaveCount(4);
            ValidateTrace(
                info2,
                false,
                "Nested_NewContext-start",
                "Nested_Bar-start",
                "Nested_Bar-end",
                "Nested_NewContext-end"
            );
        }

        public async Task Nested_Foo(Action<TraceInfo> finalizeCallback)
        {
            TraceContext.Log("Nested_Foo-start");

            await Nested_NewContext(finalizeCallback).ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.Log("Nested_Foo-end");
        }

        public async Task Nested_NewContext(Action<TraceInfo> finalizeCallback)
        {
            TraceContext.Log("Nested_NewContext-outer-start");

            using (new TraceContext(finalizeCallback))
            {
                TraceContext.Log("Nested_NewContext-start");

                await Nested_Bar().ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);

                TraceContext.Log("Nested_NewContext-end");
            }

            TraceContext.Log("Nested_NewContext-outer-end");
        }

        public async Task Nested_Bar()
        {
            TraceContext.Log("Nested_Bar-start");
            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.Log("Nested_Bar-end");
        }

        [TestMethod]
        public async Task FinalizerGlobal()
        {
            TraceInfo info = null;
            Action<TraceInfo> globalCallback = ti => info = ti;

            using (new MyTraceContext(inheritedCallback: globalCallback))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(6);
            ValidateTrace(
                info,
                false,
                "Test-start",
                "HappyPath_Foo-start",
                "HappyPath_Bar-start",
                "HappyPath_Bar-end",
                "HappyPath_Foo-end",
                "Test-end"
            );
        }


        [TestMethod]
        public async Task FinalizerLocalAndGlobal()
        {
            TraceInfo info = null, info2 = null;
            Action<TraceInfo> globalCallback = ti => info2 = ti;

            using (new MyTraceContext(ti => info = ti, globalCallback))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            object.ReferenceEquals(info, info2).Should().BeTrue();

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(6);
            ValidateTrace(
                info,
                false,
                "Test-start",
                "HappyPath_Foo-start",
                "HappyPath_Bar-start",
                "HappyPath_Bar-end",
                "HappyPath_Foo-end",
                "Test-end"
            );
        }

        [TestMethod]
        public async Task FinalizerNone()
        {
            using (new TraceContext())
            {
                try
                {
                    TraceContext.Log("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            // Nothing to assert.  Just making sure we don't get a crash.
        }

        [TestMethod]
        public async Task StressTestTasks()
        {
            DateTime start = DateTime.Now;
            int count = 1000;
            List<Task> tasks = new List<Task>(count);

            ManualResetEvent ev = new ManualResetEvent(false);

            // Spawn a bunch of background workers
            Debug.WriteLine($"Master - spawning tasks.");
            for (int i = 0; i < count; i++)
            {
                int num = i; // local copy of i gets captured into closure
                Task t = Task.Run(async () =>
                {
                    // Wait until the main thread says I can go
                    Debug.WriteLine($"Task {num} - waiting for start.");
                    ev.WaitOne();

                    // Execute the HappyPath test
                    Debug.WriteLine($"Task {num} - starting.");

                    await HappyPath();

                    Debug.WriteLine($"Task {num} - finished.");
                });

                tasks.Add(t);
            }

            // Tell all the workers to go.
            Debug.WriteLine($"Master - Releasing mutex, waiting for all to complete.");
            ev.Set();

            // Wait for all the workers to complete.
            await Task.WhenAll(tasks);
            Debug.WriteLine($"Master - finished.");
        }

        [TestMethod]
        public void StressTestThreads()
        {
            int count = 1000;

            ManualResetEvent ev = new ManualResetEvent(false);
            CountdownEvent cde = new CountdownEvent(count);

            // Spawn a bunch of background workers
            Debug.WriteLine($"Master - spawning threads.");
            for (int i = 0; i < count; i++)
            {
                int num = i; // local copy of i gets captured into closure
                Thread t = new Thread(() =>
                {
                    // Wait until the main thread says I can go
                    Debug.WriteLine($"Thread {num} - waiting for start.");
                    ev.WaitOne();

                    // Execute the NonAsync test
                    Debug.WriteLine($"Thread {num} - starting.");

                    NonAsync();

                    cde.Signal();
                    Debug.WriteLine($"Thread {num} - finished.");
                });

                t.IsBackground = true;
                t.Start();
            }

            // Tell all the workers to go.
            Debug.WriteLine($"Master - Releasing mutex, waiting for all to complete.");
            ev.Set();

            // Wait for all the workers to complete.
            cde.Wait(3000).Should().BeTrue("Test should complete in less than 3 seconds");
            Debug.WriteLine($"Master - finished.");
        }

        [TestMethod]
        public void NonAsync()
        {
            TraceInfo info = null;
            using (new TraceContext(ti => info = ti))
            {
                try
                {
                    TraceContext.Log("Test-start");

                    NonAsync_Foo();
                    Thread.Sleep(100);

                    TraceContext.Log("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.Log("error", ex);
                }
            }

            Debug.WriteLine(info.ToString());
            info.Events.Should().HaveCount(6);
            ValidateTrace(
                info,
                false,
                "Test-start",
                "NonAsync_Foo-start",
                "NonAsync_Bar-start",
                "NonAsync_Bar-end",
                "NonAsync_Foo-end",
                "Test-end"
            );
        }

        public void NonAsync_Foo()
        {
            TraceContext.Log("NonAsync_Foo-start");

            NonAsync_Bar();
            Thread.Sleep(100);

            TraceContext.Log("NonAsync_Foo-end");
        }

        public void NonAsync_Bar()
        {
            TraceContext.Log("NonAsync_Bar-start");

            Thread.Sleep(100);

            TraceContext.Log("NonAsync_Bar-end");
        }
    }
}
