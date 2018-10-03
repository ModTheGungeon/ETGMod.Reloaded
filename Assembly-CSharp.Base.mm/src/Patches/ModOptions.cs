#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;
using ETGMod.Tools;
using UnityEngine;

namespace ETGMod.BasePatches {
    [MonoModPatch("global::PreOptionsMenuController")]
    public class PreOptionsMenuController : global::PreOptionsMenuController {
        public dfButton TabETGModSelector;
        private dfPanel m_panel;

        private extern void orig_Awake();
        private void Awake() {
            orig_Awake();
            TabETGModSelector = Instantiate(TabGameplaySelector);
            TabETGModSelector.Text = "MODS";
            m_panel.AddControl(TabETGModSelector);

            TabETGModSelector.Position = new Vector3(TabETGModSelector.Position.x, TabGameplaySelector.Position.y - 42.75f, 0);
            TabControlsSelector.Position = new Vector3(TabControlsSelector.Position.x, TabETGModSelector.Position.y - 42.75f, 0);
            TabVideoSelector.Position = new Vector3(TabVideoSelector.Position.x, TabControlsSelector.Position.y - 42.75f, 0);
            TabAudioSelector.Position = new Vector3(TabAudioSelector.Position.x, TabVideoSelector.Position.y - 42.75f, 0);
            TabETGModSelector.Click += delegate (dfControl control, dfMouseEventArgs mouseEvent) {
                ToggleToPanel(((FullOptionsMenuController)FullOptionsMenu).TabETGMod, false, false);
            };
        }

        bool dumped = false;
        private extern void orig_Update();
        private void Update() {
            orig_Update();
            if (m_panel != null && dumped == false) {
                dumped = true;
                Console.WriteLine(ObjectDumper.Dump(m_panel, 10));
            }
        }
    }

    [MonoModPatch("global::FullOptionsMenuController")]
    public class FullOptionsMenuController : global::FullOptionsMenuController {
        private dfControl m_lastSelectedBottomRowControl;

        public dfScrollPanel TabETGMod;

        private extern void orig_Awake();
        private void Awake() {
            orig_Awake();
            TabETGMod = Instantiate(TabControls);
        }

        public extern void orig_ToggleToPanel(dfScrollPanel targetPanel, bool doFocus = false);
        public void ToggleToPanel(dfScrollPanel targetPanel, bool doFocus = false) {
            TabETGMod.IsVisible = (targetPanel == TabETGMod);
            orig_ToggleToPanel(targetPanel, doFocus);
        }

        private void BottomOptionFocused(dfControl control, dfFocusEventArgs args) {
            m_lastSelectedBottomRowControl = control;
            if (TabAudio.IsVisible) {
                TabAudio.Controls[TabAudio.Controls.Count - 1].GetComponent<BraveOptionsMenuItem>().down = m_lastSelectedBottomRowControl;
            } else if (TabVideo.IsVisible) {
                TabVideo.Controls[TabVideo.Controls.Count - 1].GetComponent<BraveOptionsMenuItem>().down = m_lastSelectedBottomRowControl;
            } else if (TabControls.IsVisible) {
                TabControls.Controls[TabControls.Controls.Count - 2].GetComponent<BraveOptionsMenuItem>().down = m_lastSelectedBottomRowControl;
            } else if (TabGameplay.IsVisible) {
                TabGameplay.Controls[TabGameplay.Controls.Count - 1].GetComponent<BraveOptionsMenuItem>().down = m_lastSelectedBottomRowControl;
            } else if (TabETGMod.IsVisible) {
                TabETGMod.Controls[TabETGMod.Controls.Count - 2].GetComponent<BraveOptionsMenuItem>().down = m_lastSelectedBottomRowControl;
            } else if (TabKeyboardBindings.IsVisible) {
                TabKeyboardBindings.Controls[TabKeyboardBindings.Controls.Count - 1].GetComponent<KeyboardBindingMenuOption>().KeyButton.GetComponent<UIKeyControls>().down = m_lastSelectedBottomRowControl;
                TabKeyboardBindings.Controls[TabKeyboardBindings.Controls.Count - 1].GetComponent<KeyboardBindingMenuOption>().AltKeyButton.GetComponent<UIKeyControls>().down = m_lastSelectedBottomRowControl;
            }
        }

        public void ToggleToETGMod() {
            this.TabGameplay.IsVisible = false;
            this.TabCredits.IsVisible = true;
            this.TabCredits.Controls[0].Focus(true);
        }
    }
}