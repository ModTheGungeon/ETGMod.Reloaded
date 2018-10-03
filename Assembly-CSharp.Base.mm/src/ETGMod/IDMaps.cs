using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ETGMod {
    public partial class ETGMod : Backend {
        public enum ItemType {
            Unknown,
            Item,
            Consumable,
            Syn//ergy
        }

        public enum EntityType {
            Unknown,
            Enemy,
            Friendly
        }

        public static IDPool<PickupObject, ItemType> Items;
        public static IDPool<AIActor, EntityType> Entities;

        private IDPool<T, TType> _ReadIDMap<T, TType>(IList<T> list, string path) where T : UnityEngine.Object {
            var pool = new IDPool<T, TType>();

            using (var file = File.OpenRead(path)) {
                using (var reader = new StreamReader(file)) {
                    var line_id = 0;

                    while (!reader.EndOfStream) {
                        line_id += 1;
                        var line = reader.ReadLine().Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        if (line.Length == 0) continue;

                        var split = line.Split(' ');
                        if (split.Length < 3) {
                            throw new Exception($"Failed parsing ID map file: not enough columns at line {line_id} (need at least 2, ID and the name)");
                        }
                        var type_el_split = split[0].Split(',');
                        var type = type_el_split[0];
                        var type_val = (TType)Enum.Parse(typeof(TType), type, true);

                        string subtype = null;
                        if (type_el_split.Length >= 2) {
                            subtype = type_el_split[1];
                        }

                        int id;
                        if (!int.TryParse(split[1], out id)) throw new Exception($"Failed parsing ID map file: ID column at line {line_id} was not an integer");

                        try {
                            pool[$"gungeon:{split[2]}"] = list[id];
                            pool.SetType($"gungeon:{split[2]}", type_val);
                        } catch (Exception e) {
                            throw new Exception($"Failed loading ID map file: Error while adding entry to ID pool ({e.Message})");
                        }
                    }
                }
            }

            pool.LockNamespace("gungeon");
            return pool;
        }

        private void _InitIDs() {
            var id_pool_base = Path.Combine(Paths.ResourcesFolder, "idmaps");
            ETGMod.Logger.Info("Loading item ID map");
            Items = _ReadIDMap<PickupObject, ItemType>(PickupObjectDatabase.Instance.Objects, Path.Combine(id_pool_base, "items.txt"));

            ETGMod.Logger.Info("Loading entity ID map");
            Entities = new IDPool<AIActor, EntityType>();
            using (var file = File.OpenRead(Path.Combine(id_pool_base, "enemies.txt"))) {
                using (var reader = new StreamReader(file)) {
                    var line_id = 0;

                    while (!reader.EndOfStream) {
                        line_id += 1;
                        var line = reader.ReadLine().Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        if (line.Length == 0) continue;

                        var split = line.Split(' ');
                        if (split.Length < 3) {
                            throw new Exception($"Failed parsing ID map file: not enough columns at line {line_id} (need at least 2, ID and the name)");
                        }
                        var type_el_split = split[0].Split(',');
                        var type = type_el_split[0];
                        var type_val = (EntityType)Enum.Parse(typeof(EntityType), type, true);

                        string subtype = null;
                        if (type_el_split.Length >= 2) {
                            subtype = type_el_split[1];
                        }

                        var prefab_name = split[1].Replace("%%%", " ");
                        try {
                            var prefab = EnemyDatabase.AssetBundle.LoadAsset<GameObject>(prefab_name);
                            Entities[$"gungeon:{split[2]}"] = prefab.GetComponent<AIActor>();
                            Entities.SetType($"gungeon:{split[2]}", type_val);
                        } catch (Exception e) {
                            throw new Exception($"Failed loading ID map file: Error while adding entry to ID pool ({e.Message})");
                        }
                    }
                }
            }
        }
    }
}