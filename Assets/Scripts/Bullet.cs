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
