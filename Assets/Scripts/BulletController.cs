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
            // SoundSystem 取得
            soundSystem = GameObject.Find("SoundSystem").GetComponent<SoundSystem>();
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

            // 接触した対象の削除フラグを立てる、Player のHPを削る。
            this.OnCollisionEnterAsObservable() // TODO: 他の物にHitしてるのでは？
                .Subscribe(t => {
                    if (!hits) { // 初回ヒットのみ破壊の対象
                        //Debug.Log("Hit to: " + t.gameObject.name);
                        // Block に接触した場合
                        if (t.transform.name.Contains("Block")) {
                            t.transform.GetComponent<BlockController>().DestroyWithDebris(transform); // 弾の transform を渡す
                            if (t.transform.GetComponent<BlockController>().destroyable) { // この一撃で破壊可能かどうか
                                soundSystem.PlayExplosionClip(); // 破壊した
                            } else {
                                soundSystem.PlayDamageClip(); // ダメージを与えた
                            }
                            //Debug.Log("BlockにHit!");
                            // Player に接触した場合
                        } else if (t.transform.tag.Contains("Player")) {
                            t.transform.GetComponent<PlayerController>().DecrementLife();
                        }
                        // TODO: ボスの破壊
                    }
                    hits = true; // ヒットフラグON
                });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable()
                .Subscribe(_ => {
                });
        }
    }

}
