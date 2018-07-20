#pragma warning disable 0626
#pragma warning disable 0649

using System;
using ETGMod;
using UnityEngine;
using System.Reflection;
using MonoMod;

namespace ETGMod.CorePatches {
    [MonoModPatch("global::GameManager")]
    internal class GameManager : global::GameManager {
        protected extern void orig_Awake();
        private void Awake() {
            Loader.Logger.Info("GameManager is alive");

            for (int i = 0; i < Backend.AllBackends.Count; i++) {
                var backend = Backend.AllBackends[i];
                Loader.Logger.Info($"Running PreGameManagerAlive on backend {backend.Name} {backend.StringVersion}");
                try {
                    backend.Instance.PreGameManagerAlive();
                } catch (Exception e) {
                    Loader.Logger.Error($"Exception while running PreGameManagerAlive on backend {backend.Name}: [{e.GetType().Name}] {e.Message}");
                    foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                }
            }

            for (int i = 0; i < Backend.AllBackends.Count; i++) {
                var backend = Backend.AllBackends[i];
                Loader.Logger.Info($"Running GameManagerAlive on backend {backend.Name} {backend.StringVersion}");
                try {
                    backend.Instance.GameManagerAlive();
                } catch (Exception e) {
                    Loader.Logger.Error($"Exception while running GameManagerAlive on backend {backend.Name}: [{e.GetType().Name}] {e.Message}");
                    foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                }
            }

            orig_Awake();
        }
    }
}