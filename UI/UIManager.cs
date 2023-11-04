using AMP.Extension;
using UnityEngine;

namespace AMP.UI {
    internal class UIManager : MonoBehaviour {

        private Canvas canvas;
        void Start () {
            canvas = gameObject.GetElseAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }

    }
}
