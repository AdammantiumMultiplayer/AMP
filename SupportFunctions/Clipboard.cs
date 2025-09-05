using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
namespace AMP.SupportFunctions {
    internal class Clipboard {
        internal static void SendToClipboard(string text) {
            TextEditor te = new TextEditor();
            te.text = text;
            te.SelectAll();
            te.Copy();
        }

        internal static string ReceiveFromClipboard() {
            TextEditor te = new TextEditor();
            te.text = "";
            te.SelectAll();
            te.Paste();
            return te.text;
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
