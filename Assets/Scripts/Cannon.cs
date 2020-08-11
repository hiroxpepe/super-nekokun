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
    /// 砲台の処理
    /// @author h.adachi
    /// </summary>
    public class Cannon : MonoBehaviour {

        // TODO: 砲台から弾が飛んでくる：赤-半誘導弾、青-通常弾

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        GameObject playerObject; // プレイヤー

        [SerializeField]
        GameObject bulletObject; // 弾の元

        [SerializeField]
        float bulletSpeed = 2000f; // 弾の速度

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        SoundSystem soundSystem; // サウンドシステム

        ///////////////////////////////////////////////////////////////////////////////////////////////
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
                    if (transform.LikePiece()) { return; } // '破片' は無視する
                    var _random = Mathf.FloorToInt(Random.Range(0.0f, 175.0f));
                    if (_random == 3.0f) { // 3の時だけ
                        float _SPEED = 50.0f; // 回転スピード
                        Vector3 _look = playerObject.transform.position - transform.position; // ターゲット方向へのベクトル
                        Quaternion _rotation = Quaternion.LookRotation(new Vector3(_look.x, _look.y + 0.5f, _look.z)); // 回転情報に変換 // TODO: 距離が遠い時に
                        transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, _SPEED * Time.deltaTime); // 徐々に回転

                        // 弾の複製
                        var _bullet = Instantiate(bulletObject) as GameObject;

                        // 弾の位置
                        var _pos = transform.position + transform.forward * 1.7f; // 前進させないと弾のコライダーが自分に当たる
                        _bullet.transform.position = new Vector3(_pos.x, _pos.y + 0.5f, _pos.z); // 0.5f は Y軸を中心に合わせる為

                        // 弾の回転
                        _bullet.transform.rotation = transform.rotation;

                        // 弾へ加える力
                        var _force = transform.forward * bulletSpeed;

                        // 弾を発射
                        _bullet.GetRigidbody().AddForce(_force, ForceMode.Acceleration);
                        soundSystem.PlayShootClip();
                    }
                });
        }
    }

}
