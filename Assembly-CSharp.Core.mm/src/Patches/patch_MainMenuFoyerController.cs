﻿#pragma warning disable 0626
#pragma warning disable 0649

using System;
using UnityEngine;
using MonoMod;

namespace ModTheGungeon.CorePatches {
    [MonoModPatch("global::MainMenuFoyerController")]
    public class MainMenuFoyerController : global::MainMenuFoyerController {
        public static MainMenuFoyerController Instance = null;

        private float _orig_height;

        private extern void orig_Awake();
        protected void AddModVersions() {
            if (Instance == null) {
                Console.WriteLine($"SETTING INSTANCE TO {this}");
                Instance = this;
            }
            orig_Awake();

            _orig_height = VersionLabel.Height;

            VersionLabel.Text = $"Gungeon {VersionLabel.Text}";
            VersionLabel.Color = new Color32(255, 255, 255, 255);
            VersionLabel.Shadow = true;
            VersionLabel.ShadowOffset = new Vector2(1, -1);
            VersionLabel.ShadowColor = new Color32(0, 0, 0, 255);

            for (int i = 0; i < ModTheGungeon.Backend.AllBackends.Count; i++) {
                var backend = ModTheGungeon.Backend.AllBackends[i];
                AddLine($"{backend.Name} {backend.StringVersion}");
            }
        }

        public extern void orig_InitializeMainMenu();
        public new void InitializeMainMenu() {
            orig_InitializeMainMenu();
            if (!_Notified) EventHooks.InvokeMainMenuLoadedFirstTime(this);
            _Notified = true;
        }

        private bool _Notified = false;
        protected void core_Awake() {
            AddModVersions();
        }

        private void Awake() {
            core_Awake();
        }

        public void AddLine(string line) {
            if (VersionLabel.Text.Length > 0) {
                VersionLabel.Text += $"\n{line}";
            } else {
                VersionLabel.Text = line;
            }

            VersionLabel.Position = new UnityEngine.Vector3(VersionLabel.Position.x, _orig_height - VersionLabel.Height);
        }
    }
}