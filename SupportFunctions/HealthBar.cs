namespace AMP.SupportFunctions {
    public class HealthBar {

        // https://unicode-table.com/de/blocks/block-elements/
        private const char fullBarCharacter = '█';
        private const string fullBarColor = "00FF00";
        private const char emptyBarCharacter = '█';
        private const string emptyBarColor = "FF0000";
        private const int characterCount = 80;

        public static string calculateHealthBar(float percentage) {
            return calculateHealthBar(percentage, fullBarCharacter, fullBarColor, emptyBarCharacter, emptyBarColor, characterCount);
        }

        public static string calculateHealthBar(float percentage, char fullBarCharacter, char emptyBarCharacter, int characterCount) {
            return calculateHealthBar(percentage, fullBarCharacter, fullBarColor, emptyBarCharacter, emptyBarColor, characterCount);
        }
        
        public static string calculateHealthBar(float percentage, char fullBarCharacter, string fullBarColor, char emptyBarCharacter, string emptyBarColor, int characterCount) { // percentage 0 -> 1
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
