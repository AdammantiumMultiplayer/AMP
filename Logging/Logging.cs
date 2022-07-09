using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Logging {
    public static class Log {

        public enum Type {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }

        public static void Debug(object obj) {
            Debug(obj.ToString());
        }

        public static void Debug(string message) {
            Msg(Type.DEBUG, message);
        }

        public static void Info(object obj) {
            Info(obj.ToString());
        }

        public static void Info(string message) {
            Msg(Type.INFO, message);
        }

        public static void Warn(object obj) {
            Warn(obj.ToString());
        }

        public static void Warn(string message) {
            Msg(Type.WARNING, message);
        }

        public static void Err(object obj) {
            Err(obj.ToString());
        }

        public static void Err(string message) {
            Msg(Type.ERROR, message);
        }

        public static void Out(Type type, object obj) {
            Msg(type, obj.ToString());
        }

        public static void Msg(Type type, string message) {
            switch(type) {
                case Type.DEBUG:
                    #if DEBUG_MESSAGES
                    UnityEngine.Debug.Log(message);
                    #endif
                    break;
                case Type.INFO:
                    UnityEngine.Debug.Log(message);
                    break;
                case Type.WARNING:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case Type.ERROR:
                    UnityEngine.Debug.LogError(message);
                    break;

                default: break;
            }
        }
    }
}
