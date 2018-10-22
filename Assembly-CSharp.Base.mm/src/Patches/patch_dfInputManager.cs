#pragma warning disable 0626
#pragma warning disable 0649

using System;
using SGUI;
using MonoMod;

namespace ModTheGungeon.GUI.Patches {
    [MonoModPatch("global::dfInputManager")]
    public class dfInputManager : global::dfInputManager {
        bool _ModTheGungeon_sgui_patched = false;

        public extern void orig_OnEnable();
        public new void OnEnable() {
            orig_OnEnable();

            if (_ModTheGungeon_sgui_patched) return;
            _ModTheGungeon_sgui_patched = true;

            GUI.Logger.Debug($"Patching dfInputManager adapter with SGUIDFInput");
            Adapter = new SGUIDFInput(Adapter);
        }
    }
}