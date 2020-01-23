using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace StudioMeowToon {
    /// <summary>
    /// 汎用拡張メソッドクラス
    /// </summary>
    public static class Extensions {

        public static bool LikePlayer(this GameObject self) {
            return self.tag.Contains("Player");
        }

        public static bool IsPlayer(this GameObject self) {
            return self.tag.Equals("Player");
        }
    }

}
