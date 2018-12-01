using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModTheGungeon {
    public class PrefabricTest : MonoBehaviour {
        public class InnerObject {
            public int A;
            public string B;
        }

        public static void RunTest() {

        }

        public int A;
        public string B;
        public InnerObject C;
        public string[] StringAry;
        public InnerObject[] ObjectAry;
        public List<string> StringList;
        public Dictionary<string, InnerObject> ObjectDict;

        public void Test() {
            Console.WriteLine($"A: {A} B: {B} C: {C}");
            if (C != null) Console.WriteLine($"INNER: A: {C.A} B: {C.B}");
            if (StringAry != null) {
                Console.WriteLine($"STRINGARY NOT NULL, LENGTH {StringAry.Length}");
                for (int i = 0; i < StringAry.Length; i++) {
                    var ent = StringAry[i];
                    Console.WriteLine($"IDX {i}: VAL {ent}");
                }
            }
            if (ObjectAry != null) {
                Console.WriteLine($"OBJECTARY NOT NULL, LENGTH {ObjectAry.Length}");
                for (int i = 0; i < ObjectAry.Length; i++) {
                    var ent = ObjectAry[i];
                    Console.WriteLine($"IDX {i}: A: {ent.A} B: {ent.B}");
                }
            }
            if (StringList != null) {
                Console.WriteLine($"STRINGLIST NOT NULL, COUNT {StringList.Count}");
                for (int i = 0; i < StringList.Count; i++) {
                    var ent = StringList[i];
                    Console.WriteLine($"IDX {i}: VAL {ent}");
                }
            }
            if (ObjectDict != null) {
                Console.WriteLine($"OBJECTDICT NOT NULL, COUNT {ObjectDict.Count}");
                foreach (var ent in ObjectDict) {
                    Console.WriteLine($"IDX {ent.Key}: A: {ent.Value.A} B: {ent.Value.B}");
                }
            }
        }
    }
}