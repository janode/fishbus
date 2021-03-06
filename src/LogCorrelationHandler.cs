﻿using System;
using Microsoft.Azure.ServiceBus;
using Serilog.Context;

namespace Thon.Hotels.FishBus
{
    public class LogCorrelationHandler
    {
        internal Func<Message, IDisposable> PushToLogContext { get; set; }

        internal LogCorrelationHandler(bool useCorrelationLogging, LogCorrelationOptions options = null)
        {
            if (!useCorrelationLogging)
            {
                PushToLogContext = (message) => new EmptyContextPusher();
            }
            else
            {
                var logPropertyName = options?.LogPropertyName ?? "CorrelationId";
                var messagePropertyName = options?.MessagePropertyName ?? "logCorrelationId";

                PushToLogContext =
                    CreatePushToLogContext(logPropertyName, messagePropertyName, options?.SetCorrelationLogId);
            }
        }

        private static Func<Message, IDisposable> CreatePushToLogContext(string logPropertyName,
            string messagePropertyName, Action<string> setCorrelationLogId) =>
            (message) =>
            {
                var logCorrelationId = message.UserProperties.ContainsKey(messagePropertyName)
                    ? message.UserProperties[messagePropertyName]
                    : Guid.NewGuid();

                setCorrelationLogId?.Invoke(logCorrelationId.ToString());
                return LogContext.PushProperty(logPropertyName, logCorrelationId);
            };
    }

    internal class EmptyContextPusher : IDisposable
    {
        public void Dispose()
        {
        }
    }
}