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
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// エネミーの処理
    /// @author h.adachi
    /// </summary>
    public class EnemyController : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        SimpleAnimation simpleAnime;

        [SerializeField]
        GameObject speechImage; // セリフ用吹き出し

        [SerializeField]
        Vector3 speechOffset = new Vector3(0f, 0f, 0f); // セリフ位置オフセット

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        SoundSystem soundSystem; // サウンドシステム

        DoUpdate doUpdate; // Update() メソッド用フラグクラス

        DoFixedUpdate doFixedUpdate; // FixedUpdate() メソッド用フラグクラス

        Speech speech; // セリフ用クラス

        float distance; // プレイヤーとの距離

        GameObject plate; // 移動範囲のプレート

        ///////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            doUpdate = DoUpdate.GetInstance(); // 状態フラグクラス
            doFixedUpdate = DoFixedUpdate.GetInstance(); // 物理挙動フラグクラス
            speech = Speech.GetInstance(); // セリフ用クラス

            // SoundSystem 取得
            soundSystem = GameObject.Find("SoundSystem").GetComponent<SoundSystem>();

            // セリフ吹き出し大きさ取得
            var _rect = speechImage.GetComponent<RectTransform>();
            speech.X = _rect.sizeDelta.x;
            speech.Y = _rect.sizeDelta.y;

            // セリフ吹き出しテキスト取得
            speech.Text = speechImage.GetComponentInChildren<Text>();
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

            var _damaged = false; // ダメージ中フラグ TODO: class

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

            // プレイヤーとの距離取得
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    distance = (float) System.Math.Round(
                        Vector3.Distance(transform.position, _player.transform.position), 1, System.MidpointRounding.AwayFromZero
                    );
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
                .Where(_ => doUpdate.searching && !doUpdate.rotate && !_damaged)
                .Subscribe(_ => {
                    // ランダムで移動位置指定
                    var _rand1 = Mathf.FloorToInt(Random.Range(3f, 9f));
                    var _rand2 = Mathf.FloorToInt(Random.Range(-3f, 3f));
                    var _rand3 = Mathf.FloorToInt(Random.Range(-3f, 3f));
                    doUpdate.rotate = true;
                    // [_rand1]秒後に
                    Observable.Timer(System.TimeSpan.FromSeconds(_rand1)) // FIXME: 60fpsの時は？
                        //.Where(__ => !_damaged) // TODO これが必要？
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
                .Where(_ => doUpdate.searching && !_idle && !_damaged)
                .Subscribe(_ => {
                    var _speed = _rb.velocity.magnitude; // 速度ベクトル取得
                    if (_speed < 1.1f) {
                        _rb.AddFor​​ce(Utils.TransformForward(transform.forward, _speed) * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                    }
                });

            // 障害物に当たった(壁)
            this.OnCollisionEnterAsObservable()
                .Where(t => doUpdate.searching && (t.gameObject.name.Contains("EnemyWall") || t.gameObject.name.Contains("Wall")))
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
                .Where(t => doUpdate.searching && t.gameObject.name.Contains("Block") )
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
                .Where(t => doUpdate.searching && !_damaged && t.gameObject.name.Equals("Player"))
                .Subscribe(_ => {
                    simpleAnime.Play("Run"); // 走るアニメ
                    doUpdate.ApplyChasing();
                    doFixedUpdate.ApplyRun();
                    say("I found\nthe cat!");
                });

            // プレイヤーロスト
            this.OnTriggerExitAsObservable()
                .Where(t => !doUpdate.searching && !_damaged && t.gameObject.name.Equals("Player"))
                .Subscribe(_ => {
                    simpleAnime.Play("Walk"); // 歩くアニメ
                    doUpdate.ApplySearching();
                    doFixedUpdate.ApplyWalk();
                    say("I lost\nthe cat...");
                });

            #endregion

            #region 追跡

            // 追跡中
            this.UpdateAsObservable()
                .Where(_ => doUpdate.chasing && !_damaged)
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
                .Where(_ => doUpdate.chasing && !_damaged)
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
                .Where(_ => doUpdate.attacking && !_damaged)
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
                .Where(t => !doUpdate.attacking && !_damaged && t.gameObject.name.Equals("Player"))
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
                .Where(t => !doUpdate.attacking && !_damaged && t.gameObject.name.Equals("Player"))
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

            #region ダメージを受ける

            // 爆弾の破片に当たった
            this.OnCollisionEnterAsObservable()
                .Where(t => !_damaged && t.gameObject.name.Contains("debris"))
                .Subscribe(_ => {
                    simpleAnime.Play("ClimbUp"); // ダメージ代用
                    _damaged = true;
                    say("Damn it!", 65); // FIXME: 表示されない？
                    // 10秒後に復活
                    Observable.Timer(System.TimeSpan.FromSeconds(10))
                        .Subscribe(__ => {
                            simpleAnime.Play("Walk"); // 歩くアニメ
                            doUpdate.ApplySearching();
                            doFixedUpdate.ApplyWalk();
                            _damaged = false;
                        });
                });

            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// セリフ用吹き出しにセリフを表示する。 // FIXME: 吹き出しの形
        /// </summary>
        void say(string text, int size = 60, double time = 0.5d) { // TODO: 表示されないものがある？
            if (!isRendered) { return; }
            // プレイヤーとの距離で大きさ調整
            var _distance = distance > 1 ? (int) (distance / 2) : 1;
            if (_distance == 0) { _distance = 1; }
            speechImage.GetComponent<RectTransform>().sizeDelta = new Vector2(speech.X / _distance, speech.Y / _distance);
            speech.Text.fontSize = size / (int) (_distance * 1.25f); // 調整値
            //Debug.Log("_distance: " + _distance + " x: " + speechImage.GetComponent<RectTransform>().sizeDelta.x + " y: " + speechImage.GetComponent<RectTransform>().sizeDelta.y);
            speech.Text.text = text;
            speechImage.SetActive(true);
            Observable.Timer(System.TimeSpan.FromSeconds(time))
                .First()
                .Subscribe(_ => {
                    speechImage.SetActive(false);
                });
        }

        void say(string text, double time) {
            say(text, 60, time);
        }

        void say(string text) {
            say(text, 60, 0.5d);
        }

        /// <summary>
        /// セリフ用吹き出しを非表示にする。
        /// </summary>
        void beSilent() {
            speechImage.SetActive(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // カメラ映り TODO: UniRx

        const string MAIN_CAMERA_TAG_NAME = "MainCamera";  // メインカメラに付いているタグ名

        // メインカメラに表示されているか
        bool isRendered = false;

        void OnBecameVisible() { // メインカメラに映った時
            if (Camera.current.tag == MAIN_CAMERA_TAG_NAME) {
                isRendered = true;
            }
        }

        void OnBecameInvisible() { // メインカメラに映らなくなった時
            if (Camera.current != null && Camera.current.tag == MAIN_CAMERA_TAG_NAME) {
                isRendered = false;
            }
        }

        #region DoUpdate

        /// <summary>
        /// Update() メソッド用のクラス。
        /// </summary>
        protected class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            bool _grounded; // 接地フラグ
            bool _searching; // 索敵中フラグ
            bool _rotate; //回転中フラグ
            bool _chasing; // 追跡中フラグ
            bool _attacking; // 攻撃中フラグ

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool grounded { get => _grounded; set => _grounded = value; }

            public bool searching { get => _searching; }

            public bool rotate { get => _rotate; set => _rotate = value; }

            public bool chasing { get => _chasing; }

            public bool attacking { get => _attacking; }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static DoUpdate GetInstance() {
                var _instance = new DoUpdate();
                _instance.ResetState();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

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

        }

        #endregion

        #region DoFixedUpdate

        /// <summary>
        /// FixedUpdate() メソッド用のクラス。
        /// </summary>
        protected class DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            //bool _idol;
            bool _run;
            bool _walk;
            bool _jump;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            //public bool idol { get => _idol; set => _idol = value; }

            public bool run { get => _run; }

            public bool walk { get => _walk; }

            public bool jump { get => _jump; set => _jump = value; }


            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static DoFixedUpdate GetInstance() {
                var _instance = new DoFixedUpdate();
                _instance.ResetMotion();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

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

        #region Speech

        /// <summary>
        /// セリフ用のクラス。
        /// </summary>
        protected class Speech {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            float _x; // 吹き出し幅
            float _y; // 吹き出し高さ
            Text _text; // テキスト

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float X { get => _x; set => _x = value; }

            public float Y { get => _y; set => _y = value; }

            public Text Text { get => _text; set => _text = value; }


            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static Speech GetInstance() {
                return new Speech();
            }
        }

        #endregion

    }

}