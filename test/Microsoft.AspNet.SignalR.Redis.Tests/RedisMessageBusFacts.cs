﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Redis.Tests
{
    public class RedisMessageBusFacts
    {
        internal class SignalROptionsAccessor : IOptions<SignalROptions>
        {
            private SignalROptions Options = new SignalROptions();

            SignalROptions IOptions<SignalROptions>.Options
            {
                get
                {
                    return Options;
                }
            }

            public SignalROptions GetNamedOptions(string name)
            {
                throw new NotImplementedException();
            }
        }

        internal class RedisOptionsAccessor : IOptions<RedisScaleoutOptions>
        {
            private RedisScaleoutOptions Options = new RedisScaleoutOptions();

            RedisScaleoutOptions IOptions<RedisScaleoutOptions>.Options
            {
                get
                {
                    return Options;
                }
            }

            public RedisScaleoutOptions GetNamedOptions(string name)
            {
                throw new NotImplementedException();
            }
        }

        internal class TestLogger : ILogger
        {
            public IDisposable BeginScope(object state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public void Write(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                return;
            }
        }

        internal class TestLoggerFactory : ILoggerFactory
        {
            public void AddProvider(ILoggerProvider provider)
            {
                throw new NotImplementedException();
            }

            public ILogger Create(string name)
            {
                return new TestLogger();
            }
        }

        [Fact]
        public async void ConnectRetriesOnError()
        {
            int invokationCount = 0;
            var wh = new ManualResetEventSlim();
            var redisConnection = GetMockRedisConnection();

            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<ILogger>())).Returns<string, ILogger>((connectionString, logger) =>
            {
                if (++invokationCount == 2)
                {
                    wh.Set();
                    return Task.FromResult(0);
                }
                else
                {
                    //Return cancelled task to insert error
                    return tcs.Task;
                }
            });

            var redisMessageBus = new RedisMessageBus(new Mock<IStringMinifier>().Object, new TestLoggerFactory(),
                new PerformanceCounterManager(new TestLoggerFactory()), new SignalROptionsAccessor(), new RedisOptionsAccessor(), redisConnection.Object, false);

            await redisMessageBus.ConnectWithRetry();

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal(RedisMessageBus.State.Connected, redisMessageBus.ConnectionState);
        }

        [Fact]
        public async void OpenCalledOnConnectionRestored()
        {
            int openInvoked = 0;
            var wh = new ManualResetEventSlim();

            var redisConnection = GetMockRedisConnection();

            var redisMessageBus = new Mock<RedisMessageBus>(new Mock<IStringMinifier>().Object, new TestLoggerFactory(), new Mock<IPerformanceCounterManager>().Object, new SignalROptionsAccessor(), new RedisOptionsAccessor(), redisConnection.Object)
            { CallBase = true };

            redisMessageBus.Setup(m => m.OpenStream(It.IsAny<int>())).Callback(() =>
            {
                // Open would be called twice - once when connection starts and once when it is restored
                if (++openInvoked == 2)
                {
                    wh.Set();
                }
            });

            // Creating an instance to invoke the constructor which starts the connection
            var instance = redisMessageBus.Object;

            // Give constructor time to "connect"
            await Task.Delay(100);

            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async void ConnectionFailedChangesStateToClosed()
        {
            var redisConnection = GetMockRedisConnection();

            var redisMessageBus = new RedisMessageBus(new Mock<Infrastructure.IStringMinifier>().Object, new TestLoggerFactory(), new Infrastructure.PerformanceCounterManager(new TestLoggerFactory()), new SignalROptionsAccessor(), new RedisOptionsAccessor(), redisConnection.Object, false);

            await redisMessageBus.ConnectWithRetry();

            Assert.Equal(RedisMessageBus.State.Connected, redisMessageBus.ConnectionState);

            redisConnection.Raise(mock => mock.ConnectionFailed += null, new Exception("Test exception"));

            Assert.Equal(RedisMessageBus.State.Closed, redisMessageBus.ConnectionState);
        }

        [Fact]
        public async void RestoreLatestValueForKeyCalledOnConnectionRestored()
        {
            bool restoreLatestValueForKey = false;

            var redisConnection = GetMockRedisConnection();

            redisConnection.Setup(m => m.RestoreLatestValueForKey(It.IsAny<int>(), It.IsAny<string>())).Returns(() =>
            {
                restoreLatestValueForKey = true;
                return Task.FromResult<object>(null);
            });

            var redisMessageBus = new RedisMessageBus(new Mock<IStringMinifier>().Object, new TestLoggerFactory(), new PerformanceCounterManager(new TestLoggerFactory()), new SignalROptionsAccessor(), new RedisOptionsAccessor(), redisConnection.Object, false);

            await redisMessageBus.ConnectWithRetry();

            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            Assert.True(restoreLatestValueForKey, "RestoreLatestValueForKey not invoked");
        }

        private Mock<IRedisConnection> GetMockRedisConnection()
        {
            var redisConnection = new Mock<IRedisConnection>();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<ILogger>()))
                .Returns(Task.FromResult(0));

            redisConnection.Setup(m => m.SubscribeAsync(It.IsAny<string>(), It.IsAny<Action<int, RedisMessage>>()))
                .Returns(Task.FromResult(0));

            return redisConnection;
        }
    }
}