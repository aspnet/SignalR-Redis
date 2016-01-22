// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.SignalR.Messaging;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisScaleoutOptions : ScaleoutOptions
    {
        public RedisScaleoutOptions()
            : this("localhost", 6379, "", "_default")
        {
        }

        public RedisScaleoutOptions(string server, int port, string password, string eventKey)
            : this(CreateConnectionString(server, port, password), eventKey)
        {
        }

        public RedisScaleoutOptions(string connectionString, string eventKey)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (String.IsNullOrEmpty(eventKey))
            {
                throw new ArgumentNullException(nameof(eventKey));
            }

            ConnectionString = connectionString;
            EventKey = eventKey;
        }

        public string ConnectionString { get; set; }

        public int Database { get; set; }

        public string EventKey { get; set; }

        public static string CreateConnectionString(string server, int port, string password)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}:{1}, password={2}, abortConnect=false", server, port, password);
        }
    }
}