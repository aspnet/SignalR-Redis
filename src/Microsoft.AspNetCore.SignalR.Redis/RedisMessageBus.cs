// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private readonly int _db;
        private string _key;
        private readonly ILogger _logger;

        private IRedisConnection _connection;
        private string _connectionString;
        private int _state;
        private readonly object _callbackLock = new object();

        public RedisMessageBus(IStringMinifier stringMinifier,
                                     ILoggerFactory loggerFactory,
                                     IPerformanceCounterManager performanceCounterManager,
                                     IOptions<MessageBusOptions> optionsAccessor,
                                     IOptions<RedisScaleoutOptions> scaleoutConfigurationAccessor, IRedisConnection connection)
            : this(stringMinifier, loggerFactory, performanceCounterManager, optionsAccessor, scaleoutConfigurationAccessor, connection, true)
        {
        }

        internal RedisMessageBus(IStringMinifier stringMinifier,
                                     ILoggerFactory loggerFactory,
                                     IPerformanceCounterManager performanceCounterManager,
                                     IOptions<MessageBusOptions> optionsAccessor,
                                     IOptions<RedisScaleoutOptions> scaleoutConfigurationAccessor,
                                     IRedisConnection connection,
                                     bool connectAutomatically)
            : base(stringMinifier, loggerFactory, performanceCounterManager, optionsAccessor, scaleoutConfigurationAccessor)
        {
            _connectionString = scaleoutConfigurationAccessor.Value.ConnectionString;
            _db = scaleoutConfigurationAccessor.Value.Database;
            _key = scaleoutConfigurationAccessor.Value.EventKey;

            _connection = connection;

            _logger = loggerFactory.CreateLogger<RedisMessageBus>();

            ReconnectDelay = TimeSpan.FromSeconds(2);

            if (connectAutomatically)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var ignore = ConnectWithRetry();
                });
            }
        }

        public TimeSpan ReconnectDelay { get; set; }

        // For testing purposes only
        internal int ConnectionState { get { return _state; } }

        public virtual void OpenStream(int streamIndex)
        {
            Open(streamIndex);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _connection.ScriptEvaluateAsync(
                _db,
                @"local newId = redis.call('incr', KEYS[1])
                local payload = newId .. ' ' .. ARGV[1]
                redis.call('publish', KEYS[1], payload)
                return {newId, ARGV[1], payload}",
                _key,
                RedisMessage.ToBytes(messages));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var oldState = Interlocked.Exchange(ref _state, State.Disposing);

                switch (oldState)
                {
                    case State.Connected:
                        Shutdown();
                        break;
                    case State.Closed:
                    case State.Disposing:
                        // No-op
                        break;
                    case State.Disposed:
                        Interlocked.Exchange(ref _state, State.Disposed);
                        break;
                    default:
                        break;
                }
            }

            base.Dispose(disposing);
        }

        private void Shutdown()
        {
            _logger.LogInformation("Shutdown()");

            if (_connection != null)
            {
                _connection.Close(_key, allowCommandsToComplete: false);
            }

            Interlocked.Exchange(ref _state, State.Disposed);
        }

        private void OnConnectionFailed(Exception ex)
        {
            string errorMessage = (ex != null) ? ex.Message : Resources.Error_RedisConnectionClosed;

            _logger.LogInformation("OnConnectionFailed - " + errorMessage);

            Interlocked.Exchange(ref _state, State.Closed);
        }

        private void OnConnectionError(Exception ex)
        {
            OnError(0, ex);
            _logger.LogError("OnConnectionError - " + ex.Message);
        }

        private async void OnConnectionRestored(Exception ex)
        {
            await _connection.RestoreLatestValueForKey(_db, _key);

            _logger.LogInformation("Connection restored");

            Interlocked.Exchange(ref _state, State.Connected);

            OpenStream(0);
        }

        private void OnMessage(int streamIndex, RedisMessage message)
        {
            // locked to avoid overlapping calls (even though we have set the mode 
            // to preserve order on the subscription)
            lock (_callbackLock)
            {
                OnReceived(streamIndex, message.Id, message.ScaleoutMessage);
            }
        }

        internal async Task ConnectWithRetry()
        {
            while (true)
            {
                try
                {
                    await ConnectToRedisAsync();

                    var oldState = Interlocked.CompareExchange(ref _state,
                                               State.Connected,
                                               State.Closed);

                    if (oldState == State.Closed)
                    {
                        OpenStream(0);
                    }
                    else
                    {
                        Debug.Assert(oldState == State.Disposing, "unexpected state");

                        Shutdown();
                    }

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error connecting to Redis - " + ex.GetBaseException());
                }

                if (_state == State.Disposing)
                {
                    Shutdown();
                    break;
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        private async Task ConnectToRedisAsync()
        {
            if (_connection != null)
            {
                _connection.ErrorMessage -= OnConnectionError;
                _connection.ConnectionFailed -= OnConnectionFailed;
                _connection.ConnectionRestored -= OnConnectionRestored;
            }

            _logger.LogInformation("Connecting...");

            await _connection.ConnectAsync(_connectionString, _logger);

            _logger.LogInformation("Connection opened");

            _connection.ErrorMessage += OnConnectionError;
            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;

            await _connection.SubscribeAsync(_key, OnMessage);

            _logger.LogDebug("Subscribed to event " + _key);
        }

        internal static class State
        {
            public const int Closed = 0;
            public const int Connected = 1;
            public const int Disposing = 2;
            public const int Disposed = 3;
        }
    }
}