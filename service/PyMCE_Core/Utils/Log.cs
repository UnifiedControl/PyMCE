using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace PyMCE.Core.Utils
{
    #region Enumerations

    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    [Flags]
    public enum LogTarget
    {
        Console     = 1,
        Debug       = 2,
        EventLog    = 4
    }

    #endregion

    public class Log
    {
        private const string FormatMessageFull = "({0:yyyy-MM-dd HH:mm:ss.ffffff}) [{1}] [{2}] - {3}";

        private static bool _isEnabled = true;
        private static LogTarget _target = LogTarget.Debug;
        private static readonly Dictionary<string, EventLog> EventLogCache;

        public static bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }
        public static LogTarget Target
        {
            get { return _target; }
            set { _target = value; }
        }

        static Log()
        {
            EventLogCache = new Dictionary<string, EventLog>();
        }

        private static string GetExecutingClassName()
        {
            var loggerName = "";

            var stackTrace = new StackTrace();

            var stackFrames = stackTrace.GetFrames();
            if (stackFrames == null) return "";

            for (var index = stackFrames.Length - 1; index >= 0; index--)
            {
                var frame = stackFrames[index];
                var method = frame.GetMethod();

                // Skip over any mscorlib methods
                if (method.DeclaringType != null &&
                    method.DeclaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Finish when we hit this class ("Log")
                if (method.DeclaringType == typeof(Log))
                    break;

                if(method.DeclaringType != null)
                    loggerName = method.DeclaringType.FullName;
            }

            return loggerName;
        }

        private static string GetFullMessage(LogLevel level, string message, string className)
        {
            return string.Format(FormatMessageFull, DateTime.Now, className, level.ToString().ToUpper(), message);
        }

        private static void EventLogWrite(string className, string message, EventLogEntryType type)
        {
            if (!EventLogCache.ContainsKey(className))
            {
                if (!EventLog.SourceExists(className))
                {
                    EventLog.CreateEventSource(className, "Application");
                }
                EventLogCache[className] = new EventLog()
                                               {
                                                   Source = className,
                                                   EnableRaisingEvents = true
                                               };
            }

            EventLogCache[className].WriteEntry(message, type);
        }

        public static void WriteLine(LogLevel level, Exception exception)
        {
            WriteLine(level, exception.ToString());
        }

        public static void WriteLine(LogLevel level, string message, params object[] args)
        {
            if (!IsEnabled) return;

            var className = GetExecutingClassName();

            message = string.Format(message, args);
            string messageFull = null;

            if ((_target & LogTarget.Console) == LogTarget.Console)
            {
                messageFull = GetFullMessage(level, message, className);

                Console.WriteLine(messageFull);
            }

            if ((_target & LogTarget.Debug) == LogTarget.Debug)
            {
                if (messageFull == null)
                    messageFull = GetFullMessage(level, message, className);

                System.Diagnostics.Debug.WriteLine(messageFull);
            }

            if ((_target & LogTarget.EventLog) == LogTarget.EventLog)
            {
                switch (level)
                {
                    // Ignore Debug and Trace messages (excessive logging)
                    case LogLevel.Debug:  break;
                    case LogLevel.Trace: break;

                    case LogLevel.Info:
                        EventLogWrite(className, message, EventLogEntryType.Information);
                        break;
                    case LogLevel.Warn:
                        EventLogWrite(className, message, EventLogEntryType.Warning);
                        break;
                    case LogLevel.Error:
                        EventLogWrite(className, message, EventLogEntryType.Error);
                        break;
                }
            }
        }

        public static void WriteArray(LogLevel level, Array array)
        {
            var message = "";

            foreach (var item in array)
            {
                if (item is byte)
                    message += string.Format("{0:X2}", (byte) item);

                else if (item is ushort)
                    message += string.Format("{0:X4}", (ushort) item);

                else if (item is int)
                    message += string.Format("{1}{0}", (int) item, (int) item > 0 ? "+" : String.Empty);

                else
                    message += string.Format("{0}", item);

                message += ", ";
            }

            WriteLine(level, message);
        }

        #region Trace

        public static void Trace(string message, params object[] args)
        {
            WriteLine(LogLevel.Trace, message, args);
        }

        public static void T(string message, params object[] args)
        {
            Trace(message, args);
        }

        #endregion

        #region Info

        public static void Info(Exception ex)
        {
            WriteLine(LogLevel.Info, ex);
        }

        public static void Info(string message, params object[] args)
        {
            WriteLine(LogLevel.Info, message, args);
        }

        public static void I(string message, params object[] args)
        {
            Info(message, args);
        }

        #endregion

        #region Debug

        public static void Debug(string message, params object[] args)
        {
            WriteLine(LogLevel.Debug, message, args);
        }

        public static void D(string message, params object[] args)
        {
            Debug(message, args);
        }

        #endregion

        #region Warn

        public static void Warn(Exception exception)
        {
            WriteLine(LogLevel.Warn, exception);
        }

        public static void Warn(string message, params object[] args)
        {
            WriteLine(LogLevel.Warn, message, args);
        }

        public static void W(string message, params object[] args)
        {
            Warn(message, args);
        }

        #endregion

        #region Error

        public static void Error(Exception exception)
        {
            WriteLine(LogLevel.Error, exception);
        }

        public static void Error(string message, params object[] args)
        {
            WriteLine(LogLevel.Error, message, args);
        }

        public static void E(string message, params object[] args)
        {
            Error(message, args);
        }

        #endregion
    }
}
