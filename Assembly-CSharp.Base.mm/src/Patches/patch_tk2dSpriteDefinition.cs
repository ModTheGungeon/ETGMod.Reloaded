#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;

namespace ModTheGungeon.BasePatches {
    // hacky stuff for passing the scale and region of a parsed definition

    [MonoModPatch("global::tk2dSpriteDefinition")]
    public class tk2dSpriteDefinition : global::tk2dSpriteDefinition {
        public int ModTheGungeonOffsetX {
            get {
                return (int)position0.x;
            }
        }

        public int ModTheGungeonOffsetY {
            get {
                return (int)position0.y;
            }
        }

        public int ModTheGungeonCropWidth {
            get {
                return regionW;
            }
        }

        public int ModTheGungeonCropHeight {
            get {
                return regionH;
            }
        }

        public int ModTheGungeonCropX {
            get {
                return regionX;
            }
        }

        public int ModTheGungeonCropY {
            get {
                return regionY;
            }
        }

        public float ModTheGungeonScaleW {
            get {
                var w = position3.x - ModTheGungeonOffsetX;
                return w * 16f / regionW;
            }
        }

        public float ModTheGungeonScaleH {
            get {
                var w = position3.y - ModTheGungeonOffsetY;
                return w * 16f / regionH;
            }
        }

        public float ModTheGungeonScaledWidth {
            get {
                return (position3.x - ModTheGungeonOffsetX) * 16f;
            }
        }

        public float ModTheGungeonScaledHeight {
            get {
                return (position3.y - ModTheGungeonOffsetY) * 16f;
            }
        }
    }
}