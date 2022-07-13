using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMP.Data {
    public class INIFile {

        string path = "";

        public INIFile(string path) {
            this.path = path;
        }

        public bool FileExists() {
            return File.Exists(path);
        }

        public string[] RemoveEmptyLines(string[] Lines) {
            List<string> newcontent = new List<string>();
            for (int i = 0; i < Lines.Length; i++) {
                if (!(Lines[i] == "" || Lines[i] == " " || Lines[i] == "\n")) {
                    newcontent.Add(Lines[i]);
                }
            }

            string[] NewLines = new string[newcontent.Count];
            for (int i = 0; i < newcontent.Count; i++) {
                NewLines[i] = newcontent[i];
            }

            return NewLines;
        }

        public void SetOption(string OptionName, object Wert) {
            if (File.Exists(path)) {
                string[] lines = RemoveEmptyLines(File.ReadAllLines(path));
                bool foundString = false;
                for (int i = 0; i < lines.Length; i++) {
                    if (lines[i].Contains(OptionName + "=")) {
                        lines[i] = OptionName + "=" + Wert.ToString();
                        foundString = true;
                    }
                }
                if (!foundString) {
                    string[] newlines = new string[lines.Length + 1];
                    for (int i = 0; i < lines.Length; i++) {
                        newlines[i] = lines[i];
                    }
                    newlines[lines.Length] = OptionName + "=" + Wert.ToString();
                    lines = newlines;
                }

                File.WriteAllLines(path, lines);
                return;
            } else {
                string content = OptionName + "=" + Wert.ToString() + "\n";

                File.WriteAllText(path, content);
                return;
            }
        }

        public string GetOption(string OptionName, string fallbackvalue) {
            if (File.Exists(path)) {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++) {
                    if (lines[i].Contains(OptionName + "=")) {
                        try {
                            return lines[i].Replace(OptionName + "=", "");
                        } catch { return fallbackvalue; }
                    }
                }
                return fallbackvalue;
            }
            return fallbackvalue;
        }

        public bool GetOption(string OptionName, bool fallbackvalue) {
            string val = GetOption(OptionName, fallbackvalue.ToString());
            try {
                return bool.Parse(val);
            } catch { }
            return fallbackvalue;
        }

        public float GetOption(string OptionName, float fallbackvalue) {
            string val = GetOption(OptionName, fallbackvalue.ToString());
            try {
                return float.Parse(val);
            } catch { }
            return fallbackvalue;
        }

        public int GetOption(string OptionName, int fallbackvalue) {
            string val = GetOption(OptionName, fallbackvalue.ToString());
            try {
                return int.Parse(val);
            } catch { }
            return fallbackvalue;
        }

        public short GetOption(string OptionName, short fallbackvalue) {
            string val = GetOption(OptionName, fallbackvalue.ToString());
            try {
                return short.Parse(val);
            } catch { }
            return fallbackvalue;
        }

    }
}