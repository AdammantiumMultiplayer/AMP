using System.Collections.Generic;
using ThunderRoad;

namespace AMP.Data {
    internal class DataReplacementFinder {
        internal static string FindItemReplacement(ItemData.Type category, string dataId) {
            string replacement;


            // Get first replacement match based on the category
            if(Config.itemCategoryReplacement.ContainsKey(category)) {
                replacement = Config.itemCategoryReplacement[category];
            } else {
                replacement = Config.itemCategoryReplacement[ItemData.Type.Misc];
            }


            dataId = dataId.ToLower();
            // Try to find better replacements based on the item name
            foreach(KeyValuePair<string, string> nameReplacement in Config.itemNameReplacement) {
                if(dataId.Contains(nameReplacement.Key)) {
                    replacement = nameReplacement.Value;
                    break;
                }
            }


            return replacement;
        }
    }
}
