﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.AspNet.SignalR.Redis;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.Framework.DependencyInjection
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection AddSignalRRedis(this IServiceCollection services, Action<RedisScaleoutConfiguration> configureOptions = null)
        {
            return services.AddSignalRRedis(configuration: null, configureOptions: configureOptions);
        }

        public static IServiceCollection AddSignalRRedis(this IServiceCollection services, IConfiguration configuration, Action<RedisScaleoutConfiguration> configureOptions)
        {
            services.AddSingleton<IMessageBus, RedisMessageBus>();
            services.AddSingleton<IRedisConnection, RedisConnection>();

            if (configuration != null)
            {
                services.Configure<RedisScaleoutConfiguration>(configuration);
            }

            if(configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}