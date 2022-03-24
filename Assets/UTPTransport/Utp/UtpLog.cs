using UnityEngine;
using System;

namespace Utp
{
	/// <summary>
	/// Different levels of log severity.
	/// </summary>
	public enum LogLevel : int
	{
		Off     = -1,
		None    = 0,
		Verbose = 1,
		Info    = 2,
		Warning = 3,
		Error   = 4
	}

	/// <summary>
	/// Non-static UTPLog for verbose logging inside burst compile jobs.
	/// </summary>
	public struct UtpLog
	{
		private LogLevel currentLogLevel;
		private string prefix;
		private string postfix;

		/// <summary>
		/// Instantiates a new UTP logger.
		/// </summary>
		public UtpLog(string logPrefix = "", string logPostfix = "")
        {
			currentLogLevel = LogLevel.Verbose;
			prefix = logPrefix;
			postfix = logPostfix;
		}

		/// <summary>
		/// Logs a message to the UTP logger. Uses the last logging level state.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Log(string message) { LogMessage(message); }

		/// <summary>
		/// Logs a verbose message to the UTP Logger.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Verbose(string message) { LogMessage(message, LogLevel.Verbose); }

		/// <summary>
		/// Logs an informational message to the UTP Logger.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Info(string message) { LogMessage(message, LogLevel.Info); }

		/// <summary>
		/// Logs a warning to the UTP logger.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Warning(string message) { LogMessage(message, LogLevel.Warning); }

		/// <summary>
		/// Logs an error to the UTP Logger.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Error(string message) { LogMessage(message, LogLevel.Error); }

		/// <summary>
		/// Sets the logging level.
		/// </summary>
		/// <param name="level">The level to set the UTP logger to.</param>
		public void SetLogLevel(LogLevel level) { currentLogLevel = level; }

		/// <summary>
		/// Logs a message to the UTP logger given a message type.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="level">The type of message.</param>
		private void LogMessage(string message, LogLevel level = LogLevel.None)
        {
			if (level != LogLevel.None)
			{
				SetLogLevel(level);
            }
			switch (currentLogLevel)
			{
				case (LogLevel.Verbose): Debug.Log(prefix + message + postfix);        break;
				case (LogLevel.Info):    Debug.Log(prefix + message + postfix);        break;
				case (LogLevel.Warning): Debug.LogWarning(prefix + message + postfix); break;
				case (LogLevel.Error):   Debug.LogError(prefix + message + postfix);   break;
				default: /* Off */ break;
			}
        }
	}
}