/*
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// カメラ関連の処理
    /// @author h.adachi
    /// </summary>
    public class CameraSystem : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

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

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
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
                        Debug.Log("zoom up");
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
                        Debug.Log("zoom out");
                    }
                }
            });

            // カメラ前進ポジションフラグON
            this.OnTriggerEnterAsObservable().Where(x => x.LikeBlock() || x.LikeWall())
                .Subscribe(_ => {
                    isForwardPosition = true;
                });

            // カメラ前進ポジションフラグON
            this.OnTriggerStayAsObservable().Where(x => x.LikeBlock() || x.LikeWall())
                 .Subscribe(_ => {
                     isForwardPosition = true;
                 });

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
