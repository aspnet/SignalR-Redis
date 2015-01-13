// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisScaleoutConfiguration : ScaleoutConfiguration
    {
        public RedisScaleoutConfiguration()
            : this("localhost", 6379, "", "_default")
        {
        }

        public RedisScaleoutConfiguration(string server, int port, string password, string eventKey)
            : this(CreateConnectionString(server, port, password), eventKey)
        {
        }

        public RedisScaleoutConfiguration(string connectionString, string eventKey)
        {
            if(connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if(eventKey == null)
            {
                throw new ArgumentNullException("eventKey");
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