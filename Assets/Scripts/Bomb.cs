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

using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// 爆弾の処理
    /// @author h.adachi
    /// </summary>
    public class Bomb : MonoBehaviour {
        // プレーヤーと、敵(エネミー、砲台)が同じように使えるように。
        // 起爆スイッチを用意する。
        // 何秒後に爆発するか設定出来る。
        // プレイヤーが持ったら起爆スイッチON
        // 砲台から発射されたら起爆スイッチON(発射の時にはリジッドボディ有効:接地して数秒後にrb無効)
        // 砲台から発射された爆弾をプレイヤーが拾って攻撃する。

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        int timer = 5; // 点火されて爆発までの時間

        [SerializeField]
        GameObject pieceObject; // 破片生成用のプレハブ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        bool have = false; // 持たれたかどうか

        bool ignition = false; // 点火フラグ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        //public bool ignition { set => _ignition = value; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        void Start() {
            // 爆弾は
            this.UpdateAsObservable()
                .Where(_ => transform.parent != null && transform.parent.name.Equals("Player")) // プレイヤーに持たれたら
                .Subscribe(_ => {
                    have = true; // 持たれたフラグON
                });

            // 爆弾は
            this.UpdateAsObservable()
                .Where(_ => transform.parent == null && have) // プレイヤーが離したら
                .Subscribe(_ => {
                    ignition = true; // 点火される
                });

            // 爆弾は
            var _once = false;
            this.UpdateAsObservable()
                .Where(_ => ignition && !_once) // 点火されたら
                .Subscribe(_ => {
                    _once = true; // 一度だけ
                    // [timer]秒後に
                    Observable.Timer(System.TimeSpan.FromSeconds(timer))
                        .Subscribe(__ => {
                            if (transform.parent != null && transform.parent.IsPlayer()) { // まだプレイヤーに持たれていたら
                                transform.parent.gameObject.GetPlayer().PurgeFromBomb(); // 強制パージ TODO: 要る？
                            }
                            Destroy(gameObject); // 自分を削除して
                            explodePiece(6, 0.75f, 25); // 破片を飛ばす
                        });
                }).AddTo(this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// 破片を生成する。
        /// </summary>
        void explodePiece(int number = 8, float scale = 0.25f, int force = 15) {
            var _random = new System.Random(); // 乱数発生元
            var _min = -getRandomForce(force);
            var _max = getRandomForce(force);
            for (var i = 0; i < number; i++) { // 破片を生成する // TODO: 時間差で破片を生成する？
                var _piece = Instantiate(pieceObject);
                _piece.name += "_Piece"; // 破片には名前に "_Piece" を付加する
                _piece.transform.localScale = new Vector3(scale, scale, scale);
                _piece.transform.position = transform.position; // MEMO:親の位置を入れる必要があった
                if (_piece.GetRigidbody() == null) {
                    _piece.AddRigidbody();
                }
                _piece.GetComponent<Rigidbody>().isKinematic = false;
                var _v = new Vector3(_random.Next(_min, _max), _random.Next(_min, _max), _random.Next(_min, _max));
                _piece.GetRigidbody().AddForce(_v, ForceMode.Impulse);
                _piece.GetRigidbody().AddTorque(_v, ForceMode.Impulse);
                _piece.GetTransformsInChildren().ToList().ForEach(_child => {
                    if (_piece.name != _child.name) { // なぜか破片も破片の子リストにいるので除外
                        _child.parent = null;
                        Destroy(_child.gameObject); // 破片の子オブジェクトは最初に削除
                    }
                });
                _piece.GetBlock().autoDestroy = true; // 2秒後に破片を消去する
            }
        }

        /// <summary>
        /// 飛散する破片に加える力のランダム数値取得。
        /// </summary>
        int getRandomForce(int force) {
            var _random = new System.Random();
            return _random.Next((int) force / 2, (int) force * 2); // force の2分の1から2倍の範囲で
        }

    }

}
