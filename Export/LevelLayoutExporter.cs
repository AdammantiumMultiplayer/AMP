﻿using AMP.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AMP.Export {
    internal class LevelLayoutExporter {

        internal static void Export() {
            string log = "";
            for(int i = 0; i < SceneManager.sceneCount; i++) {
                Scene scene = SceneManager.GetSceneAt(i);
                GameObject[] gos = scene.GetRootGameObjects();
            
                log += "SCENE: " + scene.name;
                foreach(GameObject go in gos) {
                    log += LogLine(go, "");
                }
            }
            Log.Debug(log);
            //File.WriteAllText("C:\\Users\\mariu\\Desktop\\log.txt", log);
        }

        internal static string LogLine(GameObject go, string prefix) {
            string compLine = "";
            UnityEngine.Component[] components = go.gameObject.GetComponents(typeof(UnityEngine.Component));
            foreach(UnityEngine.Component component in components) {
                compLine += " " + component.ToString();
            }

            string logLine = prefix + go.name + " <" + go.GetType().Name + "> [" + go.activeInHierarchy + "] " + compLine + " | Layer: " + go.layer + " | Tag: " + go.tag + "\n";

            prefix += "-";
            for(int i = 0; i < go.transform.childCount; i++) {
                logLine += LogLine(go.transform.GetChild(i).gameObject, prefix);
            }
            return logLine;
        }

    }
}
