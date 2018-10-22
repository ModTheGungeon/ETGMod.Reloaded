using System;
using MonoMod;

namespace UnityEngine.Patches {
    /// <summary>
    /// Patches the UnityEngine logger to use ModTheGungeon's logger
    /// </summary>
    [MonoModPatch("UnityEngine.Logger")]
    public class Logger {
        public static ModTheGungeon.Logger GungeonLogger = new ModTheGungeon.Logger("Gungeon");

        /// <summary>
        /// Converts a UnityEngine.LogType to an ModTheGungeon.Logger.LogLevel
        /// </summary>
        /// <returns>The ModTheGungeon LogLevel</returns>
        /// <param name="type">The Unity LogType</param>
        private ModTheGungeon.Logger.LogLevel _LogTypeToLogLevel(LogType type) {
            switch(type) {
                case LogType.Log: return ModTheGungeon.Logger.LogLevel.Info;
                case LogType.Assert: return ModTheGungeon.Logger.LogLevel.Error;
                case LogType.Error: return ModTheGungeon.Logger.LogLevel.Error;
                case LogType.Exception: return ModTheGungeon.Logger.LogLevel.Error;
                case LogType.Warning: return ModTheGungeon.Logger.LogLevel.Warn;
                default: return ModTheGungeon.Logger.LogLevel.Debug;
            }
        }

        private string _FormatMessage(object message, string tag = null, Object context = null) {
            if (tag != null && context != null) return $"[tag: {tag}, context: {context}] {message}";
            if (tag != null && context == null) return $"[context: {context}] {message}";
            if (tag == null && context != null) return $"[tag: {tag}] {message}";
            return message.ToString();
        }

        public bool IsLogTypeAllowed(LogType logType) {
            return GungeonLogger.LogLevelEnabled(_LogTypeToLogLevel(logType));
        }

        public void Log(string tag, object message, Object context) {
            GungeonLogger.Info(_FormatMessage(message, tag: tag, context: context));
        }

        public void Log(string tag, object message) {
            GungeonLogger.Info(_FormatMessage(message, tag: tag));
        }

        public void Log(object message) {
            GungeonLogger.Info(_FormatMessage(message));
        }

        public void Log(LogType logType, string tag, object message, Object context) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message, tag: tag, context: context));
        }

        public void Log(LogType logType, string tag, object message) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message, tag: tag));
        }

        public void Log(LogType logType, object message, Object context) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message, context: context));
        }

        public void Log(LogType logType, object message) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message));
        }

        public void LogError(string tag, object message, Object context) {
            GungeonLogger.Error(_FormatMessage(message, tag: tag, context: context));
        }

        public void LogError(string tag, object message) {
            GungeonLogger.Error(_FormatMessage(message, tag: tag));
        }

        public void LogException(Exception exception) {
            GungeonLogger.Error(_FormatMessage(exception));
        }

        public void LogException(Exception exception, Object context) {
            GungeonLogger.Error(_FormatMessage(exception, context: context));
        }

        public void LogWarning(string tag, object message) {
            GungeonLogger.Warn(_FormatMessage(message, tag: tag));
        }

        public void LogWarning(string tag, object message, Object context) {
            GungeonLogger.Warn(_FormatMessage(message, tag: tag, context: context));
        }
    }
}
