using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ETGMod.GUI;
using ETGMod.Tools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ETGMod.Console {
    public partial class Console : Backend {
        private Logger.Subscriber _LoggerSubscriber;
        private bool _Subscribed = false;
        private static Dictionary<Logger.LogLevel, Color> _LoggerColors = new Dictionary<Logger.LogLevel, Color> {
            {Logger.LogLevel.Debug, UnityUtil.NewColorRGB(10, 222, 0)},
            {Logger.LogLevel.Info, UnityUtil.NewColorRGB(0, 173, 238)},
            {Logger.LogLevel.Warn, UnityUtil.NewColorRGB(237, 160, 0)},
            {Logger.LogLevel.Error, UnityUtil.NewColorRGB(255, 31, 31)}
        };
        private Logger.LogLevel _LogLevel = Logger.LogLevel.Debug;

        // for the debug/mods command
        private void _GetModInfo(StringBuilder builder, ModLoader.ModInfo info, string indent = "") {
            builder.AppendLine($"{indent}- {info.Name}: {info.Resources.ResourceCount} resources");
            foreach (var mod in info.EmbeddedMods) {
                if (mod.Parent == info) {
                    _GetModInfo(builder, mod, indent + "  ");
                }
            }
        }

        private string _GetPickupObjectName(PickupObject obj) {
            try {
                var name = obj.EncounterNameOrDisplayName?.Trim();
                if (name == null || name == "") return "NO NAME";
                return name;
            } catch {
                return "ERROR";
            }
        }

        private PlayerController _ObtainCharacter(string name, bool alt) {
            PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
            Vector3 position = primaryPlayer.transform.position;
            Object.Destroy(primaryPlayer.gameObject);
            GameManager.Instance.ClearPrimaryPlayer();
            GameManager.PlayerPrefabForNewGame = (GameObject)BraveResources.Load(name, ".prefab");
            PlayerController component = GameManager.PlayerPrefabForNewGame.GetComponent<PlayerController>();
            GameStatsManager.Instance.BeginNewSession(component);
            PlayerController playerController = null;
            if (playerController == null) {
                GameObject gameObject = Object.Instantiate<GameObject>(GameManager.PlayerPrefabForNewGame, position, Quaternion.identity);
                GameManager.PlayerPrefabForNewGame = null;
                gameObject.SetActive(true);
                playerController = gameObject.GetComponent<PlayerController>();
            }
            if (alt) playerController.SwapToAlternateCostume(null);
            GameManager.Instance.PrimaryPlayer = playerController;
            playerController.PlayerIDX = 0;
            return playerController;
        }

        private IEnumerator _ChangeCharacter(string name, bool alt) {
            PlayerController new_player;
            var gun_game = false;
            //Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
            if (GameManager.Instance.PrimaryPlayer) {
                gun_game = GameManager.Instance.PrimaryPlayer.CharacterUsesRandomGuns;
            }
            GameManager.Instance.PrimaryPlayer.SetInputOverride("getting deleted");
            yield return new WaitForSeconds(0.5f);

            new_player = _ObtainCharacter(name, alt);
            yield return null;

            GameManager.Instance.MainCameraController.ClearPlayerCache();
            GameManager.Instance.MainCameraController.SetManualControl(false, true);
            Foyer.Instance.ProcessPlayerEnteredFoyer(new_player);
            Foyer.Instance.PlayerCharacterChanged(new_player);
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(new_player.specRigidbody, null, false);
            //Pixelator.Instance.FadeToBlack(0.5f, true, 0f);
            yield return new WaitForSeconds(0.1f);

            if (gun_game) {
                PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
                primaryPlayer.CharacterUsesRandomGuns = true;
                for (int i = 1; i < primaryPlayer.inventory.AllGuns.Count; i++) {
                    Gun gun = primaryPlayer.inventory.AllGuns[i];
                    primaryPlayer.inventory.RemoveGunFromInventory(gun);
                    Object.Destroy(gun.gameObject);
                    i--;
                }
            }
        }

        internal void AddDefaultCommands() {
            _LoggerSubscriber = (logger, loglevel, indent, str) => {
                PrintLine(logger.String(loglevel, str, indent: indent), color: _LoggerColors[loglevel]);
            };


            AddCommand("!!", (args, histindex) => {
                if (histindex - 1 < 0) throw new Exception("Can't run previous command (history is empty).");
                return History.Execute(histindex.Value - 1);
            });

            AddCommand("!'", (args, histindex) => {
                if (histindex - 1 < 0) throw new Exception("Can't run previous command (history is empty).");
                return History.Entries[histindex.Value - 1];
            });

            AddCommand("echo", (args) => {
                return string.Join(" ", args.ToArray());
            }).WithSubCommand("hello", (args) => {
                return "Hello, world!\nHello, world!\nHello, world!\nHello, world!\nHello, world!\nHello, world!";
            });

            AddGroup("debug")
                .WithSubCommand("summon", (args) => {
                    if (args.Count < 1) throw new Exception("At least 1 argument required.");
                    var myguid = args[0];
                    int count = 0;

                    if (args.Count >= 2) count = int.Parse(args[1]);

                    var prefab = EnemyDatabase.GetOrLoadByGuid(myguid);
                    for (int i = 0; i < count; i++) {
                        IntVector2? targetCenter = new IntVector2?(GameManager.Instance.PrimaryPlayer.CenterPosition.ToIntVector2(VectorConversions.Floor));
                        Pathfinding.CellValidator cellValidator = delegate (IntVector2 c) {
                            for (int j = 0; j < prefab.Clearance.x; j++) {
                                for (int k = 0; k < prefab.Clearance.y; k++) {
                                    if (GameManager.Instance.Dungeon.data.isTopWall(c.x + j, c.y + k)) {
                                        return false;
                                    }
                                    if (targetCenter.HasValue) {
                                        if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) < 4) {
                                            return false;
                                        }
                                        if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) > 20) {
                                            return false;
                                        }
                                    }
                                }
                            }
                            return true;
                        };
                        IntVector2? randomAvailableCell = GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomAvailableCell(new IntVector2?(prefab.Clearance), new Dungeonator.CellTypes?(prefab.PathableTiles), false, cellValidator);
                        if (randomAvailableCell.HasValue) {
                            AIActor aIActor = AIActor.Spawn(prefab, randomAvailableCell.Value, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
                            aIActor.HandleReinforcementFallIntoRoom(0);
                        }
                    }
                    return prefab?.ActorName ?? "[Unknown]";
                })
                .WithSubCommand("force-dual-wield", (args) => {
                    if (args.Count < 1) throw new Exception("At least 1 argument required.");
                    var partner_id = int.Parse(args[0]);
                    var player = GameManager.Instance.PrimaryPlayer;
                    var gun = player.inventory.CurrentGun;
                    var partner_gun = PickupObjectDatabase.GetById(partner_id) as Gun;
                    player.inventory.AddGunToInventory(partner_gun);
                    var forcer = gun.gameObject.AddComponent<DualWieldForcer>();
                    forcer.Gun = gun;
                    forcer.PartnerGunID = partner_gun.PickupObjectId;
                    forcer.TargetPlayer = player;
                    return "Done";
                })
                .WithSubCommand("unexclude-all-items", (args) => {
                    foreach (var ent in PickupObjectDatabase.Instance.Objects) {
                        if (ent == null) continue;
                        ent.quality = PickupObject.ItemQuality.SPECIAL;
                    }
                    return "Done";
                })
                .WithSubCommand("activate-all-synergies", (args) => {
                    foreach (var ent in GameManager.Instance.SynergyManager.synergies) {
                        if (ent == null) continue;
                        ent.ActivationStatus = SynergyEntry.SynergyActivation.ACTIVE;
                    }
                    return "Done";
                })
                .WithSubCommand("character", (args) => {
                    if (args.Count < 1) throw new Exception("At least 1 argument required.");
                    StartCoroutine(_ChangeCharacter(args[0], args.Count > 1));
                    return $"Changed character to {args[0]}";
                })
                .WithSubCommand("parser-bounds-test", (args) => {
                    var text = "echo Hello! \"Hello world!\" This\\ is\\ great \"It\"works\"with\"\\ wacky\" stuff\" \\[\\] \"\\[\\]\" [e[echo c][echo h][echo [echo \"o\"]] \"hel\"[echo lo][echo !]]";
                    CurrentCommandText = text;
                    return null;
                })
                .WithSubCommand("giveid", (args) => {
                    if (args.Count < 1) throw new Exception("Exactly 1 argument required.");
                    var pickup_obj = PickupObjectDatabase.Instance.InternalGetById(int.Parse(args[0]));

                    if (pickup_obj == null) {
                        return "Item ID {args[0]} doesn't exist!";
                    }

                    LootEngine.TryGivePrefabToPlayer(pickup_obj.gameObject, GameManager.Instance.PrimaryPlayer, true);
                    return pickup_obj.EncounterNameOrDisplayName;
                });

            AddGroup("pool")
                .WithSubGroup(
                    new Group("items")
                    .WithSubCommand("idof", (args) => {
                        if (args.Count < 1) throw new Exception("Exactly 1 argument required (numeric ID).");
                        var id = int.Parse(args[0]);
                        foreach (var pair in ETGMod.Items.Pairs) {
                            if (pair.Value.PickupObjectId == id) return pair.Key;
                        }
                        return "Entry not found.";
                    })
                    .WithSubCommand("nameof", (args) => {
                        if (args.Count < 1) throw new Exception("Exactly 1 argument required (ID).");
                        var id = args[0];
                        foreach (var pair in ETGMod.Items.Pairs) {
                            if (pair.Key == id) return _GetPickupObjectName(pair.Value);
                        }
                        return "Entry not found.";
                    })
                    .WithSubCommand("numericof", (args) => {
                        if (args.Count < 1) throw new Exception("Exactly 1 argument required (ID).");
                        var id = args[0];
                        foreach (var pair in ETGMod.Items.Pairs) {
                            if (pair.Key == id) return pair.Value.PickupObjectId.ToString();
                        }
                        return "Entry not found.";
                    })
                    .WithSubCommand("list", (args) => {
                        var s = new StringBuilder();
                        var pairs = new List<KeyValuePair<string, PickupObject>>();
                        foreach (var pair in ETGMod.Items.Pairs) {
                            pairs.Add(pair);
                        }
                        foreach (var pair in pairs) {
                            if (_GetPickupObjectName(pair.Value) == "NO NAME") {
                                s.AppendLine($"[{pair.Key}] {_GetPickupObjectName(pair.Value)}");
                            }
                        }
                        pairs.Sort((x, y) => string.Compare(_GetPickupObjectName(x.Value), _GetPickupObjectName(y.Value)));
                        foreach (var pair in pairs) {
                            if (_GetPickupObjectName(pair.Value) == "NO NAME") continue;
                            s.AppendLine($"[{pair.Key}] {_GetPickupObjectName(pair.Value)}");
                        }
                        return s.ToString();
                    })
                    .WithSubCommand("random", (args) => {
                        return ETGMod.Items.RandomKey;
                    })
                );

            AddCommand("listmods", (args) => {
                var s = new StringBuilder();

                s.AppendLine("Loaded mods:");
                foreach (var mod in ETGMod.ModLoader.LoadedMods) {
                    _GetModInfo(s, mod);
                }
                return s.ToString();
            });

            AddCommand("lua", (args) => {
                LuaMode = true;
                return "[entered lua mode]";
            });

            AddCommand("give", (args) => {
                LootEngine.TryGivePrefabToPlayer(ETGMod.Items[args[0]].gameObject, GameManager.Instance.PrimaryPlayer, true);
                return args[0];
            });

            AddGroup("dump")
                .WithSubCommand("synergy_chest", (args) => {
                    System.Console.WriteLine(ObjectDumper.Dump(GameManager.Instance.RewardManager.Synergy_Chest, depth: 10));
                    return "Dumped to log";
                })
                .WithSubCommand("synergies", (args) => {
                    var id = 0;
                    foreach (var synergy in GameManager.Instance.SynergyManager.synergies) {
                        if (synergy.NameKey != null) {
                            var name = StringTableManager.GetSynergyString(synergy.NameKey);
                            System.Console.WriteLine($"== SYNERGY ID {id} NAME {name} ==");
                        } else {
                            System.Console.WriteLine($"== SYNERGY ID {id} ==");
                        }
                        System.Console.WriteLine($"  ACTIVATION STATUS: {synergy.ActivationStatus}");
                        System.Console.WriteLine($"  # OF OBJECTS REQUIRED: {synergy.NumberObjectsRequired}");
                        System.Console.WriteLine($"  ACTIVE WHEN GUN UNEQUIPPED?: {synergy.ActiveWhenGunUnequipped}");
                        System.Console.WriteLine($"  REQUIRES AT LEAST ONE GUN AND ONE ITEM?: {synergy.RequiresAtLeastOneGunAndOneItem}");
                        System.Console.WriteLine($"  MANDATORY GUNS:");
                        foreach (var itemid in synergy.MandatoryGunIDs) {
                            System.Console.WriteLine($"  - {_GetPickupObjectName(PickupObjectDatabase.GetById(itemid))}");
                        }
                        System.Console.WriteLine($"  OPTIONAL GUNS:");
                        foreach (var itemid in synergy.OptionalGunIDs) {
                            System.Console.WriteLine($"  - {_GetPickupObjectName(PickupObjectDatabase.GetById(itemid))}");
                        }
                        System.Console.WriteLine($"  MANDATORY ITEMS:");
                        foreach (var itemid in synergy.MandatoryItemIDs) {
                            System.Console.WriteLine($"  - {_GetPickupObjectName(PickupObjectDatabase.GetById(itemid))}");
                        }
                        System.Console.WriteLine($"  OPTIONAL ITEMS:");
                        foreach (var itemid in synergy.OptionalItemIDs) {
                            System.Console.WriteLine($"  - {_GetPickupObjectName(PickupObjectDatabase.GetById(itemid))}");
                        }
                        System.Console.WriteLine($"  BONUS SYNERGIES:");
                        foreach (var bonus in synergy.bonusSynergies) {
                            System.Console.WriteLine($"  - {bonus}");
                        }
                        System.Console.WriteLine($"  STAT MODIFIERS:");
                        foreach (var statmod in synergy.statModifiers) {
                            System.Console.WriteLine($"  - STAT: {statmod.statToBoost}");
                            System.Console.WriteLine($"    AMOUNT: {statmod.amount}");
                            System.Console.WriteLine($"    MODIFY TYPE: {statmod.modifyType}");
                            System.Console.WriteLine($"    PERSISTS ON COOP DEATH?: {statmod.PersistsOnCoopDeath}");
                            System.Console.WriteLine($"    IGNORED FOR SAVE DATA?: {statmod.ignoredForSaveData}");
                        }
                        id++;
                    }
                    return "Dumped to log";
                })
                .WithSubCommand("items", (args) => {
                    var b = new StringBuilder();
                    var db = PickupObjectDatabase.Instance.Objects;
                    for (int i = 0; i < db.Count; i++) {
                        PickupObject obj = null;
                        string nameprefix = "";
                        string name = null;
                        try {
                            obj = db[i];
                        } catch {
                            name = "[ERROR: failed getting object by index]";
                        }
                        if (obj != null) {
                            try {
                                var displayname = obj.encounterTrackable.journalData.PrimaryDisplayName;
                                name = StringTableManager.ItemTable[displayname].GetWeightedString();
                            } catch {
                                name = "[ERROR: failed getting ammonomicon name]";
                            }
                            if (name == null) {
                                try {
                                    name = obj.EncounterNameOrDisplayName;
                                } catch {
                                    name = "[ERROR: failed getting encounter or display name]";
                                }
                            }
                        }
                        if (name == null && obj != null) {
                            name = "[NULL NAME (but object is not null)]";
                        }

                        name = $"{nameprefix} {name}";

                        if (name != null) {
                            b.AppendLine($"{i}: {name}");
                            _Logger.Info($"{i}: {name}");
                        }
                    }
                    return b.ToString();
                });

            AddGroup("log")
                .WithSubCommand("sub", (args) => {
                    if (_Subscribed) return "Already subscribed.";
                    Logger.Subscribe(_LoggerSubscriber);
                    _Subscribed = true;
                    return "Done.";
                })
                .WithSubCommand("unsub", (args) => {
                    if (!_Subscribed) return "Not subscribed yet.";
                    Logger.Unsubscribe(_LoggerSubscriber);
                    _Subscribed = false;
                    return "Done.";
                })
                .WithSubCommand("level", (args) => {
                    if (args.Count == 0) {
                        return _LogLevel.ToString().ToLowerInvariant();
                    } else {
                        switch (args[0]) {
                        case "debug": _LogLevel = Logger.LogLevel.Debug; break;
                        case "info": _LogLevel = Logger.LogLevel.Info; break;
                        case "warn": _LogLevel = Logger.LogLevel.Warn; break;
                        case "error": _LogLevel = Logger.LogLevel.Error; break;
                        default: throw new Exception($"Unknown log level '{args[0]}");
                        }
                        return "Done.";
                    }
                });
            // test commands to dump collection
            AddGroup("texdump")
                .WithSubCommand("collection", (args) =>
                {
                    if (args.Count == 0)
                    {
                        return "No name specified";
                    }
                    else
                    {
                        string collectionName = args[0];
                        Animation.Collection.Dump(collectionName);
                        return "Successfull";
                    }
                });
        }
    }
}