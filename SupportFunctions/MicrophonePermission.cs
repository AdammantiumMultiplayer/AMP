using AMP.Data;
using AMP.Logging;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AMP.SupportFunctions {
    /// <summary>
    /// On Android the microphone needs a permission. Without it, accessing the microphone breaks the mod loading,
    /// so everything using the microphone (voice chat, microphone test) has to check this first.
    /// </summary>
    internal static class MicrophonePermission {

        internal static bool IsGranted {
            get {
                if(Application.platform != RuntimePlatform.Android) return true;
                return HasAndroidPermission();
            }
        }

        // Separate method, so the Android API only gets touched on Android
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool HasAndroidPermission() {
            try {
                return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
            } catch(Exception e) {
                Log.Err(Defines.AMP, $"Couldn't check the microphone permission, voice chat stays disabled: {e}");
                return false;
            }
        }

        private static bool loggedMissing = false;
        // Logs the missing permission once, so the log doesn't get spammed by every option
        internal static bool CheckAndLog() {
            bool granted = IsGranted;
            if(!granted && !loggedMissing) {
                loggedMissing = true;
                Log.Warn(Defines.AMP, "Microphone permission is missing, voice chat is disabled. Allow the microphone for the game and restart it to use voice chat.");
            }
            return granted;
        }
    }
}
