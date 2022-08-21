using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.SupportFunctions {
    public class Clipboard {
        public static void SendToClipboard(string text) {
            TextEditor te = new TextEditor();
            te.content = new GUIContent(text);
            te.SelectAll();
            te.Copy();
        }

        public static string ReceiveFromClipboard() {
            TextEditor te = new TextEditor();
            te.content = new GUIContent("");
            te.SelectAll();
            te.Paste();
            return te.content.text;
        }
    }
}
