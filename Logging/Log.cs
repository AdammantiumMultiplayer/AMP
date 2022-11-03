using System;
using System.Collections.Generic;
using System.Drawing;

namespace AMP.Logging {
    public static class Log {

        public enum Type {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }

        public enum LoggerType {
            UNITY,
            CONSOLE
        }

        public static LoggerType loggerType = LoggerType.CONSOLE;

        public static void Debug(object obj) {
            if(obj == null) {
                Debug("null");
                return;
            }
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

        public static void Line(char character) {
            int cnt = 50;
            if(loggerType == LoggerType.CONSOLE) cnt = Console.WindowWidth;
            Msg(Type.INFO, "".PadLeft(cnt, character));
        }

        public static void Line(char character, string title) {
            int cnt = 50;
            if(loggerType == LoggerType.CONSOLE) cnt = Console.WindowWidth;
            Msg(Type.INFO, $" {title} ".PadLeft(cnt / 2, character).PadRight(cnt, character));
        }

        private static void Msg(Type type, string message) {
            switch(type) {
                case Type.DEBUG:
                    #if DEBUG_MESSAGES
                    if(loggerType == LoggerType.UNITY) UnityEngine.Debug.Log(message);
                    else if(loggerType == LoggerType.CONSOLE) ConsoleLine(message);
                    #endif

                    break;

                case Type.INFO:
                    if(loggerType == LoggerType.UNITY) UnityEngine.Debug.Log(message);
                    else if(loggerType == LoggerType.CONSOLE) ConsoleLine(message);

                    break;

                case Type.WARNING:
                    if(loggerType == LoggerType.UNITY) UnityEngine.Debug.LogWarning(message);
                    else if(loggerType == LoggerType.CONSOLE) ConsoleLine($"<color=#FFFF00>{message}</color>");

                    break;

                case Type.ERROR:
                    if(loggerType == LoggerType.UNITY) UnityEngine.Debug.LogError(message);
                    else if(loggerType == LoggerType.CONSOLE) ConsoleLine($"<color=#FF0000>{message}</color>");

                    break;
            
                default: break;
            }
        }

        private static readonly Queue<string> messageQueue = new Queue<string>();

        private static void ConsoleLine(string message) {
            lock(messageQueue) {
                messageQueue.Enqueue(message);
            }
            ProcessQueue();
        }

        private static void ProcessQueue() {
            lock(messageQueue) {
                while(messageQueue.Count > 0) {
                    string message = messageQueue.Dequeue();

                    List<ConsoleColor> colors = new List<ConsoleColor>();
                    char[] chars = message.ToCharArray();

                    for(int i = 0; i < chars.Length; i++) {
                        if(message.Substring(i).StartsWith("<color=")) {
                            Color c = ColorTranslator.FromHtml(message.Substring(i + 7, 7));
                            colors.Add(ClosestConsoleColor(c.R, c.G, c.B));
                            Console.ForegroundColor = colors[colors.Count - 1];
                            i += 15;
                        } else if(message.Substring(i).StartsWith("</color>")) {
                            colors.RemoveAt(colors.Count - 1);
                            if(colors.Count > 0) {
                                Console.ForegroundColor = colors[colors.Count - 1];
                            } else {
                                Console.ResetColor();
                            }
                            i += 8;
                        }
                        if(i >= chars.Length) break;
                        Console.Write(chars[i]);
                    }
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }
        }

        private static ConsoleColor ClosestConsoleColor(byte r, byte g, byte b) {
            ConsoleColor ret = 0;
            double rr = r, gg = g, bb = b, delta = double.MaxValue;

            foreach(ConsoleColor cc in Enum.GetValues(typeof(ConsoleColor))) {
                var n = Enum.GetName(typeof(ConsoleColor), cc);
                var c = System.Drawing.Color.FromName(n == "DarkYellow" ? "Orange" : n); // bug fix
                var t = Math.Pow(c.R - rr, 2.0) + Math.Pow(c.G - gg, 2.0) + Math.Pow(c.B - bb, 2.0);
                if(t == 0.0)
                    return cc;
                if(t < delta) {
                    delta = t;
                    ret = cc;
                }
            }
            return ret;
        }
    }
}
