using System;
using UnityEngine;

namespace ModTheGungeon.GUI.Console {
    public class DualWieldForcer : MonoBehaviour {
        //
        // Fields
        //

        public int PartnerGunID;
        public PlayerController TargetPlayer;

        public Gun Gun;
        private bool m_isCurrentlyActive;

        //
        // Methods
        //
        public void Activate() {
            if (EffectValid(TargetPlayer)) {
                m_isCurrentlyActive = true;
                TargetPlayer.inventory.SetDualWielding(true, "synergy");
                int indexForGun = GetIndexForGun(TargetPlayer, Gun.PickupObjectId);
                int indexForGun2 = GetIndexForGun(TargetPlayer, PartnerGunID);
                TargetPlayer.inventory.SwapDualGuns();
                if (indexForGun >= 0 && indexForGun2 >= 0) {
                    while (TargetPlayer.inventory.CurrentGun.PickupObjectId != PartnerGunID) {
                        TargetPlayer.inventory.ChangeGun(1, false);
                    }
                }
                TargetPlayer.inventory.SwapDualGuns();
                if (TargetPlayer.CurrentGun && !TargetPlayer.CurrentGun.gameObject.activeSelf) {
                    TargetPlayer.CurrentGun.gameObject.SetActive(true);
                }
                if (TargetPlayer.CurrentSecondaryGun && !TargetPlayer.CurrentSecondaryGun.gameObject.activeSelf) {
                    TargetPlayer.CurrentSecondaryGun.gameObject.SetActive(true);
                }
                TargetPlayer.GunChanged += new Action<Gun, Gun, bool>(HandleGunChanged);
            }
        }

        public void Awake() {
            Gun = GetComponent<Gun>();
        }

        private void CheckStatus() {
            if (m_isCurrentlyActive) {
                if (!PlayerUsingCorrectGuns() || !EffectValid(TargetPlayer)) {
                    System.Console.WriteLine("DISABLING EFFECT");
                    DisableEffect();
                }
            } else if (Gun && Gun.CurrentOwner is PlayerController) {
                PlayerController playerController = Gun.CurrentOwner as PlayerController;
                if (playerController.inventory.DualWielding && playerController.CurrentSecondaryGun.PickupObjectId == Gun.PickupObjectId && playerController.CurrentGun.PickupObjectId == PartnerGunID) {
                    m_isCurrentlyActive = true;
                    TargetPlayer = playerController;
                    return;
                }
                Activate();
            }
        }

        private void DisableEffect() {
            if (m_isCurrentlyActive) {
                m_isCurrentlyActive = false;
                TargetPlayer.inventory.SetDualWielding(false, "synergy");
                TargetPlayer.GunChanged -= new Action<Gun, Gun, bool>(HandleGunChanged);
                TargetPlayer.stats.RecalculateStats(TargetPlayer, false, false);
                TargetPlayer = null;
            }
        }

        private bool EffectValid(PlayerController p) {
            if (!p) {
                System.Console.WriteLine("NULL PLAYER");
                return false;
            }
            if (Gun.CurrentAmmo == 0) {
                System.Console.WriteLine("CURAMMO 0");
                return false;
            }
            if (!m_isCurrentlyActive) {
                int indexForGun = GetIndexForGun(p, PartnerGunID);
                if (indexForGun < 0) {
                    System.Console.WriteLine("IDX4GUN <0");
                    return false;
                }
                if (p.inventory.AllGuns[indexForGun].CurrentAmmo == 0) {
                    System.Console.WriteLine("PARTNERAMMO 0");
                    return false;
                }
            } else if (p.CurrentSecondaryGun != null && p.CurrentSecondaryGun.PickupObjectId == PartnerGunID && p.CurrentSecondaryGun.CurrentAmmo == 0) {
                System.Console.WriteLine("SECONDARYAMMO 0");
                return false;
            }
            System.Console.WriteLine("EFFECT VALID");
            return true;
        }

        private int GetIndexForGun(PlayerController p, int gunID) {
            for (int i = 0; i < p.inventory.AllGuns.Count; i++) {
                if (p.inventory.AllGuns[i].PickupObjectId == gunID) {
                    return i;
                }
            }
            return -1;
        }

        private void HandleGunChanged(Gun arg1, Gun newGun, bool arg3) {
            CheckStatus();
        }

        private bool PlayerUsingCorrectGuns() {
            return Gun && Gun.CurrentOwner && TargetPlayer && TargetPlayer.inventory.DualWielding && (!(TargetPlayer.CurrentGun != Gun) || TargetPlayer.CurrentGun.PickupObjectId == PartnerGunID) && (!(TargetPlayer.CurrentSecondaryGun != Gun) || TargetPlayer.CurrentSecondaryGun.PickupObjectId == PartnerGunID);
        }

        private void Update() {
            CheckStatus();
        }
    }
}
