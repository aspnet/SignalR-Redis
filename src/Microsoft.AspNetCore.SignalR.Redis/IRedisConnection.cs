// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public interface IRedisConnection
    {
        Task ConnectAsync(string connectionString, ILogger logger);

        void Close(string key, bool allowCommandsToComplete = true);

        Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage);

        Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments);

        Task RestoreLatestValueForKey(int database, string key);

        void Dispose();

        event Action<Exception> ConnectionFailed;
        event Action<Exception> ConnectionRestored;
        event Action<Exception> ErrorMessage;
    }
}