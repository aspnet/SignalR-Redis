// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.SignalR.Messaging;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisMessage
    {
        public ulong Id { get; private set; }
        public ScaleoutMessage ScaleoutMessage { get; private set; }

        public static byte[] ToBytes(IList<Message> messages)
        {
            using (var ms = new MemoryStream())
            {
                var binaryWriter = new BinaryWriter(ms);

                var scaleoutMessage = new ScaleoutMessage(messages);
                var buffer = scaleoutMessage.ToBytes();

                binaryWriter.Write(buffer.Length);
                binaryWriter.Write(buffer);

                return ms.ToArray();
            }
        }

        public static RedisMessage FromBytes(byte[] data, ILogger logger)
        {
            using (var stream = new MemoryStream(data))
            {
                var message = new RedisMessage();

                // read message id from memory stream until SPACE character
                var messageIdBuilder = new StringBuilder(20);
                do
                {
                    // it is safe to read digits as bytes because they encoded by single byte in UTF-8
                    int charCode = stream.ReadByte();
                    if (charCode == -1)
                    {
                        logger.LogDebug("Received Message could not be parsed.");
                        throw new EndOfStreamException();
                    }
                    char c = (char)charCode;
                    if (c == ' ')
                    {
                        message.Id = ulong.Parse(messageIdBuilder.ToString(), CultureInfo.InvariantCulture);
                        messageIdBuilder = null;
                    }
                    else
                    {
                        messageIdBuilder.Append(c);
                    }
                }
                while (messageIdBuilder != null);

                var binaryReader = new BinaryReader(stream);
                int count = binaryReader.ReadInt32();
                byte[] buffer = binaryReader.ReadBytes(count);

                message.ScaleoutMessage = ScaleoutMessage.FromBytes(buffer);
                return message;
            }
        }
    }
}