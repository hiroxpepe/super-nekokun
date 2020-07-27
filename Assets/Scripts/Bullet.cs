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
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// 弾の処理
    /// @author h.adachi
    /// </summary>
    public class Bullet : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        float lifetime = 0.5f; // 消える時間

        ///////////////////////////////////////////////////////////////////////////
        // Fields

        float secondSinceHit = 0f; // ヒットしてからの経過秒

        bool hits = false; // 弾が何かにあたったかどうか

        SoundSystem soundSystem; // サウンドシステム

        ///////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            soundSystem = gameObject.GetSoundSystem(); // SoundSystem 取得
        }

        // Start is called before the first frame update.
        void Start() {

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (hits) { // 何かに接触したら
                        secondSinceHit += Time.deltaTime; // 秒を加算して
                        if (secondSinceHit > lifetime) { // lifetime秒後に自分を消去する
                            Destroy(gameObject);
                        }
                    }
                    if (transform.position.y < -100f) { // -100m以下なら流れ弾なので消去
                        Destroy(gameObject);
                    }
                });

            // 接触した対象の削除フラグを立てる、Player のHPを削る。
            this.OnCollisionEnterAsObservable()
                .Subscribe(x => {
                    if (!hits) { // 初回ヒットのみ破壊の対象
                        // Block に接触した場合 FIXME: "Block" と名前が付いているのに Block スクリプトが付いてない場合。
                        if (x.LikeBlock()) {
                            x.gameObject.GetBlock().DestroyWithDebris(transform); // 弾の transform を渡す
                            if (x.gameObject.GetBlock().destroyable) { // この一撃で破壊可能かどうか
                                soundSystem.PlayExplosionClip(); // 破壊した
                            } else {
                                soundSystem.PlayDamageClip(); // ダメージを与えた
                            }
                        // Player に接触した場合
                        } else if (x.IsPlayer()) {
                            x.gameObject.GetPlayer().DecrementLife();
                        }
                        // TODO: ボスの破壊
                    }
                    if (!x.LikeClone()) {
                        hits = true; // ヒットフラグON
                    }
                });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        ///// <summary>
        ///// 飛散する破片に加える力のランダム数値取得。// TODO: 効かない
        ///// </summary>
        //int getRandomLifetime(float lifetime) {
        //    var _random = new System.Random();
        //    return _random.Next((int) lifetime / 2, (int) lifetime * 2); // lifetime の2分の1から2倍の範囲で
        //}
    }

}
