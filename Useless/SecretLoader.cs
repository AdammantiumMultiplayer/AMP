using AMP.Logging;
using System.Drawing.Imaging;
using System.IO;
using ThunderRoad;
using UnityEngine;

namespace AMP.Useless {
    public class SecretLoader {

        private static Sprite owlSprite = null;

        public static void DoLevelStuff() {
            UIMap[] maps = Object.FindObjectsOfType<UIMap>();
            loadOwlSprite();
            foreach(UIMap map in maps) {
                SpriteRenderer sr = new GameObject("Owl").AddComponent<SpriteRenderer>();
                sr.transform.position = map.transform.position;
                sr.transform.rotation = map.transform.rotation;
                sr.transform.Rotate(0, 0, -90);
                sr.sprite = owlSprite;
                sr.transform.localScale = Vector3.one * 0.03f;
                sr.transform.Translate(new Vector3(-0.4f, -0.4f, 0));
                sr.color = new Color(1, 1, 1, 0.5f);
            }
        }

        // Read the owl texture from dll bytes
        private static void loadOwlSprite() {
            if(owlSprite == null) {
                Texture2D tex2d = new Texture2D(Properties.Resources.OwlCookie.Width, Properties.Resources.OwlCookie.Height);

                MemoryStream memoryStream = new MemoryStream();
                Properties.Resources.OwlCookie.Save(memoryStream, ImageFormat.Png);
                ImageConversion.LoadImage(tex2d, memoryStream.ToArray());

                owlSprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), Vector2.one / 2);
            }
        }

    }
}
