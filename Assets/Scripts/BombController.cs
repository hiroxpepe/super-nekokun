using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// 爆弾の処理
    /// </summary>
    public class BombController : MonoBehaviour {
        // プレーヤーと、敵(エネミー、砲台)が同じように使えるように。
        // 起爆スイッチを用意する。
        // 何秒後に爆発するか設定出来る。
        // プレイヤーが持ったら起爆スイッチON
        // 砲台から発射されたら起爆スイッチON(発射の時にはリジッドボディ有効:接地して数秒後にrb無効)
        // 砲台から発射された爆弾をプレイヤーが拾って攻撃する。

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 設定・参照 (bool => is+形容詞、has+過去分詞、can+動詞原型、三単現動詞)

        [SerializeField]
        private int timer = 10;

        [SerializeField]
        private GameObject prefabForPiece; // 破片生成用のプレハブ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        private bool _ignition; // 点火フラグ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プロパティ(キャメルケース: 名詞、形容詞)

        public bool ignition { set => _ignition = value; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Start is called before the first frame update
        void Start() {
            // 爆弾は
            this.UpdateAsObservable()
                .Where(_ => transform.parent != null && transform.parent.name.Equals("Player")) // プレイヤーに持たれたら
                .Subscribe(_ => {
                    //Debug.Log("点火！");
                    ignition = true; // 自動的に点火される
                });

            // 爆弾は
            var _once = false;
            this.UpdateAsObservable()
                .Where(_ => _ignition && !_once) // 点火されたら
                .Subscribe(_ => {
                    _once = true; // 一度だけ
                    // [timer]秒後に
                    Observable.Timer(System.TimeSpan.FromSeconds(timer))
                        .Subscribe(__ => {
                            if (transform.parent != null && transform.parent.name.Equals("Player")) { // まだプレイヤーに持たれていたら
                                transform.parent.GetComponent<PlayerController>().PurgeFromBomb(); // 強制パージ
                            }
                            //Debug.Log("爆発！");
                            Destroy(gameObject); // 自分を削除して
                            explodePiece(8, 0.75f, 60); // 破片を飛ばす
                        });
                }).AddTo(this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プライベートメソッド(キャメルケース: 動詞)

        /// <summary>
        /// 破片を生成する。
        /// </summary>
        private void explodePiece(int number = 8, float scale = 0.25f, int force = 15) {
            var _random = new System.Random(); // 乱数発生元
            var _min = -getRandomForce(force);
            var _max = getRandomForce(force);
            for (var i = 0; i < number; i++) { // 破片を生成する // TODO: 時間差で破片を生成する？
                var _piece = Instantiate(prefabForPiece);
                _piece.name += "_Piece"; // 破片には名前に "_Piece" を付加する
                _piece.transform.localScale = new Vector3(scale, scale, scale);
                _piece.transform.position = transform.position; // MEMO:親の位置を入れる必要があった
                if (_piece.GetComponent<Rigidbody>() == null) {
                    _piece.AddComponent<Rigidbody>();
                }
                _piece.GetComponent<Rigidbody>().isKinematic = false;
                var _v = new Vector3(_random.Next(_min, _max), _random.Next(_min, _max), _random.Next(_min, _max));
                _piece.GetComponent<Rigidbody>().AddForce(_v, ForceMode.Impulse);
                _piece.GetComponent<Rigidbody>().AddTorque(_v, ForceMode.Impulse);
                _piece.GetComponentsInChildren<Transform>().ToList().ForEach(_child => {
                    if (_piece.name != _child.name) { // なぜか破片も破片の子リストにいるので除外
                        _child.parent = null;
                        Destroy(_child.gameObject); // 破片の子オブジェクトは最初に削除
                    }
                });
                _piece.GetComponent<BlockController>().autoDestroy = true; // 2秒後に破片を消去する
            }
        }

        /// <summary>
        /// 飛散する破片に加える力のランダム数値取得。
        /// </summary>
        private int getRandomForce(int force) {
            var _random = new System.Random();
            return _random.Next((int) force / 2, (int) force * 2); // force の2分の1から2倍の範囲で
        }

    }

}
