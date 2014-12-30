﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisOptionsSetup : ConfigureOptions<RedisScaleoutConfiguration>
    {
        public RedisOptionsSetup() : base(ConfigureRedis)
        {
            /// The default order for sorting is -1000. Other framework code
            /// the depends on order should be ordered between -1 to -1999.
            /// User code should order at bigger than 0 or smaller than -2000.
            Order = -1000;
        }

        private static void ConfigureRedis(RedisScaleoutConfiguration options)
        {
        }
    }
}