/*
 * Copyright 2002-2020 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// 汎用ユーティリティークラス
    /// @author h.adachi
    /// </summary>
    public class Utils { // 何もしなくても別のスクリプトから呼び出せる

        ///////////////////////////////////////////////////////////////////////////////////////////
        // パブリックメソッド(パスカルケース: 動詞)

        /// <summary>
        /// localPosition Y座標だけ入れ替える。TODO: 拡張メソッドで localPosition.ReplaceY(5f); とか実装？
        /// </summary>
        public static Vector3 ReplaceLocalPositionY(Transform t, float value) {
            return new Vector3(t.localPosition.x, value, t.localPosition.z);
        }

        /// <summary>
        /// transform.forward: (0.7, 0.0, 0.7) で AddFor​​ce が効かない対策。
        /// </summary>
        public static Vector3 TransformForward(Vector3 forward, float speed) {
            if (speed < 0.95f) { // 低速の時 ※TODO:要調整
                if (Math.Abs(Math.Round(forward.x, 1)) == 0.7d && Math.Abs(Math.Round(forward.z, 1)) == 0.7d) { // 絶対値が同じなら
                    //Debug.Log("x: " + Math.Abs(Math.Round(forward.x, 1)) + "z: " + Math.Abs(Math.Round(forward.z, 1)));
                    return new Vector3((forward.x * 1.2f), forward.y, (forward.z * 0.8f)); // 少しずらす MEMO:ベター
                    //return new Vector3((forward.x += 0.2f), forward.y, (forward.z -= 0.2f)); // 少しずらす
                }
            }
            return forward;
        }

        /// <summary>
        /// マテリアルのレンダリングモードを設定する。
        /// </summary>
        public static void SetRenderingMode(Material material, RenderingMode renderingMode) {
            switch (renderingMode) {
                case RenderingMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case RenderingMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2450;
                    break;
                case RenderingMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
                case RenderingMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
            }
        }

    }

    /// <summary>
    /// Android スマホを振動させるクラス
    /// @author h.adachi
    /// </summary>
    public static class AndroidVibrator {
#if UNITY_ANDROID && !UNITY_EDITOR
        public static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        public static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#else
        public static AndroidJavaClass unityPlayer;
        public static AndroidJavaObject currentActivity;
        public static AndroidJavaObject vibrator;
#endif
        public static void Vibrate(long milliseconds) {
            if (isAndroid()) {
                vibrator.Call("vibrate", milliseconds);
            } else {
                Handheld.Vibrate();
            }
        }

        static bool isAndroid() {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

}
