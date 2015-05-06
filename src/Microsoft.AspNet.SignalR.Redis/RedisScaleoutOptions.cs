// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.SignalR.Redis
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

        public RedisScaleoutOptions([NotNull] string connectionString, [Notnull] string eventKey)
        {
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