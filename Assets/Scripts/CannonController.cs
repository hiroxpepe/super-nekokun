using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// 砲台の処理
    /// </summary>
    public class CannonController : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        [SerializeField]
        private GameObject player; // プレイヤー

        [SerializeField]
        private GameObject bullet; // 弾の元

        [SerializeField]
        private float bulletSpeed = 2000f; // 弾の速度

        private SoundSystem soundSystem; // サウンドシステム

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Awake is called when the script instance is being loaded.
        void Awake() {
            // Player から SoundSystem 取得
            soundSystem = player.GetComponent<PlayerController>().GetSoundSystem();
        }

        // Start is called before the first frame update.
        void Start() {
            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (transform.name.Contains("_Piece")) { return; } // '破片' は無視する

                    var _random = Mathf.FloorToInt(Random.Range(0.0f, 100.0f));
                    if (_random == 3.0f) { // 3の時だけ
                        float _SPEED = 50.0f; // 回転スピード
                        Vector3 _look = player.transform.position - transform.position; // ターゲット方向へのベクトル
                        Quaternion _rotation = Quaternion.LookRotation(new Vector3(_look.x, _look.y + 0.5f, _look.z)); // 回転情報に変換 // TODO: 距離が遠い時に
                        transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, _SPEED * Time.deltaTime); // 徐々に回転

                        // 弾の複製
                        var _bullet = Instantiate(bullet) as GameObject;

                        // 弾の位置
                        var _pos = transform.position + transform.forward * 1.7f; // 前進させないと弾のコライダーが自分に当たる
                        _bullet.transform.position = new Vector3(_pos.x, _pos.y + 0.5f, _pos.z);

                        // 弾の回転
                        _bullet.transform.rotation = transform.rotation;

                        // 弾へ加える力
                        var _force = transform.forward * bulletSpeed;

                        // 弾を発射
                        _bullet.GetComponent<Rigidbody>().AddForce(_force, ForceMode.Acceleration);
                        soundSystem.PlayShootClip();
                    }
                });
        }

        //// Update is called once per frame.
        //void FixedUpdate() {
        //    if (transform.name.Contains("_Piece")) { return; } // '破片' は無視する

        //    var _random = Mathf.FloorToInt(Random.Range(0.0f, 100.0f));
        //    if (_random == 3.0f) { // 3の時だけ
        //        float _SPEED = 50.0f; // 回転スピード
        //        Vector3 _look = player.transform.position - transform.position; // ターゲット方向へのベクトル
        //        Quaternion _rotation = Quaternion.LookRotation(new Vector3(_look.x, _look.y + 0.5f, _look.z)); // 回転情報に変換 // TODO: 距離が遠い時に
        //        transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, _SPEED * Time.deltaTime); // 徐々に回転

        //        // 弾の複製
        //        var _bullet = Instantiate(bullet) as GameObject;

        //        // 弾の位置
        //        var _pos = transform.position + transform.forward * 1.7f; // 前進させないと弾のコライダーが自分に当たる
        //        _bullet.transform.position = new Vector3(_pos.x, _pos.y + 0.5f, _pos.z);

        //        // 弾の回転
        //        _bullet.transform.rotation = transform.rotation;

        //        // 弾へ加える力
        //        var _force = transform.forward * bulletSpeed;

        //        // 弾を発射
        //        _bullet.GetComponent<Rigidbody>().AddForce(_force, ForceMode.Acceleration);
        //        soundSystem.PlayShootClip();
        //    }
        //}
    }

}
