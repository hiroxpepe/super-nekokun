using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// エネミーの処理
    /// </summary>
    public class EnemyController : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////
        // 設定・参照 (bool => is+形容詞、has+過去分詞、can+動詞原型、三単現動詞)

        [SerializeField]
        private SimpleAnimation simpleAnime;

        [SerializeField]
        private GameObject speechImage; // セリフ用吹き出し

        [SerializeField]
        private Vector3 speechOffset = new Vector3(0f, 0f, 0f); // セリフ位置オフセット

        ///////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        private SoundSystem soundSystem; // サウンドシステム

        private DoUpdate doUpdate; // Update() メソッド用フラグ構造体

        private DoFixedUpdate doFixedUpdate; // FixedUpdate() メソッド用フラグ構造体

        private GameObject plate; // 移動範囲のプレート

        private Text speechText; // セリフ用吹き出しテキスト

        ///////////////////////////////////////////////////////////////////////////////////////////
        // 更新 メソッド

        // Awake is called when the script instance is being loaded.
        void Awake() {
            doUpdate = DoUpdate.GetInstance(); // 状態フラグ構造体
            doFixedUpdate = DoFixedUpdate.GetInstance(); // 物理挙動フラグ構造体

            // SoundSystem 取得
            soundSystem = GameObject.Find("SoundSystem").GetComponent<SoundSystem>();

            // セリフ吹き出しテキスト取得
            speechText = speechImage.GetComponentInChildren<Text>();
        }

        // Start is called before the first frame update.
        void Start() {
            // プレーヤー参照取得
            var _player = GameObject.FindGameObjectWithTag("Player");
            var _rb = transform.GetComponent<Rigidbody>(); // Rigidbody は FixedUpdate の中で "だけ" 使用する

            var _fps = Application.targetFrameRate;
            var _ADJUST1 = 0f;
            if (_fps == 60) _ADJUST1 = 8f;
            if (_fps == 30) _ADJUST1 = 16f;

            // セリフ追従
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    speechImage.transform.position = RectTransformUtility.WorldToScreenPoint(
                        Camera.main,
                        transform.position + speechOffset
                    );
                });

            // セリフ非表示
            this.UpdateAsObservable()
                .Where(_ => !isRendered)
                .Subscribe(_ => {
                    beSilent();
                });

            // 初期値:索敵(デフォルト)
            doUpdate.ApplySearching();

            #region 索敵

            simpleAnime.Play("Walk"); // 歩くアニメ

            // 接地プレート取得
            this.OnCollisionEnterAsObservable()
                .Where(t => t.gameObject.name.Contains("Plate"))
                .Subscribe(t => {
                    plate = t.gameObject;
                });

            bool _idle = false;
            this.UpdateAsObservable()
                .Where(_ => doUpdate.searching && !doUpdate.rotate)
                .Subscribe(_ => {
                    // ランダムで移動位置指定
                    var _rand1 = Mathf.FloorToInt(Random.Range(3f, 9f));
                    var _rand2 = Mathf.FloorToInt(Random.Range(-3f, 3f));
                    var _rand3 = Mathf.FloorToInt(Random.Range(-3f, 3f));
                    doUpdate.rotate = true;
                    // [_rand1]秒後に
                    Observable.Timer(System.TimeSpan.FromSeconds(_rand1)) // FIXME: 60fpsの時は？
                        .Subscribe(__ => {
                            var _rand4 = Mathf.FloorToInt(Random.Range(1, 6));
                            if (_rand4 % 2 == 1) { // 偶数
                                simpleAnime.Play("Walk"); // 歩くアニメ
                                doFixedUpdate.ApplyWalk();
                                _idle = false;
                                transform.LookAt(new Vector3(
                                    transform.position.x + _rand2,
                                    transform.position.y,
                                    transform.position.z + _rand3
                                ));
                                doUpdate.rotate = false;
                                say("I change\ndirection.");
                            } else { // 奇数
                                simpleAnime.Play("Default"); // デフォルトアニメ
                                _idle = true;
                                doUpdate.rotate = false;
                                say("I wait...");
                            }
                        });
                });

            // 索敵中移動
            this.FixedUpdateAsObservable()
                .Where(_ => doUpdate.searching && !_idle)
                .Subscribe(_ => {
                    var _speed = _rb.velocity.magnitude; // 速度ベクトル取得
                    if (_speed < 1.1f) {
                        _rb.AddFor​​ce(Utils.TransformForward(transform.forward, _speed) * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                    }
                });

            // 障害物に当たった(壁)
            this.OnCollisionEnterAsObservable()
                .Where(t => t.gameObject.name.Contains("EnemyWall") ||
                       t.gameObject.name.Contains("Wall") && doUpdate.searching)
                .Subscribe(_ => {
                    transform.LookAt(new Vector3( // 接地プレートの中心を向く
                        plate.transform.position.x,
                        transform.position.y,
                        plate.transform.position.z
                    ));
                    say("I hit\nthe wall...");
                });

            // 障害物に当たった(ブロック)
            this.OnCollisionEnterAsObservable()
                .Where(t => t.gameObject.name.Contains("Block") && doUpdate.searching)
                .Subscribe(_ => {
                    transform.rotation = Quaternion.Euler(
                        transform.rotation.x,
                        transform.rotation.y + 180f,
                        transform.rotation.z
                    );
                    say("I hit\nthe block...");
                });

            // プレイヤー発見
            this.OnTriggerEnterAsObservable()
                .Where(t => t.gameObject.name.Equals("Player") && doUpdate.searching)
                .Subscribe(_ => {
                    doUpdate.ApplyChasing();
                    doFixedUpdate.ApplyRun();
                    say("I found\nthe cat!");
                });

            // プレイヤーロスト
            this.OnTriggerExitAsObservable()
                .Where(t => t.gameObject.name.Equals("Player") && !doUpdate.searching)
                .Subscribe(_ => {
                    doUpdate.ApplySearching();
                    doFixedUpdate.ApplyWalk();
                    say("I lost\nthe cat...");
                });

            #endregion

            #region 追跡

            // 追跡中
            this.UpdateAsObservable()
                .Where(_ => doUpdate.chasing)
                .Subscribe(_ => {
                    simpleAnime.Play("Run"); // 走るアニメ
                    transform.LookAt(new Vector3(
                        _player.transform.position.x,
                        transform.position.y,
                        _player.transform.position.z
                    ));
                    say("I'm chasing\nhim!");
                });

            // 追跡中移動
            this.FixedUpdateAsObservable()
                .Where(_ => doUpdate.chasing)
                .Subscribe(_ => {
                    var _speed = _rb.velocity.magnitude; // 速度ベクトル取得
                    if (doFixedUpdate.run) { // 走る
                        if (_speed < 3.25f) {
                            _rb.AddFor​​ce(Utils.TransformForward(transform.forward, _speed) * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                        }
                    } else if (doFixedUpdate.walk) { // 歩く
                        if (_speed < 1.1f) {
                            _rb.AddFor​​ce(Utils.TransformForward(transform.forward, _speed) * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                        }
                    }
                });

            #endregion

            #region 攻撃

            // パンチ攻撃中
            this.UpdateAsObservable()
                .Where(_ => doUpdate.attacking)
                .Subscribe(_ => {
                    simpleAnime.Play("Punch"); // パンチアニメ
                    transform.LookAt(new Vector3(
                        _player.transform.position.x,
                        transform.position.y,
                        _player.transform.position.z
                    ));
                });

            // プレイヤー接触時にパンチ攻撃
            this.OnCollisionEnterAsObservable()
                .Where(t => t.gameObject.name.Equals("Player") && !doUpdate.attacking)
                .Subscribe(t => {
                    doUpdate.ApplyAttacking();
                    soundSystem.PlayDamageClip(); // FIXME: パンチ効果音は、頭に数ミリセック無音を仕込む
                    say("Punch!", 65);
                    _player.transform.GetComponent<PlayerController>().DecrementLife();
                    _player.transform.GetComponent<PlayerController>().DamagedByEnemy(transform.forward);
                    Observable.TimerFrame(12) // FIXME: 60fpsの時は？
                        .Subscribe(__ => {
                            doUpdate.ApplyChasing();
                        });
                });

            // プレイヤー接触中はパンチ攻撃を繰り返す
            bool _wait = false;
            this.OnCollisionStayAsObservable()
                .Where(t => t.gameObject.name.Equals("Player") && !doUpdate.attacking)
                .Subscribe(t => {
                    doUpdate.ApplyChasing();
                    Observable.TimerFrame(24).Where(_ => !_wait).Subscribe(_ => { // FIXME: 60fpsの時は？
                        _wait = true;
                        doUpdate.ApplyAttacking();
                        soundSystem.PlayDamageClip(); // FIXME: パンチ効果音は、頭に数ミリセック無音を仕込む
                        say("Take this!", 60);
                        _player.transform.GetComponent<PlayerController>().DecrementLife();
                        _player.transform.GetComponent<PlayerController>().DamagedByEnemy(transform.forward);
                        Observable.TimerFrame(12) // FIXME: 60fpsの時は？
                            .Subscribe(__ => {
                                doUpdate.ApplyChasing();
                                _wait = false;
                            });
                    });
                });

            #endregion
        }

        #region DoUpdate

        ///////////////////////////////////////////////////////////////////////////////////////////
        // プライベートメソッド(キャメルケース)

        /// <summary>
        /// セリフ用吹き出しにセリフを表示する。 // FIXME: 吹き出しの形
        /// </summary>
        private void say(string text, int size = 60, double time = 0.5d) { // TODO: バッファー？
            speechText.text = text;
            speechText.fontSize = size;
            speechImage.SetActive(true);
            Observable.Timer(System.TimeSpan.FromSeconds(time))
                .First()
                .Subscribe(_ => {
                    speechImage.SetActive(false);
                });
        }

        /// <summary>
        /// セリフ用吹き出しを非表示にする。
        /// </summary>
        private void beSilent() {
            speechImage.SetActive(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // カメラ映り

        private const string MAIN_CAMERA_TAG_NAME = "MainCamera";  // メインカメラに付いているタグ名

        // メインカメラに表示されているか
        private bool isRendered = false;

        void OnBecameVisible() { // メインカメラに映った時
            if (Camera.current.tag == MAIN_CAMERA_TAG_NAME) {
                isRendered = true;
                Debug.Log("isRendered: " + isRendered);
            }
        }

        void OnBecameInvisible() { // メインカメラに映らなくなった時
            if (Camera.current != null && Camera.current.tag == MAIN_CAMERA_TAG_NAME) {
                isRendered = false;
                Debug.Log("isRendered: " + isRendered);
            }
        }

        /// <summary>
        /// Update() メソッド用の構造体。
        /// </summary>
        protected struct DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // フィールド(アンダースコアorキャメルケース)

            private bool _grounded; // 接地フラグ
            private bool _searching; // 索敵中フラグ
            private bool _rotate; //回転中フラグ
            private bool _chasing; // 追跡中フラグ
            private bool _attacking; // 攻撃中フラグ

            ///////////////////////////////////////////////////////////////////////////////////////////
            // プロパティ(キャメルケース)

            public bool grounded { get => _grounded; set => _grounded = value; }

            public bool searching { get => _searching; }

            public bool rotate { get => _rotate; set => _rotate = value; }

            public bool chasing { get => _chasing; }

            public bool attacking { get => _attacking; }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // コンストラクタ(パスカルケース)

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static DoUpdate GetInstance() {
                var _instance = new DoUpdate();
                _instance.ResetState();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // パブリックメソッド(パスカルケース)

            public void ApplySearching() {
                _searching = true;
                _chasing = false;
                _attacking = false;
                _rotate = false;
            }

            public void ApplyChasing() {
                _searching = false;
                _chasing = true;
                _attacking = false;
                _rotate = false;
            }

            public void ApplyAttacking() {
                _searching = false;
                _chasing = false;
                _attacking = true;
                _rotate = false;
            }

            public void ResetState() {
                _grounded = false;
                _searching = false;
                _chasing = false;
                _attacking = false;
                _rotate = false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // プライベートメソッド(キャメルケース)
        }

        #endregion

        #region DoFixedUpdate

        /// <summary>
        /// FixedUpdate() メソッド用の構造体。
        /// </summary>
        protected struct DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // フィールド

            //private bool _idol;
            private bool _run;
            private bool _walk;
            private bool _jump;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // プロパティ(キャメルケース)

            //public bool idol { get => _idol; set => _idol = value; }

            public bool run { get => _run; }

            public bool walk { get => _walk; }

            public bool jump { get => _jump; set => _jump = value; }


            ///////////////////////////////////////////////////////////////////////////////////////////
            // コンストラクタ

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static DoFixedUpdate GetInstance() {
                var _instance = new DoFixedUpdate();
                _instance.ResetMotion();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // パブリックメソッド

            public void ApplyRun() {
                _run = false;
                _walk = true;
            }

            public void ApplyWalk() {
                _run = true;
                _walk = false;
            }

            /// <summary>
            /// 全フィールドの初期化
            /// </summary>
            public void ResetMotion() {
                //_idol = false;
                _run = false;
                _walk = false;
                _jump = false;
            }
        }

        #endregion
    }

}