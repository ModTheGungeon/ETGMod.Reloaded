#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;

namespace ModTheGungeon.BasePatches {
    [MonoModPatch("global::MainMenuFoyerController")]
    public class MainMenuFoyerController : global::MainMenuFoyerController {
        public extern void AddLine(string s);

        public void Start() {
            var word = ModTheGungeon.ModLoader.LoadedMods.Count == 1 ? "mod" : "mods";
            AddLine($"{ModTheGungeon.ModLoader.LoadedMods.Count} {word} loaded");
        }

    }
}