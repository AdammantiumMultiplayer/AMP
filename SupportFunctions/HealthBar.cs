namespace AMP.SupportFunctions {
    internal class HealthBar {

        // https://unicode-table.com/de/blocks/block-elements/
        private const char fullBarCharacter = '█';
        private const string fullBarColor = "00FF00";
        private const char emptyBarCharacter = '█';
        private const string emptyBarColor = "FF0000";
        private const int characterCount = 80;

        internal static string calculateHealthBar(float percentage) { // percentage 0 -> 1
            string bar = "";

            int fullCharacters = (int) (characterCount * percentage);
            int emptyCharacters = characterCount - fullCharacters;

            if(fullCharacters > 0) {
                bar += $"<color=#{fullBarColor}>" + "".PadLeft(fullCharacters, fullBarCharacter) + "</color>";
            }
            if(emptyCharacters > 0) {
                bar += $"<color=#{emptyBarColor}>" + "".PadLeft(emptyCharacters, emptyBarCharacter) + "</color>";
            }

            return bar;
        }

    }
}
