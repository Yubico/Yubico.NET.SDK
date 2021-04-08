// Copyright (c) Yubico AB

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Yubico.Core.Logging
{
    public static class Log
    {
        private static ILoggerFactory? _factory = null;

        public static ILoggerFactory LoggerFactory
        {
            get
            {
                if (_factory is null)
                {
                    _factory = new NullLoggerFactory();
                }
                return _factory;
            }
            set => _factory = value;
        }

        public static ILogger GetLogger() => LoggerFactory.CreateLogger("Yubico.Core logger");
    }
}
