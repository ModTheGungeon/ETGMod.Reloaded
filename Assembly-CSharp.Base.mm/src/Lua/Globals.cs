﻿using System;
using UnityEngine;

namespace ModTheGungeon.Lua {
    public static class Globals {
        public static PlayerController PrimaryPlayer { get { return GameManager.Instance.PrimaryPlayer; } }
        public static PlayerController SecondaryPlayer { get { return GameManager.Instance.SecondaryPlayer; } }
    }
}    