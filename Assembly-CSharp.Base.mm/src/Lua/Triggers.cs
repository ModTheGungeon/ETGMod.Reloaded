using System;
using UnityEngine;
using MicroLua;

namespace ModTheGungeon.Lua {
    public class TriggerContainer : IDisposable {
        public ModLoader.ModInfo Info;
       
        public int MainMenuLoadedFirstTimeRef = -1;
        public int UnloadedRef = -1;

        private Action<MainMenuFoyerController> _MainMenuLoadedFirstTime;

        public void InvokeUnloaded() {
            if (UnloadedRef == -1) return;

            ModTheGungeon.ModLoader.LuaState.EnterArea();
            ModTheGungeon.ModLoader.LuaState.BeginProtCall();
            ModTheGungeon.ModLoader.LuaState.PushLuaReference(UnloadedRef);
            ModTheGungeon.ModLoader.LuaState.ExecProtCallVoid(0, cleanup: true);
            ModTheGungeon.ModLoader.LuaState.LeaveAreaCleanup();
        }

        public void InvokeMainMenuLoadedFirstTime() {
            if (MainMenuLoadedFirstTimeRef == -1) return;

            ModTheGungeon.ModLoader.LuaState.EnterArea();
            ModTheGungeon.ModLoader.LuaState.BeginProtCall();
            ModTheGungeon.ModLoader.LuaState.PushLuaReference(MainMenuLoadedFirstTimeRef);
            ModTheGungeon.ModLoader.LuaState.ExecProtCallVoid(0, cleanup: true);
            ModTheGungeon.ModLoader.LuaState.LeaveAreaCleanup();
        }

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
            var lua = ModTheGungeon.ModLoader.LuaState;

            Info = info;
            lua.PushLuaReference(env_ref);
            _Trigger(lua, "MainMenuLoadedFirstTime", ref MainMenuLoadedFirstTimeRef);
            _Trigger(lua, "Unloaded", ref UnloadedRef);
            lua.Pop();
        }

        public void Dispose() {
            ModTheGungeon.ModLoader.LuaState.DeleteLuaReference(MainMenuLoadedFirstTimeRef);
            ModTheGungeon.ModLoader.LuaState.DeleteLuaReference(UnloadedRef);

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
    