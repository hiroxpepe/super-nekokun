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
    /// アイテムの処理
    /// ※持てるアイテムにリジッドボディは扱いずらいので外している
    /// @author h.adachi
    /// </summary>
    public class Item : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        bool isFloat = false; // 浮遊フラグ ※現状で持てない

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        bool isGrounded; // 接地フラグ

        Transform leftHandTransform; // Player 持たれる時の左手の位置 Transform

        Transform rightHandTransform; // Player 持たれる時の右手の位置 Transform

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// プレイヤーに持たれているかどうか。
        /// </summary>
        public bool holdedByPlayer {
            get {
                if (transform.parent == null) {
                    return false;
                } else if (transform.parent.gameObject.IsPlayer()) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        public Transform GetLeftHandTransform() { // キャラにIKで持たれる用
            return leftHandTransform;
        }

        public Transform GetRightHandTransform() { // キャラにIKで持たれる用
            return rightHandTransform;
        }

        ///////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        void Start() {
            isGrounded = false;
            // 持たれる時の手の位置オブジェクト初期化
            var _leftHandGameObject = new GameObject("LeftHand");
            var _rightHandGameObject = new GameObject("RightHand");
            leftHandTransform = _leftHandGameObject.transform;
            rightHandTransform = _rightHandGameObject.transform;
            leftHandTransform.parent = transform;
            rightHandTransform.parent = transform;
            leftHandTransform.localPosition = Vector3.zero;
            rightHandTransform.localPosition = Vector3.zero;

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(_ => {
                });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (isFloat) { // 浮遊フラグONはリターン
                        return;
                    }
                    if (isGrounded && transform.parent != null && transform.parent.gameObject.IsPlayer()) {
                        // 親が Player になった時
                        isGrounded = false; // 接地フラグOFF
                    } else if (!isGrounded && transform.parent != null && transform.parent.gameObject.IsPlayer()) {
                        // 親が Player 継続なら
                        if (transform.localPosition.y < 0.35f) { // 親の持ち上げられた位置に移動する
                            transform.localPosition += new Vector3(0f, 1.5f * Time.deltaTime, 0f);
                        }
                    } else if (!isGrounded && (transform.parent == null || !transform.parent.gameObject.IsPlayer())) {
                        // 親が Player でなくなれば落下する
                        var _ray = new Ray(transform.position, new Vector3(0, -1f, 0)); // 下方サーチするレイ作成
                        if (Physics.Raycast(_ray, out RaycastHit _hit, 20f)) { // 下方にレイを投げて反応があった場合
#if DEBUG
                            Debug.DrawRay(_ray.origin, _ray.direction, Color.yellow, 3, false);
#endif
                            var _distance = (float) Math.Round(_hit.distance, 3, MidpointRounding.AwayFromZero);
                            if (_distance < 0.1) { // ある程度距離が近くなら
                                isGrounded = true; // 接地とする
                                var _top = getHitTop(_hit.transform.gameObject); // その後、接地したとするオブジェクトのTOPを調べて
                                transform.localPosition = Utils.ReplaceLocalPositionY(transform, _top); // その位置に置く
                            }
                        }
                        if (!isGrounded) { // まだ接地してなければ落下する
                            transform.localPosition -= new Vector3(0f, 4.0f * Time.deltaTime, 0f); // 4.0f は調整値
                        }
                    }
                });

            // 接地した
            this.OnCollisionEnterAsObservable().Where(x => x.LikeGround() || x.LikeBlock())
                .Subscribe(x => {
                    isGrounded = true; // FIXME: 開始早々には効かない？
                });

            // 下のブロックが破壊された
            this.OnCollisionExitAsObservable().Where(x => x.LikeBlock())
                .Subscribe(_ => {
                    isFloat = false;
                    isGrounded = false; // 下のブロックが破壊された
                });
        }

        // FIXME: アイテムはブロックの子にする。移動するブロックの上でアイテムもいどうする。ブロックが破壊されたらアイテムは落下する
        // FIXME: ブロックの上には一つしかアイテムが置けない

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        // 衝突したオブジェクトの側面に当たったか判定する
        float getHitTop(GameObject hit) {
            float _height = hit.GetRenderer().bounds.size.y; // 対象オブジェクトの高さ取得 
            float _y = hit.transform.position.y; // 対象オブジェクトのy座標取得(※0基点)
            float _top = _height + _y; // 対象オブジェクトのTOP取得
            return _top;
        }
    }

}
