using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// 弾の処理
    /// </summary>
    public class BulletController : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////
        // フィールド

        private float secondSinceHit = 0f; // ヒットしてからの経過秒

        private bool hits = false; // 弾が何かにあたったかどうか

        private SoundSystem soundSystem; // サウンドシステム

        ///////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Awake is called when the script instance is being loaded.
        void Awake() {
            // Player から SoundSystem 取得
            soundSystem = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().GetSoundSystem();
        }

        // Start is called before the first frame update.
        void Start() {

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (hits) { // 何かに接触したら
                        secondSinceHit += Time.deltaTime; // 秒を加算して
                        if (secondSinceHit > 0.5f) { // 0.5秒後に自分を消去する
                            Destroy(gameObject);
                        }
                    }
                    if (transform.position.y < -100f) { // -100m以下なら流れ弾なので消去
                        Destroy(gameObject);
                    }
                });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable()
                .Subscribe(_ => {
                });
        }

        ///////////////////////////////////////////////////////////////////////////
        // イベントハンドラ

        /// <summary>
        /// 接触した対象の削除フラグを立てる、Player のHPを削る。
        /// </summary>
        void OnCollisionEnter(Collision collision) {
            if (!hits) { // 初回ヒットのみ破壊の対象
                         // Block に接触した場合
                if (collision.transform.name.Contains("Block")) {
                    collision.transform.GetComponent<BlockController>().DestroyWithDebris(transform); // 弾の transform を渡す
                    if (collision.transform.GetComponent<BlockController>().destroyable) { // この一撃で破壊可能かどうか
                        soundSystem.PlayExplosionClip(); // 破壊した
                    } else {
                        soundSystem.PlayDamageClip(); // ダメージを与えた
                    }
                    // Player に接触した場合
                } else if (collision.transform.tag.Contains("Player")) {
                    collision.transform.GetComponent<PlayerController>().DecrementLife();
                }
                // TODO: ボスの破壊
            }
            hits = true; // ヒットフラグON
        }
    }

}
