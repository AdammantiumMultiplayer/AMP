namespace AMP.SupportFunctions {
    internal class HealthBar {

        // https://unicode-table.com/de/blocks/block-elements/
        private const char fullBarCharacter = '█';
        private const string fullBarColor = "00FF00";
        private const char emptyBarCharacter = '█';
        private const string emptyBarColor = "FF0000";
        private const int characterCount = 80;

        internal static string calculateHealthBar(float percentage) { // percentage 0 -> 1
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
