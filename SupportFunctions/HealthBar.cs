using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.SupportFunctions {
    public class HealthBar {

        const char fullBarCharacter = '█';
        const string fullBarColor = "00FF00";
        const char emptyBarCharacter = '▒';
        const string emptyBarColor = "FF0000";
        const int characterCount = 80;

        public static string calculateHealthBar(float percentage) { // percentage 0 -> 1
            string bar = $"<color=#{fullBarColor}>";
            bool switched = false;
            for(float i = 0; i < characterCount; i++) {
                if(percentage > i / characterCount) {
                    bar += fullBarCharacter;
                } else {
                    if(!switched) {
                        bar += $"</color><color=#{emptyBarColor}>";
                        switched = true;
                    }
                    bar += emptyBarCharacter;
                }
            }
            bar += "</color>";
            return bar;
        }

    }
}
