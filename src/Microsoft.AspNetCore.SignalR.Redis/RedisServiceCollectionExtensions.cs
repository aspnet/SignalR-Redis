// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Messaging;
using Microsoft.AspNetCore.SignalR.Redis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, Action<RedisScaleoutOptions> configureOptions = null)
        {
            return services.AddRedis(configuration: null, configureOptions: configureOptions);
        }

        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration, Action<RedisScaleoutOptions> configureOptions)
        {
            services.AddSingleton<IMessageBus, RedisMessageBus>();
            services.AddSingleton<IRedisConnection, RedisConnection>();

            if (configuration != null)
            {
                services.Configure<RedisScaleoutOptions>(configuration);
            }

            if(configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}