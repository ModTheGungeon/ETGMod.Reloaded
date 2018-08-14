using System;
using System.Collections.Generic;
using System.Reflection;
using ETGMod.Lua;
using MicroLua;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo : IDisposable {
            public Logger Logger = new Logger("Unnamed Mod");
            private string _NameOverride;

            public ModInfo Parent;

            public string Name {
                get {
                    if (_NameOverride == null) return ModMetadata.Name;
                    return _NameOverride;
                }
                internal set {
                    _NameOverride = value;
                    Logger.ID = value;
                }
            }
            public List<ModInfo> EmbeddedMods {
                get;
                internal set;
            } = new List<ModInfo>();

            public int LuaEnvironmentRef;
            internal int RealPackageTableRef;

            public TriggerContainer Triggers;
            public HookManager Hooks;

            private Metadata _ModMetadata;
            public Metadata ModMetadata {
                get {
                    return _ModMetadata;
                }
                internal set {
                    _ModMetadata = value;
                    if (_NameOverride == null) {
                        Logger.ID = value.Name;
                    }
                }
            }

            public string RealPath {
                get;
                internal set;
            }

            public string Path {
                get;
                internal set;
            }

            public string ScriptPath {
                get;
                internal set;
            }

            public bool HasScript {
                get {
                    return ScriptPath != null;
                }
            }

            public bool HasAnyEmbeddedMods {
                get {
                    return EmbeddedMods.Count > 0;
                }
            }

            public bool IsComplete {
                get {
                    return RealPath != null && Path != null && ModMetadata != null;
                }
            }

            public void RunLua(int func_ref, string name = "[unknown]", object[] args = null) {
                var lua = ETGMod.ModLoader.LuaState;

                try {
                    lua.BeginProtCall();
                    lua.PushLuaReference(func_ref);
                    lua.PushLuaReference(LuaEnvironmentRef);
                    lua.SetEnvironment();
                    if (args != null) {
                        for (int i = 0; i < args.Length; i++) {
                            lua.PushCLR(args);
                        }
                    }
                    lua.ExecProtCall(args?.Length ?? 0);
                } catch (Exception e) {
                    ETGMod.ModLoader.LuaError.Invoke(this, LuaEventMethod.Loaded, e);

                    Logger.Error(e.ToString());
                }
            }

            public void Dispose() {
                ETGMod.ModLoader.LuaState.DeleteLuaReference(LuaEnvironmentRef);
                ETGMod.ModLoader.LuaState.DeleteLuaReference(RealPackageTableRef);
                Triggers?.Dispose();
                Hooks?.Dispose();
            }
        }
    }
}