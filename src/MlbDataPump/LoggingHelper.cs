// -----------------------------------------------------------------------
// <copyright file="LoggingHelper.cs" company="Microsoft">
//     Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace MlbDataPump
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Infrastructure.DataAccess;

    public enum LoggingLevelEnum
    {
        Error = 1,
        Warning = 2,
        InvalidData = 4,
        Trace = 8,
        Debug_Low = 16,
        Debug_High = 32,
    }

    /// <summary>
    /// Helper class for logging operations.
    /// </summary>
    internal class LoggingHelper : IMessageLogger
    {
        /// <summary>
        /// The version of this component (used for instrumentation).
        /// </summary>
        private static readonly string ComponentVersion;

        /// <summary>
        /// The log directory.
        /// </summary>
        private static string logDir;

        /// <summary>
        /// Initializes static members of the <see cref="LoggingHelper" /> class.
        /// </summary>
        static LoggingHelper()
        {
            ComponentVersion = FileVersionInfo.GetVersionInfo(
                Assembly.GetExecutingAssembly().Location).FileVersion;

            string assemblyOriginalLocation = Assembly.GetExecutingAssembly().CodeBase;
            if (assemblyOriginalLocation.StartsWith("file:", StringComparison.Ordinal))
            {
                assemblyOriginalLocation = (new Uri(assemblyOriginalLocation)).LocalPath;
            }

            FileInfo originalAssemblyFile = new FileInfo(assemblyOriginalLocation);
            string originalDir = originalAssemblyFile.Directory.FullName;
            logDir = Path.Combine(originalAssemblyFile.Directory.Parent.Parent.FullName, "Data");
            if (Directory.Exists(logDir) == false)
            {
                Directory.CreateDirectory(logDir);
            }
        }

        /// <summary>
        /// Gets the Logging level flags
        /// </summary>
        public static short LevelFlags
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the path to the logging directory.
        /// </summary>
        public string LoggingDirectory
        {
            get
            {
                return logDir;
            }
        }

        /// <summary>
        /// Informational log.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public void Info(string message, params object[] args)
        {
            LogString(LoggingLevelEnum.Trace, message, args);
        }

        /// <summary>
        /// Warning log.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public void Warn(string message, params object[] args)
        {
            LogString(LoggingLevelEnum.Warning, message, args);
        }

        /// <summary>
        /// Error log.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">Additional message.</param>
        /// <param name="args">The arguments.</param>
        public void Error(Exception exception, string message, params object[] args)
        {
            LogString(LoggingLevelEnum.Error, message + exception.ToString(), args);
        }

        /// <summary>
        /// A helper function to fire the qos event.
        /// </summary>
        /// <param name="transactionContext">The transaction context.</param>
        /// <param name="apiId">The api for the event.</param>
        /// <param name="duration">The time span.</param>
        /// <param name="exception">The exception encountered.</param>
        public void FireQosEvent(string transactionContext, string apiId, TimeSpan elapsed, Exception exception)
        {
            FireQosEventInternal(transactionContext, apiId, elapsed, exception);
        }

        /// <summary>
        /// Log a debug low message
        /// </summary>
        /// <param name="partner">partner name context</param>
        /// <param name="message">Log Message</param>
        /// <param name="parameters">Log Message string format parameters</param>
        internal static void LogDebugLow(string message, params object[] parameters)
        {
            LogString(LoggingLevelEnum.Debug_Low, message, parameters);
        }

        /// <summary>
        /// Log a debug high message
        /// </summary>
        /// <param name="partner">partner name context</param>
        /// <param name="message">Log Message</param>
        /// <param name="parameters">Log Message string format parameters</param>
        internal static void LogDebugHigh(string message, params object[] parameters)
        {
            LogString(LoggingLevelEnum.Debug_High, message, parameters);
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="level">Logging level</param>
        /// <param name="partner">partner name context</param>
        /// <param name="message">Log Message</param>
        /// <param name="parameters">Log Message string format parameters</param>
        private static void LogString(LoggingLevelEnum level, string message, params object[] parameters)
        {
            if ((LevelFlags & (short)level) != 0)
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = string.Empty;
                }

                if (parameters.Length > 0)
                {
                    message = string.Format(CultureInfo.InvariantCulture, message, parameters);
                }

                string path = Path.Combine(logDir, "logdata.txt");
                using (StreamWriter outfile = File.Exists(path) ? File.AppendText(path) : File.CreateText(path))
                {
                    outfile.WriteLine(message);
                    outfile.Flush();
                }
            }
        }

        /// <summary>
        /// A helper function to fire the qos event.
        /// </summary>
        /// <param name="transactionContext">The transaction context.</param>
        /// <param name="apiId">The api for the event.</param>
        /// <param name="duration">The time span.</param>
        /// <param name="exception">The exception encountered.</param>
        private static void FireQosEventInternal(
            string transactionContext,
            string apiId,
            TimeSpan duration,
            Exception exception)
        {
            try
            {
                DateTimeOffset now = DateTimeOffset.Now;
                if (exception == null)
                {
                    LogString(
                        LoggingLevelEnum.Debug_Low,
                        "{0}::{1}::{2}::{3}",
                        now.ToString(),
                        transactionContext,
                        apiId,
                        duration.Milliseconds);
                }
                else
                {
                    LogString(
                        LoggingLevelEnum.Error,
                        "{0}::{1}::{2}::{3}::{4}\n{5}",
                        now.ToString(),
                        transactionContext,
                        apiId,
                        duration.Milliseconds,
                        exception.Message,
                        exception.StackTrace);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
