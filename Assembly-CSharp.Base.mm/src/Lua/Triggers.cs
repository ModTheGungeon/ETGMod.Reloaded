using System;
using UnityEngine;
using MicroLua;

namespace ETGMod.Lua {
    public class TriggerContainer : IDisposable {
        public ModLoader.ModInfo Info;
       
        public int MainMenuLoadedFirstTimeRef = 0;
        public int UnloadedRef = 0;

        private Action<MainMenuFoyerController> _MainMenuLoadedFirstTime;

        private void _Trigger(LuaState lua, string name, ref int func) {
            lua.GetField(name);
            if (lua.Type() != LuaType.Nil) {
                if (lua.Type() == LuaType.Function) {
                    func = lua.MakeLuaReference();
                }
            }
            lua.Pop();
        }

        public TriggerContainer(int env_ref, ModLoader.ModInfo info) {
            var lua = ETGMod.ModLoader.LuaState;

            Info = info;
            lua.PushLuaReference(env_ref);
            _Trigger(lua, "MainMenuLoadedFirstTime", ref MainMenuLoadedFirstTimeRef);
            _Trigger(lua, "Unloaded", ref UnloadedRef);
            lua.Pop();
        }

        public void Dispose() {
            ETGMod.ModLoader.LuaState.DeleteLuaReference(MainMenuLoadedFirstTimeRef);
            ETGMod.ModLoader.LuaState.DeleteLuaReference(UnloadedRef);

            RemoveExternalHooks();
        }

        public void RemoveExternalHooks() {
            if (_MainMenuLoadedFirstTime != null) EventHooks.MainMenuLoadedFirstTime -= _MainMenuLoadedFirstTime;

            _MainMenuLoadedFirstTime = null;
        }

        public void SetupExternalHooks() {
            if (MainMenuLoadedFirstTimeRef != -1 && _MainMenuLoadedFirstTime == null) {
                _MainMenuLoadedFirstTime = (main_menu) => {
                    Info.RunLua(MainMenuLoadedFirstTimeRef, "Events.MainMenuLoadedFirstTime", new object[] { main_menu });
                };
                EventHooks.MainMenuLoadedFirstTime += _MainMenuLoadedFirstTime;
            }
        }
    }
}
    