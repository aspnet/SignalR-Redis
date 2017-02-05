// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.AspNet.SignalR.Redis;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.Framework.DependencyInjection
{
    public static class RedisSignalRServicesBuilderExtensions
    {
        public static SignalRServicesBuilder AddRedis(this SignalRServicesBuilder builder, Action<RedisScaleoutConfiguration> configureOptions = null)
        {
            return builder.AddRedis(configuration: null, configureOptions: configureOptions);
        }

        public static SignalRServicesBuilder AddRedis(this SignalRServicesBuilder builder, IConfiguration configuration, Action<RedisScaleoutConfiguration> configureOptions)
        {
            builder.ServiceCollection.AddSingleton<IMessageBus, RedisMessageBus>();
            builder.ServiceCollection.AddSingleton<IRedisConnection, RedisConnection>();

            if (configuration != null)
            {
                builder.ServiceCollection.Configure<RedisScaleoutConfiguration>(configuration);
            }

            if(configureOptions != null)
            {
                builder.ServiceCollection.Configure(configureOptions);
            }

            return builder;
        }
    }
}