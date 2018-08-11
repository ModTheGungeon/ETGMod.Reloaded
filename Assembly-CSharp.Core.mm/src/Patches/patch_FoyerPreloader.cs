#pragma warning disable 0626
#pragma warning disable 0649

/////////////////////
//// ENTRY POINT ////
/////////////////////

using System;
using ETGMod;
using UnityEngine;
using System.Reflection;
using MonoMod;
using System.IO;
using ETGMod.Tools;
using System.Collections.Generic;

namespace ETGMod.CorePatches {
    [MonoModPatch("global::FoyerPreloader")]
    internal class FoyerPreloadeer : global::FoyerPreloader {
        public static string[] AssetBundles = new string[] {
            "dungeon_scene_001",
            "shared_base_001",
            "shared_auto_002",
            "foyer_003",
            "foyer_001",
            "shared_auto_001",
            "enemies_base_001",
            "foyer_002",
            "dungeons",
            "dungeons/base_tutorial",
            "dungeons/finalscenario_soldier",
            "dungeons/base_foyer",
            "dungeons/finalscenario_convict",
            "dungeons/base_castle",
            "dungeons/base_cathedral",
            "dungeons/base_mines",
            "dungeons/finalscenario_coop",
            "dungeons/base_catacombs",
            "dungeons/base_bullethell",
            "dungeons/finalscenario_pilot",
            "dungeons/finalscenario_guide",
            "dungeons/finalscenario_robot",
            "dungeons/base_forge",
            "dungeons/finalscenario_bullet",
            "dungeons/base_gungeon",
            "dungeons/base_resourcefulrat",
            "dungeons/base_sewer",
            "flows_base_001",
            "brave_resources_001",
            "encounters_base_001"
        };
        public void DumpAssets() {
            Console.WriteLine("DUMPING ALL ASSET BUNDLES!");
            var output_dir = Path.Combine(Paths.GameFolder, "ETGMOD ASSET DUMP");
            if (Directory.Exists(output_dir)) Directory.Delete(output_dir, true);
            Directory.CreateDirectory(output_dir);

            foreach (var assetname in AssetBundles) {
                try {
                    var bundle_dir = Path.Combine(output_dir, assetname.Replace("/", "+"));
                    Directory.CreateDirectory(bundle_dir);
                    System.Console.WriteLine($"=== LOADING ASSET BUNDLE: {assetname} ===");
                    var bundle = ResourceManager.LoadAssetBundle(assetname);
                    System.Console.WriteLine($"=== OBTAINING ALL ASSETS ===");
                    var assets = bundle.LoadAllAssets();
                    System.Console.WriteLine($"=== {assets.Length} ASSETS LOADED ===");
                    var assets_table = new Dictionary<string, int>();
                    foreach (var asset in assets) {
                        System.Console.WriteLine($"=== DUMPING ASSET: {asset.name} ===");
                        var name = asset.name.Replace("/", "+");
                        int amount;
                        if (assets_table.TryGetValue(name, out amount)) {
                            amount += 1;
                            assets_table[name] = amount;
                            name = $"{name}+++{amount}";
                        } else {
                            assets_table[name] = 1;
                        }
                        var asset_file = Path.Combine(bundle_dir, $"{name}.json");
                        using (var writer = new StreamWriter(File.OpenWrite(asset_file))) {
                            writer.Write(ObjectDumper.Dump(asset, depth: 10));
                        }
                    }
                } catch {
                    System.Console.WriteLine($"Failed dumping asset bundle {assetname}");
                }
            }
            Console.WriteLine("!!! DONE !!!");
        }

        protected extern void orig_Awake();
        private void Awake() {
            Loader.Logger.Info("Mod the Gungeon entry point");
            EventHooks.InvokeGameStarted();
            Backend.GameObject = new GameObject("Mod the Gungeon");

            var asm = Assembly.GetExecutingAssembly();
            var types = asm.GetTypes();
            for (int i = 0; i < types.Length; i++) {
                var type = types[i];
                if (type.IsSubclassOf(typeof(Backend))) {
                    var backend = (Backend)Backend.GameObject.AddComponent(type);

                    DontDestroyOnLoad(backend);

                    Backend.AllBackends.Add(new Backend.Info {
                        Name = type.Name,
                        StringVersion = backend.StringVersion,
                        Version = backend.Version,
                        Type = type,
                        Instance = backend
                    });

                    try {
                        backend.NoBackendsLoadedYet();
                    } catch (Exception e) {
                        Loader.Logger.Error($"Exception while pre-loading backend {type.Name}: [{e.GetType().Name}] {e.Message}");
                        foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                    }
                }
            }

            for (int i = 0; i < Backend.AllBackends.Count; i++) {
                var backend = Backend.AllBackends[i];
                Loader.Logger.Info($"Initializing backend {backend.Name} {backend.StringVersion}");
                try {
                    backend.Instance.Loaded();
                } catch (Exception e) {
                    Loader.Logger.Error($"Exception while loading backend {backend.Name}: [{e.GetType().Name}] {e.Message}");
                    foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                }
            }

            for (int i = 0; i < Backend.AllBackends.Count; i++) {
                try {
                    Backend.AllBackends[i].Instance.AllBackendsLoaded();
                } catch (Exception e) {
                    Loader.Logger.Error($"Exception while post-loading backend {Backend.AllBackends[i].Name}: [{e.GetType().Name}] {e.Message}");
                    foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                }
            }

            //DumpAssets();

            orig_Awake();
        }
    }
}
