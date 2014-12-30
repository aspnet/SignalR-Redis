// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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

        //public static IServiceCollection AddSignalRRedis(this IServiceCollection services, string server, int port, string password, string eventKey)
        //{
        //    var configuration = new RedisScaleoutConfiguration(server, port, password, eventKey);

        //    return AddSignalRRedis(services, configuration);
        //}

        //public static IServiceCollection AddSignalRRedis(this IServiceCollection services, RedisScaleoutConfiguration configuration)
        //{
        //    //var describer = new ServiceDescriber(configuration);
        //    //services.TryAdd(describer.Transient<IConfigureOptions<RedisOptions>, RedisOptionsSetup>());
        //    //services.TryAdd(describer.Singleton<IMessageBus, RedisMessageBus>());
            
        //    services.AddTransient<IConfigureOptions<RedisScaleoutConfiguration>, RedisOptionsSetup>();
        //    services.AddSingleton<IMessageBus, RedisMessageBus>();

        //    //if (configuration != null)
        //    {
        //        services.Configure<RedisScaleoutConfiguration>(configuration);
        //    }

        //    return services;
        //}
    }
}