using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using YamlDotNet.Serialization;
using Mono.Cecil;
using System.Security.Cryptography;
using System.Linq;
using MicroLua;
using ModTheGungeon.Lua;

namespace ModTheGungeon {
    public partial class ModLoader {
        public static Logger Logger = new Logger("ModLoader");
        private static ModuleDefinition _AssemblyCSharpModuleDefinition = ModuleDefinition.ReadModule(typeof(WingsItem).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));
        private static ModuleDefinition _UnityEngineModuleDefinition = ModuleDefinition.ReadModule(typeof(GameObject).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));

        const string METADATA_FILE_NAME = "mod.yml";

        public LuaState LuaState { get; internal set; }

        public Deserializer Deserializer = new DeserializerBuilder().Build();
        public string CachePath;
        public string ModsPath;
        public GameObject GameObject;

        public string RelinkCachePath;
        public string UnpackCachePath;

        public List<ModInfo> LoadedMods = new List<ModInfo>();

        public enum LuaEventMethod {
            Loaded,
            Unloaded
        }

        public Action<ModInfo> PostLoadMod = (obj) => { };
        public Action<ModInfo> PostUnloadMod = (obj) => { };
        public Action<ModInfo, LuaEventMethod, Exception> LuaError = (obj, method, ex) => { };

        private Dictionary<string, ModuleDefinition> _AssemblyRelinkMap;
        public Dictionary<string, ModuleDefinition> AssemblyRelinkMap {
            get {
                if (_AssemblyRelinkMap != null) return _AssemblyRelinkMap;

                _AssemblyRelinkMap = new Dictionary<string, ModuleDefinition>();

                var entries = Directory.GetFileSystemEntries(Paths.ManagedFolder);

                for (int i = 0; i < entries.Length; i++) {
                    var full_entry = entries[i];
                    var entry = Path.GetFileName(full_entry);

                    if (full_entry.EndsWithInvariant(".mm.dll")) {
                        if (entry.StartsWithInvariant("Assembly-CSharp.")) {
                            _AssemblyRelinkMap[entry.RemoveSuffix(".dll")] = _AssemblyCSharpModuleDefinition;
                        } else if (entry.StartsWithInvariant("UnityEngine.")) {
                            _AssemblyRelinkMap[entry.RemoveSuffix(".dll")] = _UnityEngineModuleDefinition;
                        } else {
                            Logger.Debug($"Found MonoMod patch assembly {entry}, but it's neither a patch for Assembly-CSharp nor UnityEngine. Ignoring.");
                        }
                    }
                }

                return _AssemblyRelinkMap;
            }
        }


        public ModLoader(string modspath, string cachepath) {
            CachePath = cachepath;
            UnpackCachePath = Path.Combine(cachepath, "Unpack");
            RelinkCachePath = Path.Combine(cachepath, "Relink");
            ModsPath = modspath;
            GameObject = new GameObject("Mod the Gungeon Mod Loader");
            RefreshLuaState();
        }

        private int _CreateNewEnvironment(ModInfo info) {
            LuaState.EnterArea();

            LuaState.PushCLR(info);
            LuaState.SetGlobal("MOD");

            LuaState.GetGlobal("package");
            LuaState.GetField("path");
            var prev_path = LuaState.ToString();
            LuaState.Pop();
            LuaState.PushString(Path.Combine(Paths.ResourcesFolder, "lua/?.lua"));
            LuaState.SetField("path");
            LuaState.Pop();

            LuaState.BeginProtCall();
            LuaState.LoadFile(Path.Combine(Paths.ResourcesFolder, "lua/env.lua"));
            LuaState.ExecProtCall(0, cleanup: true);

            var env_ref = LuaState.MakeLuaReference();
            LuaState.Pop();

            LuaState.GetGlobal("package");
            LuaState.PushString(prev_path);
            LuaState.SetField("path");
            LuaState.Pop();

            LuaState.PushNil();
            LuaState.SetGlobal("MOD");

            LuaState.LeaveArea();
            return env_ref;
        }

        private void _SetupSandbox(int env_ref) {
            LuaState.EnterArea();

            LuaState.BeginProtCall();
            LuaState.BeginProtCall();
            LuaState.LoadFile(Path.Combine(Paths.ResourcesFolder, "lua/sandbox.lua"));
            LuaState.ExecProtCall(0, cleanup: true);
            LuaState.PushLuaReference(env_ref);
            LuaState.ExecProtCall(1, cleanup: true);

            LuaState.LeaveAreaCleanup();
        }

        internal void RefreshLuaState() {
            if (LuaState != null) LuaState.Dispose();
            LuaState = new LuaState();
            LuaState.LoadInteropLibrary();
        }

        public ModInfo Load(string path) {
            return Load(path, null);
        }

        private ModInfo Load(string path, ModInfo parent) {
            ModInfo mod;
            if (Directory.Exists(path)) {
                mod = _LoadFromDir(path);
            } else if (path.EndsWithInvariant(".zip")) {
                mod = _LoadFromZip(path);
            } else {
                throw new InvalidOperationException($"Mod type not suppored: {path}");
            }

            if (parent == null) LoadedMods.Add(mod);
            else {
                parent.EmbeddedMods.Add(mod);
                mod.Parent = parent;
                mod.Name = $"[{parent.ModMetadata.Name}] {mod.ModMetadata.Name}";
            }

            if (mod.ModMetadata.IsModPack) {
                _HandleModPack(mod);
            }

            if (mod.ModMetadata.HasScript && !File.Exists(Path.Combine(path, mod.ModMetadata.Script))) {
                throw new FileNotFoundException($"{mod.ModMetadata.Script} doesn't exist in unpacked mod directory {path}");
            }

            if (mod.ModMetadata.HasScript) {
                Logger.Debug($"Mod has script ({mod.ModMetadata.Script}), running");
                _RunModScript(mod);
            }

            PostLoadMod.Invoke(mod);

            if (!mod.IsComplete) throw new InvalidOperationException($"Tried to return incomplete ModInfo when loading {path}");
            return mod;
        }

        private static int _FakePackageNewIndex(LuaState lua) {
            throw new LuaException("I'm sorry, Dave.");
        }

        private static int _FakePackageMetatable(LuaState lua) {
            lua.PushString("I'm afraid I can't let you do that.");
            return 1;
        }

        private void _RunModScript(ModInfo info, ModInfo parent = null) {
            LuaState.EnterArea();

            info.ScriptPath = Path.Combine(info.RealPath, info.ModMetadata.Script);
            Logger.Info($"Running Lua script at '{info.ScriptPath}'");

            if (parent != null) info.Parent = parent;

            info.Hooks = new HookManager();

            var env_ref = _CreateNewEnvironment(info);

            LuaState.LoadFile(info.ScriptPath);
            var mod_func_ref = LuaState.MakeLuaReference();

            LuaState.PushNewTable();
            info.RealPackageTableRef = LuaState.MakeLuaReference();
            LuaState.Pop();

            LuaState.PushLuaReference(mod_func_ref);
            LuaState.PushLuaReference(env_ref);
            LuaState.SetEnvironment();
            LuaState.Pop();

            // Setup the metatable
            LuaCLRFunction fake_package_index = (lua) => {
                lua.PushLuaReference(info.RealPackageTableRef);
                lua.PushValue(2);
                lua.GetField();
                return 1;
            };

            LuaState.PushNewTable(); // fake package
            LuaState.PushNewTable(); // mt
            LuaState.PushLuaCLRFunction(fake_package_index);
            LuaState.SetField("__index");
            LuaState.PushLuaCLRFunction(_FakePackageNewIndex);
            LuaState.SetField("__newindex");
            LuaState.PushLuaCLRFunction(_FakePackageMetatable);
            LuaState.SetField("__metatable");
            LuaState.SetMetatable(); // setmetatable(fake_package, mt)
            LuaState.Pop(); // pop fake_package

            LuaState.PushLuaReference(info.RealPackageTableRef);
            LuaState.PushNewTable();
            LuaState.SetField("loaded");
            LuaState.PushString(Path.Combine(Paths.ResourcesFolder, "lua/libs/?.lua") + ";" + Path.Combine(Paths.ResourcesFolder, "lua/libs/?/init.lua") + ";" + Path.Combine(Paths.ResourcesFolder, "lua/libs/?/?.lua") + ";" + info.RealPath + "/?.lua");
            LuaState.SetField("path");
            LuaState.PushString("Really makes you think");
            LuaState.SetField("cpath");
            LuaState.SetGlobal("package");

            info.LuaEnvironmentRef = env_ref;
            info.RunLua(mod_func_ref, "the main script");

            info.Triggers = new TriggerContainer(info.LuaEnvironmentRef, info);
            info.Triggers.SetupExternalHooks();

            LuaState.LeaveAreaCleanup(); // I am lazy
        }

        private string _HashPath(string path) {
            using (var md5 = MD5.Create()) {
                return Convert.ToBase64String(
                    md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(path))
                ).Replace('/', '_');
            }
        }

        private byte[] _HashContent(string inpath) {
            using (var infile = File.OpenRead(inpath)) {
                using (var md5 = MD5.Create()) {
                    return md5.ComputeHash(infile);
                }
            }
        }

        private void _HashContentIntoFile(string inpath, string outpath) {
            using (var outfile = File.OpenWrite(outpath)) {
                var checksum = _HashContent(inpath);

                outfile.Write(checksum, 0, checksum.Length);
            }

        }

        private string _PrepareZipUnpackPath(string zippath) {
            if (!Directory.Exists(UnpackCachePath)) Directory.CreateDirectory(UnpackCachePath);
            var hashedpath = _HashPath(zippath);
            var hashedcontent = _HashContent(zippath);
            var moddir = Path.Combine(UnpackCachePath, $"{hashedpath}.unpack");
            var modchecksum = Path.Combine(UnpackCachePath, $"{hashedpath}.sum");

            if (!File.Exists(modchecksum) || !File.ReadAllBytes(modchecksum).SequenceEqual(hashedcontent)) {
                Logger.Debug($"[{zippath}] ZIP unpack checksum doesn't match or doesn't exist, updating");

                using (var file = File.OpenWrite(modchecksum)) {
                    file.Write(hashedcontent, 0, hashedcontent.Length);
                }

                if (Directory.Exists(moddir)) Directory.Delete(moddir, recursive: true);
                Directory.CreateDirectory(moddir);
            }

            return moddir;
        }

        private void _Relink(string input, string output) {
            using (var modder = new MonoModder() {
                InputPath = input,
                OutputPath = output
            }) {
                modder.CleanupEnabled = false;

                modder.RelinkModuleMap = AssemblyRelinkMap;

                modder.ReaderParameters.ReadSymbols = false;
                modder.WriterParameters.WriteSymbols = false;
                modder.WriterParameters.SymbolWriterProvider = null;

                modder.Read();
                modder.MapDependencies();
                modder.AutoPatch();
                modder.Write();
            }
        }

        private string _PrepareRelinkPath(string asmpath) {
            if (!Directory.Exists(RelinkCachePath)) Directory.CreateDirectory(RelinkCachePath);
            var hashedpath = _HashPath(asmpath);
            var hashedcontent = _HashContent(asmpath);
            var asm = Path.Combine(RelinkCachePath, $"{hashedpath}.dll");
            var checksum = Path.Combine(RelinkCachePath, $"{hashedpath}.sum");

            if (!File.Exists(checksum) || !File.ReadAllBytes(checksum).SequenceEqual(hashedcontent)) {
                Logger.Debug($"[{asmpath}] Relink checksum doesn't match or doesn't exist, updating");

                using (var file = File.OpenWrite(checksum)) {
                    file.Write(hashedcontent, 0, hashedcontent.Length);
                }

                _Relink(asmpath, output: asm);
            }

            if (!File.Exists(asm)) {
                Logger.Debug("Relinked checksum exists and is valid, but the assembly doesn't exist (probably crashed while relinking) - invalidating checksum");
                File.Delete(checksum);
                return _PrepareRelinkPath(asmpath);
            }

            return asm;
        }

        public void Unload(ModInfo info) {
            UnloadAll(info.EmbeddedMods);

            Logger.Info($"Unloading mod {info.Name}");
            if (info.HasScript) {
                try {
                    info.Triggers.InvokeUnloaded();
                } catch (LuaException e) {
                    Logger.Error(e.Message);
                    LuaError.Invoke(info, LuaEventMethod.Unloaded, e);

                    for (int i = 0; i < e.TracebackArray.Length; i++) {
                        Logger.ErrorIndent("  " + e.TracebackArray[i]);
                    }
                }
            }

            info.Dispose();

            info.EmbeddedMods = new List<ModInfo>();
            PostUnloadMod.Invoke(info);
        }

        public void UnloadAll(List<ModInfo> list) {
            for (int i = 0; i < list.Count; i++) {
                var info = list[i];
                Unload(info);
            }
        }

        public void UnloadAll() {
            UnloadAll(LoadedMods);
            LoadedMods = new List<ModInfo>();
        }

        private ModInfo _LoadFromZip(string path) {
            string unpacked_path = _PrepareZipUnpackPath(path);
            using (var zip = System.IO.Compression.ZipStorer.Open(path, FileAccess.Read)) {
                var dir = zip.ReadCentralDir();

                for (int i = 0; i < dir.Count; i++) {
                    var entry = dir[i];

                    var dirname = Path.GetDirectoryName(entry.FilenameInZip);
                    var outdir = Path.Combine(unpacked_path, dirname);
                    if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);

                    zip.ExtractFile(entry, Path.Combine(unpacked_path, entry.FilenameInZip));
                }
            }

            return _LoadFromDir(unpacked_path, new ModInfo.Metadata {
                Name = Path.GetFileNameWithoutExtension(path)
            }, path);
        }

        private void _HandleModPack(ModInfo info) {
            var modpackpath = Path.Combine(info.RealPath, info.ModMetadata.ModPackDir);
            if (!Directory.Exists(modpackpath)) {
                Logger.Error($"Mod pack folder {info.ModMetadata.ModPackDir} doesn't exist (in {info.RealPath}). Ignoring.");
            } else {
                var modpackentries = Directory.GetFileSystemEntries(modpackpath);
                for (int i = 0; i < modpackentries.Length; i++) {
                    var full_entry = modpackentries[i];

                    Load(full_entry, parent: info);
                }
            }
        }

        private ModInfo _LoadFromDir(string path, ModInfo.Metadata default_metadata = null, string original_path = null) {
            var info = new ModInfo {
                ModMetadata = default_metadata ?? new ModInfo.Metadata {
                    Name = Path.GetFileNameWithoutExtension(path),
                },
                RealPath = path,
                Path = original_path ?? path
            };

            var entries = Directory.GetFileSystemEntries(path);
            for (int i = 0; i < entries.Length; i++) {
                var full_entry = entries[i];

                if (Path.GetFileName(full_entry) == METADATA_FILE_NAME) {
                    using (var file = File.OpenRead(full_entry)) {
                        using (var reader = new StreamReader(file)) {
                            info.ModMetadata = Deserializer.Deserialize<ModInfo.Metadata>(reader);
                        }
                    }
                }
            }

            return info;
        }
    }
}