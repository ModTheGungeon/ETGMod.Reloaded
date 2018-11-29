using System;
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
        }
    }
}