using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.Data {
    public static class Config {

        public static readonly ItemData.Type[] ignoredTypes = {
            ItemData.Type.Body,
            ItemData.Type.Spell,
            ItemData.Type.Prop,
            ItemData.Type.Wardrobe
        };

        public static int TICK_RATE = 30;

    }
}
