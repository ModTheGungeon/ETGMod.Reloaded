using System;
using System.Collections;
using UnityEngine;

namespace ModTheGungeon {
    public static class Loader {
        public static Logger Logger = new Logger("Loader");
    }

    public class Coroutine {
        private class DummyBehaviour : MonoBehaviour {}
        private static GameObject _GameObject = new GameObject("ModTheGungeon Global Coroutines");
        private static DummyBehavior _DummyBehaviour;

        static Coroutine() {
            _DummyBehaviour = _GameObject.AddComponent<DummyBehavior>();
        }

        public static void Start(IEnumerator coroutine) {
            _DummyBehaviour.StartCoroutine(coroutine);
        }
    }
}
