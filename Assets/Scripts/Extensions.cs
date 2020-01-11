using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// 汎用拡張メソッドクラス
    /// </summary>
    public static class Extension {
        public static void Method(this GameObject self) {
            var n = self.name;
        }

        public static bool LikePlayer(this GameObject self) {
            return self.tag.Contains("Player");
        }

        public static bool IsPlayer(this GameObject self) {
            return self.tag.Equals("Player");
        }
    }

}