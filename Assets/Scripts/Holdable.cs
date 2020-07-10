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
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// 持たれる処理
    /// @author h.adachi
    /// </summary>
    public class Holdable : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 設定・参照 (bool => is+形容詞、has+過去分詞、can+動詞原型、三単現動詞)

        [SerializeField]
        bool canHold = true; // 持たれることが出来るかフラグ

        [SerializeField]
        float holdedHeight = 0.5f; // 持たれる時の高さ調整値

        [SerializeField]
        float holdedMargin = 0.5f; // 持たれる時のクリアランス

        [SerializeField]
        float holdedTilt = 15f; // 持たれる時の傾き

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        bool isGrounded; // 接地フラグ

        Transform leftHandTransform; // Player 持たれる時の左手の位置 Transform

        Transform rightHandTransform; // Player 持たれる時の右手の位置 Transform

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プロパティ(キャメルケース: 名詞、形容詞)

        /// <summary>
        /// プレイヤーに持たれているかどうか。
        /// </summary>
        public bool holdedByPlayer {
            get {
                if (transform.parent == null) {
                    return false;
                } else if (transform.parent.gameObject.tag.Equals("Player")) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // パブリックメソッド(パスカルケース)

        // 持たれる実装用
        public Transform GetLeftHandTransform() { // キャラにIKで持たれる用
            return leftHandTransform;
        }

        public Transform GetRightHandTransform() { // キャラにIKで持たれる用
            return rightHandTransform;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Awake is called when the script instance is being loaded.
        protected void Awake() {
        }

        // Start is called before the first frame update.
        void Start() {
            // 持たれる実装用
            if (canHold) {
                isGrounded = false; // MEMO: 初期設定の位置そのまま？
                // 持たれる時の手の位置オブジェクト初期化
                var _leftHandGameObject = new GameObject("LeftHand");
                var _rightHandGameObject = new GameObject("RightHand");
                leftHandTransform = _leftHandGameObject.transform;
                rightHandTransform = _rightHandGameObject.transform;
                leftHandTransform.parent = transform;
                rightHandTransform.parent = transform;
                leftHandTransform.localPosition = Vector3.zero;
                rightHandTransform.localPosition = Vector3.zero;
            }

            // 持たれる実装用
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    // 持たれる実装用
                    if (!canHold) {
                        return;
                    } else {
                        if (isGrounded && transform.parent != null && transform.parent.gameObject.tag.Equals("Player")) {
                            // 親が Player になった時
                            isGrounded = false; // 接地フラグOFF
                        } else if (!isGrounded && transform.parent != null && transform.parent.gameObject.tag.Equals("Player")) {
                            // 親が Player 継続なら
                            if (!transform.parent.GetComponent<PlayerController>().Faceing) { // プレイヤーの移動・回転を待つ
                                if (transform.parent.transform.position.y > transform.position.y + 0.2f) { // 0.2fは調整値
                                    beHolded(8.0f); // 上から持ち上げられる
                                } else {
                                    beHolded(); // 横から持ち上げられる
                                }
                            }
                        } else if (!isGrounded && (transform.parent == null || !transform.parent.gameObject.tag.Equals("Player"))) {
                            // 親が Player でなくなれば落下する
                            var _ray = new Ray(transform.position, new Vector3(0, -1f, 0)); // 下方サーチするレイ作成
                            if (Physics.Raycast(_ray, out RaycastHit _hit, 20f)) { // 下方にレイを投げて反応があった場合
#if DEBUG
                                Debug.DrawRay(_ray.origin, _ray.direction, Color.yellow, 3, false);
#endif
                                var _distance = (float) Math.Round(_hit.distance, 3, MidpointRounding.AwayFromZero);
                                if (_distance < 0.2f) { // ある程度距離が近くなら
                                    isGrounded = true; // 接地とする
                                    var _top = getHitTop(_hit.transform.gameObject); // その後、接地したとするオブジェクトのTOPを調べて
                                    transform.localPosition = Utils.ReplaceLocalPositionY(transform, _top); // その位置に置く
                                    align2(); // 位置調整
                                }
                            }
                            if (!isGrounded) { // まだ接地してなければ落下する
                                transform.localPosition -= new Vector3(0f, 5.0f * Time.deltaTime, 0f); // 5.0f は調整値
                            }
                        }
                    }
                });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プライベートメソッド(キャメルケース: 動詞)

        /// <summary>
        /// ブロックの位置をグリッドに合わせ微調整する。
        /// </summary>
        void align2() {
            transform.position = new Vector3(
                (float) Math.Round(transform.position.x * 2, 0, MidpointRounding.AwayFromZero) / 2, // 0.5単位にする為、2倍して2で割る
                (float) Math.Round(transform.position.y * 2, 0, MidpointRounding.AwayFromZero) / 2,
                (float) Math.Round(transform.position.z * 2, 0, MidpointRounding.AwayFromZero) / 2
            );
            transform.localRotation = new Quaternion(0, 0f, 0f, 0f);
        }

        // 衝突したオブジェクトの側面に当たったか判定する
        float getHitTop(GameObject hit) {
            float _height = hit.GetComponent<Renderer>().bounds.size.y; // 対象オブジェクトの高さ取得 
            float _y = hit.transform.position.y; // 対象オブジェクトのy座標取得(※0基点)
            float _top = _height + _y; // 対象オブジェクトのTOP取得
            return _top;
        }

        /// <summary>
        /// Player に押された方向を列挙体で返す。
        /// </summary>
        PushedDirection getPushedDirection(Vector3 forwardVector) {
            var _fX = (float) Math.Round(forwardVector.x);
            var _fY = (float) Math.Round(forwardVector.y);
            var _fZ = (float) Math.Round(forwardVector.z);
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                return PushedDirection.PositiveZ;
            }
            if (_fX == 0 && _fZ == -1) { // Z軸負方向
                return PushedDirection.NegativeZ;
            }
            if (_fX == 1 && _fZ == 0) { // X軸正方向
                return PushedDirection.PositiveX;
            }
            if (_fX == -1 && _fZ == 0) { // X軸負方向
                return PushedDirection.NegativeX;
            }
            // ここに来たら二軸の差を判定する TODO: ロジック再確認
            float _abX = Math.Abs(forwardVector.x);
            float _abZ = Math.Abs(forwardVector.z);
            if (_abX > _abZ) {
                if (_fX == 1) { // X軸正方向
                    return PushedDirection.PositiveX;
                }
                if (_fX == -1) { // X軸負方向
                    return PushedDirection.NegativeX;
                }
            } else if (_abX < _abZ) {
                if (_fZ == 1) { // Z軸正方向
                    return PushedDirection.PositiveZ;
                }
                if (_fZ == -1) { // Z軸負方向
                    return PushedDirection.NegativeZ;
                }
            }
            return PushedDirection.None; // 判定不明
        }

        /// <summary>
        /// プレイヤーに持ち上げられる。
        /// </summary>
        void beHolded(float speed = 2.0f) {
            if (transform.localPosition.y < holdedHeight) { // 親に持ち上げられた位置に移動する: 0.6fは調整値
                var _direction = getPushedDirection(transform.parent.forward);
                if (_direction == PushedDirection.PositiveZ) { // Z軸正方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x,
                        transform.position.y + speed * Time.deltaTime, // 調整値
                        transform.parent.transform.position.z + holdedMargin // 調整値
                    );
                    transform.rotation = Quaternion.Euler(-holdedTilt, 0f, 0f); // 15度傾ける
                } else if (_direction == PushedDirection.NegativeZ) { // Z軸負方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x,
                        transform.position.y + speed * Time.deltaTime,
                        transform.parent.transform.position.z - holdedMargin
                    );
                    transform.rotation = Quaternion.Euler(holdedTilt, 0f, 0f);
                } else if (_direction == PushedDirection.PositiveX) { // X軸正方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x + holdedMargin,
                        transform.position.y + speed * Time.deltaTime,
                        transform.parent.transform.position.z
                    );
                    transform.rotation = Quaternion.Euler(0f, 0f, holdedTilt);
                } else if (_direction == PushedDirection.NegativeX) { // X軸負方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x - holdedMargin,
                        transform.position.y + speed * Time.deltaTime,
                        transform.parent.transform.position.z
                    );
                    transform.rotation = Quaternion.Euler(0f, 0f, -holdedTilt);
                }
            }
        }
    }

}
