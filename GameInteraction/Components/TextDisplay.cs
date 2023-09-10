﻿using AMP.Network.Packets.Implementation;
using System.Collections.Concurrent;
using ThunderRoad;
using UnityEngine;

namespace AMP.GameInteraction.Components {
    internal class TextDisplay : ThunderBehaviour {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        private DisplayTextPacket data;
        private float timeTilDestroy;

        private TextMesh tm;
        void Awake() {
            tm = gameObject.AddComponent<TextMesh>();
        }

        void SetData(DisplayTextPacket data) {
            this.data = data;

            timeTilDestroy = data.displayTime;

            tm.text = data.text;
            tm.color = data.textColor;
            tm.fontSize = data.textSize;

            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.characterSize = 0.0025f;

            if(!data.relativeToPlayer) {
                transform.position = data.position;
            }
            if(!data.lookAtPlayer) {
                transform.eulerAngles = data.rotation;
            }
        }

        protected override void ManagedUpdate() {
            if(timeTilDestroy < 0) {
                if(textDisplays.ContainsKey(data.identifier)) textDisplays.TryRemove(data.identifier, out _);
                Destroy(gameObject);
            }
            else if(timeTilDestroy <= data.fadeTime) tm.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, timeTilDestroy / data.fadeTime));
            else if(timeTilDestroy + data.fadeTime > data.displayTime) tm.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, (data.displayTime - timeTilDestroy) / data.fadeTime));
            else tm.color = new Color(1, 1, 1, 1);

            timeTilDestroy -= Time.deltaTime;


            if(Player.currentCreature != null) {
                if(data.relativeToPlayer) {
                    transform.position = Player.currentCreature.ragdoll.headPart.transform.TransformPoint(data.position);
                }
                if(data.lookAtPlayer) {
                    transform.LookAt(2 * transform.position - Player.currentCreature.ragdoll.headPart.transform.position);
                }
            }
        }


        public static ConcurrentDictionary<string, TextDisplay> textDisplays = new ConcurrentDictionary<string, TextDisplay>();
        public static void ShowTextDisplay(DisplayTextPacket data) {
            TextDisplay textDisplay = null;
            if(textDisplays.ContainsKey(data.identifier)) {
                textDisplay = textDisplays[data.identifier];
            }
            if(textDisplay == null) {
                textDisplay = new GameObject().AddComponent<TextDisplay>();
                if(textDisplays.ContainsKey(data.identifier)) {
                    textDisplays[data.identifier] = textDisplay;
                } else {
                    textDisplays.TryAdd(data.identifier, textDisplay);
                }
            }

            textDisplay.SetData(data);
        }

        public static void ClearText() {
            foreach(TextDisplay display in textDisplays.Values) {
                if(display != null && display.gameObject != null) {
                    Destroy(display.gameObject);
                }
            }
            textDisplays.Clear();
        }
    }
}
