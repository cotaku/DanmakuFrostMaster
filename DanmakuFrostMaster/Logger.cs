﻿using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Foundation.Diagnostics;

namespace Atelier39
{
    internal static class Logger
    {
        private static LoggingChannel _logChannel;

        public static void SetLogger(LoggingChannel loggingChannel)
        {
            _logChannel = loggingChannel;
        }

        public static void Log(string message, LoggingLevel level = LoggingLevel.Information, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                filePath = Path.GetFileName(filePath);
            }
            message = $"{filePath}({lineNumber})->{memberName}(): {message}";
            Debug.WriteLine(message);
            _logChannel?.LogMessage(message, level);
        }
    }
}
