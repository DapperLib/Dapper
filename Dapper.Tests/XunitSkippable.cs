using System;

#if XUNIT2 // Not in .Net 4.0 (xUnit v1)...
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
#endif

namespace Dapper.Tests
{
    public class SkipTestException : Exception
    {
        public SkipTestException(string reason) : base(reason) { }
    }

#if XUNIT2
    // Most of the below is a direct copy & port from the wonderful examples by Brad Wilson at
    // https://github.com/xunit/samples.xunit/tree/master/DynamicSkipExample
    public class SkippableFactDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink _diagnosticMessageSink;

        public SkippableFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            yield return new SkippableFactTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
        }
    }
    
    public class SkippableFactTestCase : XunitTestCase
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippableFactTestCase() { }

        public SkippableFactTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        { }

        public override async Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            var skipMessageBus = new SkippableFactMessageBus(messageBus);
            var result = await base.RunAsync(
                diagnosticMessageSink,
                skipMessageBus,
                constructorArguments,
                aggregator,
                cancellationTokenSource);
            if (skipMessageBus.DynamicallySkippedTestCount > 0)
            {
                result.Failed -= skipMessageBus.DynamicallySkippedTestCount;
                result.Skipped += skipMessageBus.DynamicallySkippedTestCount;
            }

            return result;
        }
    }

    public class SkippableFactMessageBus : IMessageBus
    {
        readonly IMessageBus _innerBus;
        public SkippableFactMessageBus(IMessageBus innerBus)
        {
            _innerBus = innerBus;
        }

        public int DynamicallySkippedTestCount { get; private set; }

        public void Dispose() { }

        public bool QueueMessage(IMessageSinkMessage message)
        {
            var testFailed = message as ITestFailed;
            if (testFailed != null)
            {
                var exceptionType = testFailed.ExceptionTypes.FirstOrDefault();
                if (exceptionType == typeof(SkipTestException).FullName)
                {
                    DynamicallySkippedTestCount++;
                    return _innerBus.QueueMessage(new TestSkipped(testFailed.Test, testFailed.Messages.FirstOrDefault()));
                }
            }
            return _innerBus.QueueMessage(message);
        }
    }
#endif
}
