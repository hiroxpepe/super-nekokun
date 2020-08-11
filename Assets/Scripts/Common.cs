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
    /// オブジェクトの共通処理
    /// @author h.adachi
    /// </summary>
    public class Common : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        bool canClimb; // 上ることが出来るかフラグ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Vector3 defaultPosition; // 初期配置位置

        Vector3[] previousPosition; // 10フレ分の位置を保存

        AutoDestroyParam autoDestroyParam; // 自動削除用パラメータクラス

        ShockParam shockParam; // 衝撃用パラメータクラス

        Vector3 bulletUp; // 当たった弾のベクトルUp

        Vector3 bulletForward; // 当たった弾のベクトルForward

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        public bool climbable { get => canClimb; } // 上ることが出来るかフラグを返す

        public float autoDestroyAfter { set { autoDestroyParam.enable = true; autoDestroyParam.limit = getRandomLimit(value); } } // n秒後に自動的に消去する

        public bool autoDestroy { set { autoDestroyParam.enable = value; autoDestroyParam.limit = getRandomLimit(2.0f); } } // 2秒後に自動的に消去する

        float getRandomLimit(float limit) { // 自動削除される秒数のランダム要素
            var _random = new System.Random();
            return _random.Next(2, (int) limit * 25); // 2秒から (limit * 25倍) 秒の範囲で
        }

        /// <summary>
        /// 何かで衝撃を受ける
        /// </summary>
        public Transform shockedBy {
            set {
                bulletForward = value.forward;
                bulletUp = value.up;
                shockParam.limit = 2.0f; // ※limitは実質固定値
                if (shockParam.enable == false) { // 初回 or 移動動作終了
                    shockParam.enable = true;
                } else if (shockParam.enable == true) { // 移動動作中に再ヒット
                    shockParam.reHits = true;
                }
                if (value.LikeBullet()) { // 弾の場合
                    shockParam.hitsType = HitsType.Bullet;
                } else if (value.IsPlayer()) { // Player の場合
                    shockParam.hitsType = HitsType.Player;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        protected void Awake() {
            autoDestroyParam = AutoDestroyParam.GetInstance();
            shockParam = ShockParam.GetInstance();
            previousPosition = new Vector3[10];
        }

        // Start is called before the first frame update.
        protected void Start() {
            defaultPosition = transform.position;

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    doAutoDestroy(); // 自動削除
                    doShock(); // 弾に当たった時の衝撃
                    if (transform.position.y < -100f) { // -100m以下ならLevelから落ちたので消去
                        Destroy(gameObject);
                    }
                });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable()
                .Subscribe(_ => {
                    cashPreviousPosition(); // 10フレ前分の位置情報保存
                });
        }

        /// <summary>
        /// 10フレ前分の位置情報保存する。※使用していない
        /// </summary>
        void cashPreviousPosition() {
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// 弾に当たった衝撃時の挙動。
        /// </summary>
        void doShock() {
            if (shockParam.reHits) { // 再ヒット時にリセット
                transform.position = shockParam.position; // 完全に元の位置に戻る
                shockParam.second = 0f;
                shockParam.moved = false;
                shockParam.reHits = false;
                return; // 1フレ分終了する
            }
            if (shockParam.enable) { // 衝撃有効なら
                shockParam.second += Time.deltaTime; // 経過秒加算
                if (shockParam.second > shockParam.limit) { // リミット時間が来たら
                    transform.position = shockParam.position; // 完全に元の位置に戻る
                    shockParam.Reset(); // パラメータリセット
                } else {
                    if (!shockParam.moved) { // 衝撃で移動するのは初回のみ
                        if (shockParam.position == Vector3.zero) shockParam.position = transform.position; // 現在の位置保存
                        moveByShocked(); // 衝撃で動く
                        shockParam.moved = true;
                    }
                    // 徐々に元の位置に戻ろうとする
                    transform.position = Vector3.Lerp(transform.position, shockParam.position, 0.25f); // 戻る調整値
                }
            }
        }

        /// <summary>
        /// 自動で自身を削除する。
        /// </summary>
        void doAutoDestroy() {
            if (autoDestroyParam.enable) { // 自動削除有効なら
                autoDestroyParam.second += Time.deltaTime;
                if (autoDestroyParam.second > 0.1f) { // 0.1秒後にコライダー判定ON
                    gameObject.GetCollider().enabled = true;
                }
                fadeoutToDestroy(); // 徐々に透明化する
                if (autoDestroyParam.second > autoDestroyParam.limit) { // 時間が来たら
                    Destroy(gameObject); // 自分を削除する
                }
            }
        }

        /// <summary>
        /// 自動で徐々に透明化する。
        /// </summary>
        void fadeoutToDestroy() {
            if (autoDestroyParam.second > autoDestroyParam.limit - 0.8f) { // 時間0.8秒前から
                var _render = gameObject.GetMeshRenderer();
                var _materialList = _render.materials;
                foreach (var _material in _materialList) {
                    Utils.SetRenderingMode(_material, RenderingMode.Fade);
                    var _color = _material.color;
                    _color.a = autoDestroyParam.limit - autoDestroyParam.second; // 徐々に透明化
                    _material.color = _color;
                }
            }
        }

        /// <summary>
        /// 弾の衝撃で少し後ろ上に動く、Player が下から叩いて少し上に動く。
        /// </summary>
        void moveByShocked() {
            var _ADJUST = 3;
            if (shockParam.hitsType == HitsType.Bullet) {
                transform.position = shockParam.position + (bulletForward + bulletUp) / _ADJUST;
            } else if (shockParam.hitsType == HitsType.Player) {
                transform.position = shockParam.position + bulletUp * (_ADJUST / 2.9f); // ※調整値
            }
        }

        #region AutoDestroyParam

        /// <summary>
        /// 自動削除用のパラメータークラス。
        /// </summary>
        protected class AutoDestroyParam {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            float _second; // 削除までの秒加算用

            float _limit; // 何秒後に削除されるか

            bool _enable; // 自動削除発動フラグ

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            AutoDestroyParam(float second, float limit, bool enable) {
                _second = second;
                _limit = limit;
                _enable = enable;
            }

            public static AutoDestroyParam GetInstance() {
                return new AutoDestroyParam(0f, 0f, false);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float second { get => _second; set => _second = value; }

            public float limit { get => _limit; set => _limit = value; }

            public bool enable { get => _enable; set => _enable = value; }

        }

        #endregion

        #region ShockParam

        /// <summary>
        /// 衝撃用のパラメータークラス。
        /// </summary>
        protected class ShockParam {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            float _second; // 秒加算用

            float _limit; // 何秒で戻るか

            bool _enable; // 衝撃発動フラグ

            bool _moved; // 移動したかフラグ

            bool _reHits; // 移動中に再ヒットしたかフラグ

            Vector3 _position; // 衝撃時の位置保存

            HitsType _hitsType; // ぶつかった相手の種別

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            ShockParam(float second, float limit, bool enable, bool moved, bool reHits) {
                _second = second;
                _limit = limit;
                _enable = enable;
                _moved = moved;
                _reHits = reHits;
                _position = new Vector3();
                _hitsType = HitsType.Other;
            }

            public static ShockParam GetInstance() {
                return new ShockParam(0f, 0f, false, false, false);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float second { get => _second; set => _second = value; }

            public float limit { get => _limit; set => _limit = value; }

            public bool enable { get => _enable; set => _enable = value; }

            public bool moved { get => _moved; set => _moved = value; }

            public bool reHits { get => _reHits; set => _reHits = value; }

            public Vector3 position { get => _position; set => _position = value; }

            public HitsType hitsType { get => _hitsType; set => _hitsType = value; }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public void Reset() {
                _second = 0f;
                _enable = false;
                _moved = false;
                _reHits = false;
                _position = Vector3.zero;
                _hitsType = HitsType.Other;
            }
        }

        #endregion
    }

}
