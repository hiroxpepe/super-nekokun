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

using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// カメラ関連の処理
    /// @author h.adachi
    /// </summary>
    public class CameraController : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // カメラ用 GameObject への参照

        [SerializeField]
        GameObject mainCamera;

        [SerializeField]
        GameObject verticalAxis;

        [SerializeField]
        GameObject horizontalAxis;

        // 光源:デフォルト 44,16,100,100
        
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Vector3 defaultLocalPosition; // カメラシステム位置 デフォルト値 // x:0, y:0.95, z:-1.2

        bool isForwardPosition = false; // カメラシステムが前進ポジションかどうか

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        public void LookPlayer() { // TODO: 再検討
            transform.localPosition = new Vector3(0f, 0.75f, 0.8f);
            transform.localRotation = new Quaternion(0f, -180f, 0f, 0f);
        }

        public void ResetLookAround() { // カメラリセット
            resetLookAround();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            defaultLocalPosition = transform.localPosition; // デフォルトのカメラポジション保存
        }

        // Update is called once per frame
        new void Update() {
            base.Update();

            // 視点操作のリセット
            if (xButton.wasReleasedThisFrame) {
                resetLookAround();
                return;
            }

            // 視点操作(Xボタン押しっぱなし)
            if (xButton.isPressed) {
                lookAround();
                return;
            }

            if (!xButton.isPressed) { // 視点操作モードでない場合
                float _ADJUST = 80.0f; // 移動係数
                if (isForwardPosition) {
                    if (transform.localPosition.z < -0.5f) { // カメラシステムをプレイヤーの頭のすぐ後ろまで移動する
                        transform.localPosition += new Vector3(0f, 0f, 0.075f * Time.deltaTime * _ADJUST);
                    }
                    transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
                    checkSix();
                } else if (!isForwardPosition) {
                    if (transform.localPosition.z > defaultLocalPosition.z) {
                        transform.localPosition -= new Vector3(0f, 0f, 0.035f * Time.deltaTime * _ADJUST);
                    }
                    if (transform.localPosition.y > defaultLocalPosition.y) {
                        transform.localPosition -= new Vector3(0f, 0.035f * Time.deltaTime * _ADJUST, 0f);
                    }
                    transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
                }
            }

            // 視点ズームアップ
            if (rStickUpButton.isPressed) {
                if (!(defaultLocalPosition.z >= -0.6f)) { // 調整値
                    defaultLocalPosition = new Vector3(
                        defaultLocalPosition.x,
                        defaultLocalPosition.y,
                        defaultLocalPosition.z + 0.05f
                    );
                    transform.localPosition = defaultLocalPosition;
                    Debug.Log("ズームアップ");
                }
            }

            // 視点ズームアウト
            if (rStickDownButton.isPressed) {
                if (!(defaultLocalPosition.z <= -1.55f)) { // 調整値
                    defaultLocalPosition = new Vector3(
                        defaultLocalPosition.x,
                        defaultLocalPosition.y,
                        defaultLocalPosition.z - 0.05f
                    );
                    transform.localPosition = defaultLocalPosition;
                    Debug.Log("ズームアウト");
                }
            }

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Event handler

        void OnTriggerEnter(Collider other) {
            if (other.name.Contains("Block") || other.name.Contains("Wall")) {
                isForwardPosition = true; // カメラ前進ポジションフラグON
            }
        }

        void OnTriggerStay(Collider other) {
            if (other.name.Contains("Block") || other.name.Contains("Wall")) {
                isForwardPosition = true; // カメラ前進ポジションフラグON
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        void checkSix() {
            var _player = transform.parent.gameObject;
            Ray _ray = new Ray( // プレイヤーの位置から後方を確認する
                new Vector3(
                    _player.transform.position.x,
                    _player.transform.position.y + 0.6f, // カメラシステムのコライダーと同じ高さにする
                    _player.transform.position.z
                ),
                -transform.forward
            );
            if (Physics.Raycast(_ray, out RaycastHit _hit, 1.0f)) {
#if DEBUG
                Debug.DrawRay(_ray.origin, _ray.direction * 1.0f, Color.magenta, 3, false); //レイを可視化
#endif
            } else { // 後方にレイを投げて反応がなくなった場合
                isForwardPosition = false; // 前進ポジションフラグOFF
            }
        }

        void resetLookAround() { // 一人称視点カメラリセット
            // 位置 X: 0, Y:0.95, Z:-1.2 ※CameraSystemに設定 // Y:0.75 → 0.95
            // 回転 X:15, Y:0   , Z:0    ※MainCameraに設定
            // 拡縮 X: 1, Y:1   , Z:1
            transform.localPosition = defaultLocalPosition; // カメラシステム位置リセット
            transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
            horizontalAxis.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f); // カメラ水平回転リセット
            verticalAxis.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f); // カメラ垂直回転リセット
            mainCamera.transform.localEulerAngles = new Vector3(15f, 0f, 0f); // カメラ本体は少し下向き
        }

        void lookAround() { // 一人称視点カメラ操作
            float _ADJUST = 80.0f; // 回転係数
            mainCamera.transform.localEulerAngles = new Vector3(0f, 0f, 0f); // カメラ本体を水平に
            if (upButton.isPressed) { // 上
                verticalAxis.transform.Rotate(1.0f * Time.deltaTime * _ADJUST, 0f, 0f);
            } else if (downButton.isPressed) { // 下
                verticalAxis.transform.Rotate(-1.0f * Time.deltaTime * _ADJUST, 0f, 0f);
            } else if (leftButton.isPressed) { // 左
                horizontalAxis.transform.Rotate(0f, -1.0f * Time.deltaTime * _ADJUST, 0f);
            } else if (rightButton.isPressed) { // 右
                horizontalAxis.transform.Rotate(0f, 1.0f * Time.deltaTime * _ADJUST, 0f);
            }
            if (transform.localPosition.z < 0.1f) { // カメラシステムをキャラの目の位置に移動する
                transform.localPosition += new Vector3(0f, 0f, 0.075f * Time.deltaTime * _ADJUST);
            }
        }
    }

}
