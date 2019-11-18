﻿using System;
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
    [TestClass]
    public class TraceTests
    {
        public void ValidateTrace(TraceInfo info, bool hasException, params string[] lines)
        {
            int i = 0;
            info.Lines.ForEach(actualLine => actualLine.Should().Contain(lines[i++]));
            i.Should().Equals(lines.Length); // Make sure we got all the way through the list.

            info.HasExceptionLogged.Should().Equals(hasException);
        }

        [TestMethod]
        public async Task HappyPath()
        {
            TraceInfo info = null;
            using (TraceContext.Begin(ti => info = ti))
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            info.Lines.Should().HaveCount(6);
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
            TraceContext.WriteLine("HappyPath_Foo-start");

            await HappyPath_Bar().ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("HappyPath_Foo-end");
        }

        public async Task HappyPath_Bar()
        {
            TraceContext.WriteLine("HappyPath_Bar-start");
            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("HappyPath_Bar-end");
        }

        [TestMethod]
        public async Task CallWriteLineWithoutTraceContext()
        {
            TraceContext.WriteLine("This should not get logged.");

            TraceInfo info = null;
            using (TraceContext.Begin(ti => info = ti))
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            TraceContext.WriteLine("This should not get logged.");

            info.Lines.Should().HaveCount(6);
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
            using (TraceContext.Begin(ti => info = ti))
            {
                // Do fake work...
                await Task.Delay(100).ConfigureAwait(false);
            }

            info.Lines.Should().HaveCount(0);
            ValidateTrace(
                info,
                false
            );
        }

        [TestMethod]
        public async Task HappyPathToString()
        {
            TraceInfo info = null;
            using (TraceContext.Begin(ti => info = ti))
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
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
            using (TraceContext.Begin(ti => info = ti))
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await Exception_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            info.Lines.Should().HaveCount(5);
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
            TraceContext.WriteLine("Exception_Foo-start");

            await Exception_Bar().ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("Exception_Foo-end");
        }

        public async Task Exception_Bar()
        {
            TraceContext.WriteLine("Exception_Bar-start");
            await Task.Delay(100).ConfigureAwait(false);

            throw new Exception("Yeeeet!");
        }

        [TestMethod]
        public async Task NestedContexts()
        {
            TraceInfo info1 = null, info2 = null;
            using (TraceContext.Begin(ti => info1 = ti))
            {
                try
                {
                    TraceContext.WriteLine("Outer-start");

                    await Nested_Foo(ti => info2 = ti);

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Outer-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            info1.Lines.Should().HaveCount(10);
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

            info2.Lines.Should().HaveCount(4);
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
            TraceContext.WriteLine("Nested_Foo-start");

            await Nested_NewContext(finalizeCallback).ConfigureAwait(false);

            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("Nested_Foo-end");
        }

        public async Task Nested_NewContext(Action<TraceInfo> finalizeCallback)
        {
            TraceContext.WriteLine("Nested_NewContext-outer-start");

            using (TraceContext.Begin(finalizeCallback))
            {
                TraceContext.WriteLine("Nested_NewContext-start");

                await Nested_Bar().ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);

                TraceContext.WriteLine("Nested_NewContext-end");
            }

            TraceContext.WriteLine("Nested_NewContext-outer-end");
        }

        public async Task Nested_Bar()
        {
            TraceContext.WriteLine("Nested_Bar-start");
            await Task.Delay(100).ConfigureAwait(false);

            TraceContext.WriteLine("Nested_Bar-end");
        }

        [TestMethod]
        public async Task FinalizerGlobal()
        {
            TraceInfo info = null;
            Action<TraceInfo> globalCallback = ti => info = ti;

            TraceContext.FinalizeCallback += globalCallback;

            using (TraceContext.Begin())
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            TraceContext.FinalizeCallback -= globalCallback;

            info.Lines.Should().HaveCount(6);
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
            TraceInfo info1 = null, info2 = null;
            Action<TraceInfo> globalCallback = ti => info2 = ti;

            TraceContext.FinalizeCallback += globalCallback;

            using (TraceContext.Begin(ti => info1 = ti))
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            TraceContext.FinalizeCallback -= globalCallback;

            object.ReferenceEquals(info1, info2).Should().BeTrue();

            info1.Lines.Should().HaveCount(6);
            ValidateTrace(
                info1,
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
            using (TraceContext.Begin())
            {
                try
                {
                    TraceContext.WriteLine("Test-start");

                    await HappyPath_Foo();

                    await Task.Delay(100).ConfigureAwait(false);

                    TraceContext.WriteLine("Test-end");
                }
                catch (Exception ex)
                {
                    TraceContext.LogException(ex, "error");
                }
            }

            // Nothing to assert.  Just making sure we don't get a crash.
        }

        [TestMethod]
        public void StressTestThreads()
        {
            DateTime start = DateTime.Now;
            int count = 1000;
            List<Thread> threads = new List<Thread>(count);

            ManualResetEvent ev = new ManualResetEvent(false);
            CountdownEvent cde = new CountdownEvent(count);

            // Spawn a bunch of background workers
            Debug.WriteLine($"Master - spawning threads.");
            for (int i = 0; i < count; i++)
            {
                int num = i; // local copy of i gets captured into closure
                Thread t = new Thread(async () =>
                {
                    Debug.WriteLine($"Task {num} - waiting for start.");
                    // Wait until the main thread says I can go
                    ev.WaitOne();

                    // Do the test
                    Debug.WriteLine($"Task {num} - starting.");
                    await HappyPath();

                    cde.Signal();
                    Debug.WriteLine($"Task {num} - finished.");
                });
                t.IsBackground = true;
                t.Start();

                threads.Add(t);
            }

            // Tell all the workers to go.
            Debug.WriteLine($"Master - Setting the event, waiting for all threads to complete.");
            ev.Set();
            cde.Wait();

            Debug.WriteLine($"Master - finished.");
            Debug.WriteLine($"Finished in {(DateTime.Now - start):hh\\:mm\\:ss\\.fff}");
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
                    Debug.WriteLine($"Task {num} - waiting for start.");
                    // Wait until the main thread says I can go
                    ev.WaitOne();

                    // Do the test
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
            Debug.WriteLine($"Finished in {(DateTime.Now - start):hh\\:mm\\:ss\\.fff}");
        }
    }
}
