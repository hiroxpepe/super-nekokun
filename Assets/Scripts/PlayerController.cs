using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;

namespace StudioMeowToon {
    /// <summary>
    /// プレイヤーの処理
    /// </summary>
    public class PlayerController : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 設定・参照

        [SerializeField]
        private GameSystem gameSystem; // ゲームシステム

        [SerializeField]
        private SoundSystem soundSystem; // サウンドシステム

        [SerializeField]
        private CameraSystem cameraSystem; // カメラシステム

        [SerializeField]
        private float jumpPower = 5.0f;

        [SerializeField]
        private float rotationalSpeed = 5.0f;

        [SerializeField]
        private SimpleAnimation simpleAnime;

        [SerializeField]
        private GameObject bullet; // 弾の元

        [SerializeField]
        private float bulletSpeed = 5000.0f; // 弾の速度

        private BombAngle bombAngle; // 弾道角度

        private GameObject pushed; // 押されるオブジェクト

        private GameObject holded; // 持たれるオブジェクト

        private GameObject stairUped; // 階段を上られるオブジェクト

        private GameObject stairDowned; // 階段を下りられるオブジェクト

        private DoUpdate doUpdate; // Update() メソッド用フラグ構造体

        private DoFixedUpdate doFixedUpdate; // FixedUpdate() メソッド用フラグ構造体

        private int life; // ヒットポイント

        private float waterLevel; // 水面の高さ TODO:プレイヤーのフィールドでOK?

        private GameObject playerNeck; // プレイヤーの水面判定用

        private GameObject intoWaterFilter; // 水中でのカメラエフェクト用

        private GameObject bodyIntoWater; // 水中での体

        //////////////////////////////////////////////////////
        // その他 TODO: ⇒ speed・position オブジェクト化する

        private float speed; // 速度ベクトル

        private float previousSpeed; // 1フレ前の速度ベクトル

        private Vector3[] previousPosition = new Vector3[30]; // 30フレ分前のポジション保存用

        ////////// TODO: 実験的
        private System.Diagnostics.Stopwatch sw;

        private Quaternion originalRotation;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プロパティ(キャメルケース: 名詞、形容詞)

        public bool Faceing { get => doUpdate.faceing; } // オブジェクトに正対中かどうか

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // パブリックメソッド

        public SoundSystem GetSoundSystem() { return soundSystem; } // サウンドシステムを返す

        public void DecrementLife() { life--; } // HPデクリメント

        /// <summary>
        /// 敵から攻撃を受ける
        /// </summary>
        public void DamagedByEnemy(Vector3 forward) {
            moveByShocked(forward);
            doUpdate.damaged = true;
            simpleAnime.Play("ClimbUp"); // FIXME: ダメージアニメ
            Observable.TimerFrame(9) // FIXME: 60fpsの時は？
                .Subscribe(_ => {
                    doUpdate.damaged = false;
                });
        }

        /// <summary>
        /// 攻撃の衝撃で少し後ろ上に動く。
        /// </summary>
        private void moveByShocked(Vector3 forward) {
            var _ADJUST = 2.5f; // 調整値
            transform.position += (forward + new Vector3(0f, 0.5f, 0)) / _ADJUST; // 0.5fは上方向調整値
        }

        ///////////////////////////////////////////////////////////////////////////

        //　IK左手位置用のTransform
        private Transform leftHandTransform;

        //　IK右手位置用のTransform
        private Transform rightHandTransform;

        void OnAnimatorIK() {
            if (doUpdate.holding) { // 持つフラグON
                var _animator = GetComponent<Animator>();
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.5f);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.5f);
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.5f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.5f);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTransform.position);
                _animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTransform.rotation);
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTransform.position);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTransform.rotation);
                _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0.5f);
                _animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0.5f);
                _animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightHandTransform.position);
                _animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftHandTransform.position);
            }
        }

        ///////////////////////////////////////////////////////////////////////////
        // 更新 メソッド

        // Awake is called when the script instance is being loaded.
        void Awake() {

            doUpdate = DoUpdate.GetInstance(); // 状態フラグ構造体
            doFixedUpdate = DoFixedUpdate.GetInstance(); // 物理挙動フラグ構造体
            bombAngle = BombAngle.GetInstance(); // 弾道角度用構造体
            life = 10; // HP初期化

            if (SceneManager.GetActiveScene().name != "Start") { // TODO: 再検討
                // 水面の高さを取得
                waterLevel = GameObject.Find("Water").transform.position.y; // TODO: x, z 軸で水面(水中の範囲取得)

                // 水中カメラエフェクト取得
                intoWaterFilter = GameObject.Find("Canvas");

                // 水中での体取得
                bodyIntoWater = GameObject.Find("Player/Body");

                // 水面判定用
                playerNeck = GameObject.Find("Bell");
            }
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            speed = 0; // 速度初期化

            // TODO: 実験的
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
        }

        // Update is called once per frame.
        new void Update() {
            base.Update();

            // ポーズ中動作停止
            if (Time.timeScale == 0f) {
                return;
            }

            if (SceneManager.GetActiveScene().name != "Start") { // TODO: 再検討
                // ステージをクリアした・GAMEオーバーした場合抜ける
                if (gameSystem.levelClear || gameSystem.gameOver) {
                    return;
                }
            }

            // 視点操作中(Xボタン押しっぱなし)
            if (xButton.isPressed) {
                return;
            }

            if (SceneManager.GetActiveScene().name != "Start") { // TODO: 再検討
                gameSystem.playerLife = life; // HP設定
                gameSystem.bombAngle = bombAngle.Value; // 弾角度
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // ダメージ受け期間
            if (doUpdate.damaged) {
                return;
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // オブジェクトに正対中キャラ操作無効
            if (doUpdate.faceing) {
                if (holded != null) {
                    faceToObject(holded);
                }
                return;
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // ブロックを押してる時キャラ操作無効
            if (doUpdate.pushing && transform.parent != null) {
                simpleAnime.Play("Push"); // 押すアニメ
                pushed.GetComponent<BlockController>().pushed = true; // ブロックを押すフラグON
                return;
                // 押してるブロックの子オブジェクト化が解除されたら
            } else if (doUpdate.pushing && transform.parent == null) {
                doUpdate.pushing = false; // 押すフラグOFF
                pushed = null; // 押すブロックの参照解除
                simpleAnime.Play("Default"); // デフォルトアニメ
            }

            doUpdate.IncrementTime(Time.deltaTime); // 時間インクリメント

            ///////////////////////////////////////////////////////////////////////////////////////
            // 水中かどうかチェック
            if (checkIntoWater()) {
                if (dpadUp.isPressed) {
                    simpleAnime.Play("Swim"); // 泳ぐアニメ
                } else {
                    simpleAnime.Play("Default"); // デフォルトアニメ TODO: 浮かぶアニメ
                }
                if (transform.localPosition.y + 0.9f < waterLevel) { // 0.9f は調整値
                    intoWaterFilter.GetComponent<Image>().enabled = true;
                } else if (transform.localPosition.y + 0.85f > waterLevel) { // 0.8f は調整値(※浮上時は速く切り替える)
                    intoWaterFilter.GetComponent<Image>().enabled = false;
                }
                doUpdate.grounded = false;
            } else {
                intoWaterFilter.GetComponent<Image>().enabled = false; // TODO: GetComponent をオブジェクト参照に
            }

            // 持つ(Rボタン)を離した
            if (r1Button.wasReleasedThisFrame || (!r1Button.isPressed && doUpdate.holding)) {
                if (holded != null) {
                    holded.transform.parent = null; // 子オブジェクト解除
                    doUpdate.holding = false; // 持つフラグOFF
                    holded = null; // 持つオブジェクト参照解除
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // 接地フラグONの場合
            if (doUpdate.grounded && !doUpdate.climbing) {

                // 持つ(Rボタン)押した
                if (r1Button.wasPressedThisFrame) {
                    if (checkToFace() && checkToHoldItem()) { // アイテムが持てるかチェック
                        startFaceing(); // オブジェクトに正対する開始
                        faceToObject(holded); // オブジェクトに正対する
                        goto STEP0; // 弾発射を飛ばす
                    }
                }

                if (doUpdate.bombing) {
                    bomb(); // 弾を撃つ
                    doUpdate.bombed = true;
                    goto STEP1;
                } else if (aButton.isPressed && doUpdate.throwed) {
                    simpleAnime.CrossFade("Push", 0.2f); // 投げるからしゃがむ(代用)アニメ
                    goto STEP1;
                } else if (yButton.isPressed && doUpdate.throwed) {
                    simpleAnime.CrossFade("Run", 0.3f); // 投げるから走るアニメ
                    goto STEP1;
                } else if (doUpdate.throwed) {
                    simpleAnime.CrossFade("Walk", 0.5f); // 投げるから歩くアニメ
                    goto STEP1;
                } else if (r1Button.wasPressedThisFrame) { // Rボタンを押した時
                    if (!doUpdate.holding || !doUpdate.faceing) { // Item を持っていなかったら、またはオブジェクトに正対中でなければ
                        simpleAnime.CrossFade("Throw", 0.3f); // 投げるアニメ
                        doUpdate.throwing = true;
                    }
                    goto STEP1;
                }

STEP0:
                if (aButton.isPressed) { // Aボタン押しっぱなし
                    // MEMO: 追加:しゃがむ時、持ってるモノを離す ///////////////
                    if (holded != null) {
                        holded.transform.parent = null; // 子オブジェクト解除
                        doUpdate.holding = false; // 持つフラグOFF
                        holded = null; // 持つオブジェクト参照解除
                    }
                    ////////////////////////////////////////////////////////////
                    if (dpadLeft.isPressed) { // 左
                        if (!doUpdate.throwing) {
                            simpleAnime.Play("Push");
                        } else if (doUpdate.throwed) {
                            simpleAnime.CrossFade("Push", 0.2f); // 投げるからしゃがむ(代用)アニメ
                        }
                    } else if (dpadLeft.isPressed) { // 右
                        if (!doUpdate.throwing) {
                            simpleAnime.Play("Push");
                        } else if (doUpdate.throwed) {
                            simpleAnime.CrossFade("Push", 0.2f); // 投げるからしゃがむ(代用)アニメ
                        }
                    }
                }

                if (dpadUp.isPressed) { // 上を押した時
                    if (l1Button.isPressed) {
                        bombAngle.Value -= Time.deltaTime * 2.5f; // 弾道角度調整※*反応速度
                        return;
                    }
                    if (yButton.isPressed) { // Yボタン押しっぱなしなら
                        if (!doUpdate.throwing) {
                            simpleAnime.Play("Run"); // 走るアニメ
                            soundSystem.PlayRunClip();
                        }
                        doFixedUpdate.run = true;
                    } else {
                        if (!doUpdate.throwing) {
                            if (aButton.isPressed) { // Aボタン押しっぱなし
                                simpleAnime.Play("Push"); // しゃがむ(代用)アニメ
                            } else {
                                simpleAnime.Play("Walk"); // 歩くアニメ
                                soundSystem.PlayWalkClip();
                            }
                        }
                        doFixedUpdate.walk = true;
                    }
                    // 階段を上がるかチェック
                    checkStairUp();
                    if (doUpdate.stairUping != true) {
                        // 階段を下がるかチェック
                        checkStairDown();
                    }
                } else if (dpadDown.isPressed) { // 下を押した時
                    if (l1Button.isPressed) {
                        bombAngle.Value += Time.deltaTime * 2.5f; // 弾道角度調整※*反応速度
                        return;
                    }
                    if (!doUpdate.throwing) {
                        if (aButton.isPressed) { // Aボタン押しっぱなし
                            simpleAnime.Play("Push"); // しゃがむアニメ代用
                        } else {
                            simpleAnime.Play("Backward"); // 後ろアニメ
                        }
                    }
                    soundSystem.PlayWalkClip();
                    doFixedUpdate.backward = true;
                } else if (dpadUp.isPressed == false && dpadDown.isPressed == false) { // 上下を離した時
                    if (!doUpdate.lookBackJumping) { // 捕まり反転ジャンプ中でなければ
                        if (!doUpdate.throwing) {
                            if (aButton.isPressed) { // Aボタン押しっぱなし
                                simpleAnime.Play("Push"); // しゃがむアニメ代用
                            } else {
                                simpleAnime.Play("Default"); // デフォルトアニメ
                            }
                        }
                        soundSystem.StopClip();
                        doFixedUpdate.idol = true;
                    }
                }

                // ジャンプ(Bボタン)
                if (bButton.wasPressedThisFrame) {
                    doUpdate.InitThrowBomb(); // 爆撃フラグOFF
                    simpleAnime.Play("Jump"); // ジャンプアニメ
                    soundSystem.PlayJumpClip();
                    doUpdate.grounded = false;
                    doUpdate.secondsAfterJumped = 0f; // ジャンプ後経過秒リセット
                    doFixedUpdate.jump = true;
                }

                // 上を押しながら、押す(Aボタン)
                if (aButton.wasPressedThisFrame && dpadUp.isPressed) {
                    if (checkToPushBlock()) {
                        //startFaceing(); // オブジェクトに正対する開始
                        faceToObject(pushed); // オブジェクトに正対する
                    }
                }
            }
            ///////////////////////////////////////////////////////////////////////////////////////
            // ジャンプ中 ※水中もここに来る
            else if (!doUpdate.grounded && !doUpdate.climbing) {
                doUpdate.secondsAfterJumped += Time.deltaTime; // ジャンプ後経過秒インクリメント
                // 空中で移動
                var _axis = dpadUp.isPressed ? 1 : dpadDown.isPressed ? -1 : 0;
                if (_axis == 1) { // 前移動
                    doFixedUpdate.jumpForward = true;
                    if (checkIntoWater()) { soundSystem.PlayWaterForwardClip(); }
                } else if (_axis == -1) { // 後ろ移動
                    doFixedUpdate.jumpBackward = true;
                    if (checkIntoWater()) { soundSystem.StopClip(); }
                } else {
                    if (checkIntoWater()) { soundSystem.StopClip(); }
                }
                if (Math.Round(previousSpeed, 4) == Math.Round(speed, 4) && !doUpdate.lookBackJumping && (doUpdate.secondsAfterJumped > 0.1f && doUpdate.secondsAfterJumped < 0.4f)) { // 完全に空中停止した場合※捕まり反転ジャンプ時以外
#if DEBUG
                    Debug.Log("344 完全に空中停止した場合 speed:" + speed);
#endif 
                    transform.Translate(0, -5.0f/*-0.05f*/ * Time.deltaTime, 0); // 下げる
                    doUpdate.grounded = true; // 接地
                    doFixedUpdate.unintended = true; // 意図しない状況フラグON
                }
                if (!checkIntoWater() && !bButton.isPressed && doUpdate.secondsAfterJumped > 10.0f) { // TODO: checkIntoWater 重くない？
#if DEBUG
                    Debug.Log("352 JUMP後に空中停止した場合 speed:" + speed); // TODO: 水面で反応
#endif 
                    transform.Translate(0, -5.0f/*-0.05f*/ * Time.deltaTime, 0); // 下げる
                    doUpdate.grounded = true; // 接地
                    doFixedUpdate.unintended = true; // 意図しない状況フラグON
                }
                // モバイル動作時に面に正対する TODO: ジャンプ後しばらくたってから
                if (useVirtualController && !checkIntoWater()) {
                    faceToFace(5f);
                }
            }

        STEP1:
            // Yボタン押しっぱなし
            if (yButton.isPressed && !doUpdate.holding) {
                if (yButton.wasPressedThisFrame && checkIntoWater()) { soundSystem.PlayWaterSinkClip(); } // 水中で沈む音
                if (!doUpdate.climbing) { // 上り降り発動なら
                    if (!doUpdate.lookBackJumping) { // 捕まり反転ジャンプが発動してなかったら
                        if (dpadDown.isPressed) { // ハシゴを降りる
                            if (previousPosition[0].y - (0.1f * Time.deltaTime) > transform.position.y) {
                                checkToClimbDownByLadder();
                            }
                        }
                    }
                    checkToClimb(); // よじ登り可能かチェック
                }

                // 上り降り中
                if (doUpdate.climbing) {
                    simpleAnime.Play("ClimbUp"); // よじ登るアニメ
                    if (l1Button.isPressed) { // 捕まり反転ジャンプ準備
                        simpleAnime.Play("Default");
                    }
                    climb(); // 上り下り
                    if (r1Button.isPressed) { // さらにRボタン押しっぱなしなら
                        moveSide(); // 横に移動
                    }
                }
            }
            // Yボタン離した
            else if (yButton.wasReleasedThisFrame) { // TODO: Yボタンを離したらジャンプモーションがキャンセルされる
                if (doUpdate.climbing) {
                    simpleAnime.Play("Default"); // デフォルトアニメ
                    soundSystem.StopClip();
                }
                // RayBox位置の初期化
                var _rayBox = transform.Find("RayBox").gameObject;
                _rayBox.transform.localPosition = new Vector3(0, 0.4f, 0.1f); // RayBoxローカルポジション
                doUpdate.climbing = false; // 登るフラグOFF
                doFixedUpdate.cancelClimb = true;
            }

            //// 持つ(Rボタン)を離した
            //if (r1Button.wasReleasedThisFrame) {
            //    if (holded != null) {
            //        holded.transform.parent = null; // 子オブジェクト解除
            //        doUpdate.holding = false; // 持つフラグOFF
            //        holded = null; // 持つオブジェクト参照解除
            //    }
            //}

            // ロック(Lボタン)を押して続けている // TODO: 敵ロック
            if (l1Button.isPressed) {
                //lockOnTarget(); // TODO: 捕まり反転ジャンプの時に向いてしまう…
            }

            // Lボタンを離した(※捕まり反転ジャンプ準備のカメラリセット)
            if (l1Button.wasReleasedThisFrame) {
                cameraSystem.ResetLookAround(); // カメラ初期化
            }

            // TODO: テスト
            //if (Input.GetButtonUp("L1")) {
            //    lookBack(); // TODO: 未完成
            //}

            // 回転 // TODO: 入力の遊びを持たせる？ // TODO: 左右2回押しで180度回転？
            if (!doUpdate.climbing) {
                var _ADJUST = 20; // 調整値
                var _axis = dpadRight.isPressed ? 1 : dpadLeft.isPressed ? -1 : 0;
                if (aButton.isPressed && doUpdate.grounded) { // Aボタン押しながら左右でサイドステップ※接地時のみ
                    if (_axis == -1) {
                        if (speed < 2.0f) {
                            doFixedUpdate.sideStepLeft = true; // 左ステップ
                        }
                    } else if (_axis == 1) {
                        if (speed < 2.0f) {
                            doFixedUpdate.sideStepRight = true; // 右ステップ
                        }
                    }
                    faceToFace(5); // 面に正対する
                } else {
                    if (Math.Round(speed, 2) == 0) { // 静止時回転は速く
                        transform.Rotate(0, _axis * (rotationalSpeed * Time.deltaTime) * _ADJUST * 1.5f, 0);
                    } else if (speed < 4.5f) { // 加速度制御
                        if (doUpdate.grounded) {
                            transform.Rotate(0, _axis * (rotationalSpeed * Time.deltaTime) * _ADJUST, 0); // 回転は transform.rotate の方が良い
                        } else {
                            transform.Rotate(0, _axis * (rotationalSpeed * Time.deltaTime) * _ADJUST / 1.2f, 0); // ジャンプ中は回転控えめに
                        }
                    }
                }
            }

            // TODO: 砲台から弾が飛んでくる：赤-半誘導弾、青-通常弾

            // 階段を上る下りる ※水中は無関係
            if (doUpdate.stairUping) {
                doStairUp();
            } else if (doUpdate.stairDowning) {
                doStairDown();
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // モバイル用モード
            if (useVirtualController) {
                if (yButton.wasReleasedThisFrame) {
                    doFixedUpdate.virtualControllerMode = true;
                    Observable.TimerFrame(30)
                        .Subscribe(_ => {
                            doFixedUpdate.virtualControllerMode = false;
                        });
                }
            }
        }

        // FixedUpdate is called just before each physics update.
        void FixedUpdate() {
            // フラグ系の切り替えはここには書かない
            // Time.deltaTime は一定である

            var _rb = transform.GetComponent<Rigidbody>(); // Rigidbody は FixedUpdate の中で "だけ" 使用する
            previousSpeed = speed; // 速度ベクトル保存
            speed = _rb.velocity.magnitude; // 速度ベクトル取得

            // TODO: 10フレ分のスピードを保存しとく？
            //if (speed != 0 && speed < 0.05/*0.001*/) {
            //    _rb.velocity = Vector3.zero;
            //    _rb.velocity = new Vector3(0, 0.2f, 0); // 押しても進まないとき
            //}

            if (speed > 5.0f) { // 加速度リミッター
                _rb.velocity = new Vector3(
                    _rb.velocity.x - (_rb.velocity.x / 10),
                    _rb.velocity.y - (_rb.velocity.y / 10),
                    _rb.velocity.z - (_rb.velocity.z / 10)
                );
            }

            if (doFixedUpdate.cancelClimb) {
                _rb.useGravity = true; // 重力再有効化
                _rb.AddRelativeFor​​ce(Vector3.down * 3f, ForceMode.Impulse); // 落とす
            }

            // ジャンプ
            if (doFixedUpdate.jump) { // TODO: ジャンプボタンを押し続けると飛距離が伸びるように
                var _ADJUST = 0f;
                if (doFixedUpdate.virtualControllerMode || speed > 2.9f) { // TODO: 再検討
                    _ADJUST = jumpPower * 1.75f; // 最高速ジャンプ
                } else if (speed > 1.9f) {
                    _ADJUST = jumpPower * 1.25f; // 走りジャンプ
                } else if (speed > 0) {
                    _ADJUST = jumpPower;        // 歩きジャンプ
                } else if (speed == 0) {
                    _ADJUST = jumpPower * 1.5f;   // 静止ジャンプ
                }
                _rb.useGravity = true;
                _rb.velocity += Vector3.up * _ADJUST;
            }
            if (doFixedUpdate.jumpForward) { // ジャンプ中前移動 : 追加:水中移動
                if (!checkIntoWater()) {
                    if (speed < 3.25f) {
                        _rb.AddRelativeFor​​ce(Vector3.forward * 6.5f, ForceMode.Acceleration);
                    }
                } else { // 水中移動
                    if (speed < 3.25f) {
                        _rb.AddRelativeFor​​ce(Vector3.forward * 13.0f, ForceMode.Acceleration);
                    }
                }
            }
            if (doFixedUpdate.jumpBackward) { // ジャンプ中後ろ移動 : 追加:水中移動
                if (!checkIntoWater()) {
                    if (speed < 1.5f) {
                        _rb.AddRelativeFor​​ce(Vector3.back * 4.5f, ForceMode.Acceleration);
                    }
                } else { // 水中移動
                    if (speed < 1.5f) {
                        _rb.AddRelativeFor​​ce(Vector3.back * 9.0f, ForceMode.Acceleration);
                    }
                }
            }

            //  歩く、走る TODO: ⇒ 二段階加速：ifネスト
            var _fps = Application.targetFrameRate;
            var _ADJUST1 = 0f;
            if (_fps == 60) _ADJUST1 = 8f;
            if (_fps == 30) _ADJUST1 = 16f;
            if (doFixedUpdate.run) { // 走る
                _rb.useGravity = true; // 重力再有効化 
                if (speed < 3.25f) { // ⇒ フレームレートに依存する 60fps,8f, 30fps:16f, 20fps:24f, 15fps:32f
                    _rb.AddFor​​ce(Utils.TransformForward(transform.forward, speed) * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                }
            } else if (doFixedUpdate.walk) { // 歩く
                _rb.useGravity = true; // 重力再有効化 
                if (speed < 1.1f) {
                    _rb.AddFor​​ce(Utils.TransformForward(transform.forward, speed) * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                }
            } else if (doFixedUpdate.backward) { // 下がる
                _rb.useGravity = true; // 重力再有効化 
                if (speed < 0.75f) {
                    _rb.AddFor​​ce(-Utils.TransformForward(transform.forward, speed) * _ADJUST1, ForceMode.Acceleration); // 後ろに移動させる
                }
            } else if (doFixedUpdate.idol) {
                _rb.useGravity = true; // 重力有効化
            }

            // サイドステップ
            var _ADJUST2 = 0f;
            if (_fps == 60) _ADJUST2 = 18f;
            if (_fps == 30) _ADJUST2 = 36f;
            if (doFixedUpdate.sideStepLeft) {
                _rb.AddRelativeFor​​ce(Vector3.left * _ADJUST2, ForceMode.Acceleration); // 左に移動させる
            } else if (doFixedUpdate.sideStepRight) {
                _rb.AddRelativeFor​​ce(Vector3.right * _ADJUST2, ForceMode.Acceleration); // 右に移動させる
            }

            // 水中での挙動
            if (doFixedUpdate.intoWater) { // 水の中に入ったら
                _rb.drag = 5f; // 抵抗を増やす(※大きな挙動変化をもたらす)
                _rb.angularDrag = 5f; // 回転抵抗を増やす(※大きな挙動変化をもたらす)
                _rb.useGravity = false;
                _rb.AddForce(new Vector3(0, 3.8f, 0), ForceMode.Acceleration); // 3.8f は調整値
            } else if (!doFixedUpdate.intoWater) { // 元に戻す
                _rb.drag = 0f;
                _rb.angularDrag = 0f;
                _rb.useGravity = true;
            }

            // ブロック上る下りる
            if (doFixedUpdate.climbUp || doUpdate.climbing) { // Update と FixedUpdate の呼び出され差 60fps, 30fps を考慮したら
                _rb.useGravity = false; // 重力無効化 ※重力に負けるから
                _rb.velocity = Vector3.zero;
            } else if (doFixedUpdate.grounded) {
                _rb.useGravity = true; // 重力再有効化 
                _rb.velocity = Vector3.zero; // TODO: 必要？
            }

            // 捕まり反転ジャンプ
            if (doFixedUpdate.reverseJump) { 
                var _ADJUST = 0f;
                _ADJUST = jumpPower;
                _rb.useGravity = true;
                _rb.velocity += Vector3.up * _ADJUST / 2.0f;
                _rb.velocity += transform.forward * _ADJUST / 3.5f;
            }

            // 階段を上る下りる
            if (doFixedUpdate.stairUp || doFixedUpdate.stairDown) { 
                _rb.useGravity = false; // 重力無効化 ※重力に負けるから
                _rb.velocity = Vector3.zero;
            }

            // 意図していない状況
            if (doFixedUpdate.unintended) {
                _rb.useGravity = true; // 重力有効化
                _rb.velocity = Vector3.zero; // 速度0にする
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // アイテム

            if (doFixedUpdate.getItem) {
                _rb.velocity = Vector3.zero; // アイテム取得時停止
            }

            doFixedUpdate.ResetMotion(); // 物理挙動フラグ初期化
        }

        // LateUpdate is called after all Update functions have been called.
        void LateUpdate() {
            if (doUpdate.climbing) { // 捕まり反転ジャンプの準備
                if (dpadLeft.isPressed) {
                    AxisToggle.Left = AxisToggle.Left == true ? false : true;
                } else if (dpadRight.isPressed) {
                    AxisToggle.Right = AxisToggle.Right == true ? false : true;
                }
                if (l1Button.isPressed) { // Lボタン押しっぱなし TODO: ボタンの変更
                    readyForBackJump();
                }
            }

            cashPreviousPosition(); // 10フレ前分の位置情報保存
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // イベントハンドラ

        void OnCollisionEnter(Collision collision) {
            // ブロックに接触したら
            var _name = collision.gameObject.name;
            if (_name.Contains("Block")) {
                if (isUpOrDown()) { // 上下変動がある場合
                    // 上に乗った状況
                    if (!isHitSide(collision.gameObject)) {
                        simpleAnime.Play("Default"); // デフォルトアニメ
                        soundSystem.PlayGroundedClip();
                        doUpdate.grounded = true; // 接地フラグON
                        doUpdate.lookBackJumping = false; // 捕まり反転ジャンプフラグOFF
                        doUpdate.stairUping = false; // 階段上りフラグOFF
                        doUpdate.stairDowning = false; // 階段下りフラグOFF
                        doFixedUpdate.idol = true;
                        doFixedUpdate.grounded = true;
                        cameraSystem.ResetLookAround(); // カメラ初期化
                        doUpdate.secondsAfterJumped = 0f; // ジャンプ後経過秒リセット TODO:※試験的
                        flatToFace(); // 面に合わせる TODO:※試験的
                    } else if (isHitSide(collision.gameObject)) {
                        // 下に当たった場合
                        if (isHitBlockBottom(collision.gameObject)) {
                            soundSystem.PlayKnockedupClip();
                            collision.gameObject.GetComponent<CommonController>().shockedBy = transform; // 下から衝撃を与える
                        }
                        // 横に当たった場合
                        else {
                        }
                    }
                }
                // ブロックを持つ実装 TODO: 修正
                if (_name.Contains("Item") && !doUpdate.holding) { // TODO: Holdable 追加？
                    holded = collision.gameObject; // 持てるアイテムの参照を保持する
                }
            }
            // 地上・壁に接地したら
            else if ((_name.Contains("Ground") || _name.Contains("Wall")) && !checkIntoWater()) { // 水中ではない場合
                simpleAnime.Play("Default"); // デフォルトアニメ
                soundSystem.PlayGroundedClip();
                doUpdate.grounded = true; // 接地フラグON
                doUpdate.lookBackJumping = false; // 振り返りジャンプフラグOFF
                doFixedUpdate.idol = true;
                doFixedUpdate.grounded = true;
                cameraSystem.ResetLookAround(); // カメラ初期化
                doUpdate.secondsAfterJumped = 0f; // ジャンプ後経過秒リセット TODO:※試験的
                flatToFace(); // 面に合わせる TODO:※試験的
            }
            // 持てるアイテムと接触したら
            else if (_name.Contains("Item") && !doUpdate.holding) { // TODO: Holdable 追加？
                holded = collision.gameObject; // 持てるアイテムの参照を保持する
            }
            // 被弾したら
            else if (_name.Contains("Bullet")) {
                soundSystem.PlayHitClip();
            }
        }

        void OnCollisionStay(Collision collision) {
            // ブロックに接触し続けている
            var _name = collision.gameObject.name;
            if (_name.Contains("Block")) {
                // 横に当たった場合
                if (isHitSide(collision.gameObject)) {
                }
            }
        }

        private void OnCollisionExit(Collision collision) {
            // ブロックから離れたら
            var _name = collision.gameObject.name;
            if (_name.Contains("Block")) {
                // ブロックを持つ実装 TODO: 修正
                if (_name.Contains("Item")) { // TODO: Holdable 追加？
                    // 持つ(Rボタン)を離した
                    if (!doUpdate.holding) {
                        holded = null; // 持てるブロックの参照を解除する
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other) {
            // アイテムと接触したら消す
            if (other.gameObject.tag == "Item") {
                soundSystem.PlayItemClip(); // 効果音を鳴らす
                gameSystem.DecrementItem(); // アイテム数デクリメント
                doFixedUpdate.getItem = true;
                Destroy(other.gameObject);
            }
            // 水面に接触したら
            if (other.gameObject.name == "Water") {
                if (transform.localPosition.y + 0.75f > waterLevel) { // 0.75f は調整値 ⇒ TODO:再検討
                    soundSystem.PlayWaterInClip();
                }
            }
        }

        void OnGUI() {
            if (SceneManager.GetActiveScene().name != "Start") { // TODO: 再検討
                // デバッグ表示
                var _y = string.Format("{0:F3}", Math.Round(transform.position.y, 3, MidpointRounding.AwayFromZero));
                var _s = string.Format("{0:F3}", Math.Round(speed, 3, MidpointRounding.AwayFromZero));
                var _aj = string.Format("{0:F3}", Math.Round(doUpdate.secondsAfterJumped, 3, MidpointRounding.AwayFromZero));
                var _rb = transform.GetComponent<Rigidbody>(); // Rigidbody は FixedUpdate の中で "だけ" 使用する
                gameSystem.TRACE("Hight: " + _y + "m \r\nSpeed: " + _s + "m/s" +
                    "\r\nGrounded: " + doUpdate.grounded +
                    "\r\nClimbing: " + doUpdate.climbing +
                    "\r\nHolding: " + doUpdate.holding +
                    "\r\nStairUp: " + doUpdate.stairUping +
                    "\r\nStairDown: " + doUpdate.stairDowning +
                    "\r\nGravity: " + _rb.useGravity +
                    "\r\nJumped: " + _aj + "sec",
                    speed, 3.0f
                );
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プライベート メソッド(キャメルケース: 動詞)

        // TODO: 一時凍結
        private void lookBack() { // 後ろをふりかえる
            float _SPEED = 10.01f; // 回転スピード
            var _behind = transform.Find("Behind").gameObject;
            //transform.LookAt(_behind.transform);
            // TODO: gameobject を new ?
            var _target = _behind.transform;
            var _relativePos = _target.position - transform.position;
            var _rotation = Quaternion.LookRotation(_relativePos);
            transform.rotation =
              Quaternion.Slerp(transform.rotation, _rotation, Time.deltaTime * _SPEED);
            //cameraSystem.LookPlayer();
        }

        /// <summary>
        /// nフレ前分の位置情報保存する。
        /// </summary>
        private void cashPreviousPosition() {
            for (var i = previousPosition.Length - 1 ; i > -1; i--) {
                if (i > 0) {
                    previousPosition[i] = previousPosition[i - 1]; // 0 ～ 19 を保存
                } else if (i == 0) {
                    previousPosition[i] = new Vector3( // 現在のポジション保存
                        (float) Math.Round(transform.position.x, 3), // 小数点3桁まで保存しておく
                        (float) Math.Round(transform.position.y, 3),
                        (float) Math.Round(transform.position.z, 3)
                    );
                }
            }
        }

        private void bomb() { // 弾を撃つ TODO: fps で加える値を変化？
            // 弾の複製
            var _bullet = Instantiate(bullet) as GameObject;

            // 弾の位置
            var _pos = transform.position + (transform.forward * 1.2f) + (transform.right * 0.25f);
            _bullet.transform.position = new Vector3(_pos.x, _pos.y + 0.8f, _pos.z);

            // 弾の回転
            _bullet.transform.rotation = transform.rotation;

            // 弾へ加える力
            var _force = (transform.forward + (transform.up / 4 * bombAngle.Value)) * bulletSpeed;
            // TODO: トルク: カーブ、シュート、スライダー？

            // 弾を発射
            _bullet.GetComponent<Rigidbody>().AddForce(_force, ForceMode.Force);
            soundSystem.PlayShootClip();
        }

        private float getRendererTop(GameObject target) { // TODO: Player が測っても良いのでは？
            float _height = target.GetComponent<Renderer>().bounds.size.y; // オブジェクトの高さ取得 
            float _y = target.transform.position.y; // オブジェクトのy座標取得(※0基点)
            float _top = _height + _y; // オブジェクトのTOP取得
            return _top;
        }

        private bool isUpOrDown() { // 上下変動があったかどうか
            var _fps = Application.targetFrameRate;
            var _ADJUST1 = 0;
            if (_fps == 60) _ADJUST1 = 9;
            if (_fps == 30) _ADJUST1 = 20; // MEMO: 正直ここは詳細な検討が必要 TODO: ⇒ x, z 軸でも変動があったか調べる？
            var _y = (float) Math.Round(transform.position.y, 1, MidpointRounding.AwayFromZero);
            var _previousY = (float) Math.Round(previousPosition[_ADJUST1].y, 1, MidpointRounding.AwayFromZero);
            if (_y == _previousY) { // nフレ前の値と比較
                return false;
            } else if (_y != _previousY) {
                return true;
            } else {
                return true;
            }
        }

        private void moveSide() { // 上り下り中に横に移動する
            faceToFace();
            var _MOVE = 0.8f;
            var _fX = (float) Math.Round(transform.forward.x);
            var _fZ = (float) Math.Round(transform.forward.z);
            var _axis = dpadRight.isPressed ? 1 : dpadLeft.isPressed ? -1 : 0;
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                transform.localPosition = new Vector3(
                    transform.localPosition.x + (_axis * _MOVE * Time.deltaTime),
                    transform.localPosition.y, transform.localPosition.z
                );
            } else if (_fX == 0 && _fZ == -1) { // Z軸負方向
                transform.localPosition = new Vector3(
                    transform.localPosition.x + (-(_axis) * _MOVE * Time.deltaTime),
                    transform.localPosition.y, transform.localPosition.z
                );
            } else if (_fX == 1 && _fZ == 0) { // X軸正方向
                transform.localPosition = new Vector3(
                    transform.localPosition.x, transform.localPosition.y,
                    transform.localPosition.z + (-(_axis) * _MOVE * Time.deltaTime)
                );
            } else if (_fX == -1 && _fZ == 0) { // X軸負方向
                transform.localPosition = new Vector3(
                    transform.localPosition.x, transform.localPosition.y,
                    transform.localPosition.z + (_axis * _MOVE * Time.deltaTime)
                );
            }
        }

        private void flatToFace() { // 面に高さを合わせる // TODO:※試験中
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                (float) Math.Round(transform.position.y, 2, MidpointRounding.AwayFromZero),
                transform.localPosition.z
            );
        }

        private void faceToFace(float speed = 20.0f) { // 面に正対する
            float _SPEED = speed; // 回転スピード
            var _fX = (float) Math.Round(transform.forward.x);
            var _fZ = (float) Math.Round(transform.forward.z);
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, 0), _SPEED * Time.deltaTime); // 徐々に回転
            } else if (_fX == 0 && _fZ == -1) { // Z軸負方向
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 180, 0), _SPEED * Time.deltaTime); // 徐々に回転
            } else if (_fX == 1 && _fZ == 0) { // X軸正方向
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 90, 0), _SPEED * Time.deltaTime); // 徐々に回転
            } else if (_fX == -1 && _fZ == 0) { // X軸負方向
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 270, 0), _SPEED * Time.deltaTime); // 徐々に回転
            }
        }

        /// <summary>
        /// 面への正対が許容範囲かチェックする。
        /// </summary>
        private bool checkToFace() {
            var _fX = (float) Math.Round(transform.forward.x);
            var _fZ = (float) Math.Round(transform.forward.z);
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                return true;
            } else if (_fX == 0 && _fZ == -1) { // Z軸負方向
                return true;
            } else if (_fX == 1 && _fZ == 0) { // X軸正方向
                return true;
            } else if (_fX == -1 && _fZ == 0) { // X軸負方向
                return true;
            }
            return false; // 判定不可
        }

        /// <summary>
        /// オブジェクトに正対する。
        /// </summary>
        private void faceToObject(GameObject target, float speed = 2.0f) {
            var _fx = (float) Math.Round(transform.forward.x);
            var _fz = (float) Math.Round(transform.forward.z);
            if (_fx == 0 && _fz == 1) { // Z軸正方向
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, 0f), speed * Time.deltaTime);
                if (transform.rotation.eulerAngles.y >= 0f) {
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    float _tx = (float) Math.Round(transform.position.x * 2, 0, MidpointRounding.AwayFromZero) / 2;
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(_tx, transform.position.y, transform.position.z), speed * Time.deltaTime);
                    if (Math.Round(transform.position.x, 2) == Math.Round(_tx, 2)) {
                        doUpdate.faceing = false;
                    }
                }
            } else if (_fx == 0 && _fz == -1) { // Z軸負方向
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 180f, 0f), speed * Time.deltaTime);
                if (transform.rotation.eulerAngles.y >= 179f) {
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    float _tx = (float) Math.Round(transform.position.x * 2, 0, MidpointRounding.AwayFromZero) / 2;
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(_tx, transform.position.y, transform.position.z), speed * Time.deltaTime);
                    if (Math.Round(transform.position.x, 2) == Math.Round(_tx, 2)) {
                        doUpdate.faceing = false;
                    }
                }
            } else if (_fx == 1 && _fz == 0) { // X軸正方向
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 90f, 0f), speed * Time.deltaTime);
                if (transform.rotation.eulerAngles.y >= 89f) {
                    transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    float _tz = (float) Math.Round(transform.position.z * 2, 0, MidpointRounding.AwayFromZero) / 2;
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, transform.position.y, _tz), speed * Time.deltaTime);
                    if (Math.Round(transform.position.z, 2) == Math.Round(_tz, 2)) {
                        doUpdate.faceing = false;
                    }
                }
            } else if (_fx == -1 && _fz == 0) { // X軸負方向
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 270f, 0f), speed * Time.deltaTime);
                if (transform.rotation.eulerAngles.y >= 269f) {
                    transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                    float _tz = (float) Math.Round(transform.position.z * 2, 0, MidpointRounding.AwayFromZero) / 2;
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, transform.position.y, _tz), speed * Time.deltaTime);
                    if (Math.Round(transform.position.z, 2) == Math.Round(_tz, 2)) {
                        doUpdate.faceing = false;
                    }
                }
            }
            Debug.Log("969 faceToObject doUpdate.faceing: " + doUpdate.faceing + "_fx: " + _fx + " _fz: " + _fz);
        }

        /// <summary>
        /// オブジェクトに正対するフラグの開始。
        /// </summary>
        private void startFaceing() {
            doUpdate.faceing = true;
        }

        ///// <summary>
        ///// オブジェクトに正対するフラグの解除。
        ///// </summary>
        //private void doneFaceing() {
        //    doUpdate.faceing = false;
        //}

        private void lockOnTarget() { // ロックオン対象の方向に回転
            var target = gameSystem.SerchNearTargetByTag(gameObject, "Block");
            if (target != null) {
                float _SPEED = 3.0f; // 回転スピード
                Vector3 _look = target.transform.position - transform.position; // ターゲット方向へのベクトル
                Quaternion _rotation = Quaternion.LookRotation(new Vector3(_look.x, 0, _look.z)); // 回転情報に変換※Y軸はそのまま
                transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, _SPEED * Time.deltaTime); // 徐々に回転
            }
        }

        private void readyForBackJump() { // 捕まり反転ジャンプの準備
            simpleAnime.Play("Default"); // デフォルトアニメ
            var _h = transform.Find("Head").gameObject;
            var _e = transform.Find("Ear").gameObject;
            var _cs = transform.Find("CameraSystem").gameObject;
            float _SPEED = 100; // 回転スピード※ゆっくり回転は効かない
            var _fX = (float) Math.Round(transform.forward.x);
            var _fZ = (float) Math.Round(transform.forward.z);
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                _h.transform.rotation = Quaternion.Slerp(_h.transform.rotation, Quaternion.Euler(0, 180, 0), _SPEED * Time.deltaTime);
                _e.transform.rotation = Quaternion.Slerp(_e.transform.rotation, Quaternion.Euler(0, 180, 0), _SPEED * Time.deltaTime);
                _cs.transform.rotation = Quaternion.Slerp(_cs.transform.rotation, Quaternion.Euler(0, 180, 0), _SPEED * Time.deltaTime);
                _cs.transform.position = new Vector3(_cs.transform.position.x, transform.position.y + 0.9f, transform.position.z + 0.36f);
            } else if (_fX == 0 && _fZ == -1) { // Z軸負方向
                _h.transform.rotation = Quaternion.Slerp(_h.transform.rotation, Quaternion.Euler(0, 0, 0), _SPEED * Time.deltaTime);
                _e.transform.rotation = Quaternion.Slerp(_e.transform.rotation, Quaternion.Euler(0, 0, 0), _SPEED * Time.deltaTime);
                _cs.transform.rotation = Quaternion.Slerp(_cs.transform.rotation, Quaternion.Euler(0, 0, 0), _SPEED * Time.deltaTime);
                _cs.transform.position = new Vector3(_cs.transform.position.x, transform.position.y + 0.9f, transform.position.z - 0.36f);
            } else if (_fX == 1 && _fZ == 0) { // X軸正方向
                _h.transform.rotation = Quaternion.Slerp(_h.transform.rotation, Quaternion.Euler(0, 270, 0), _SPEED * Time.deltaTime);
                _e.transform.rotation = Quaternion.Slerp(_e.transform.rotation, Quaternion.Euler(0, 270, 0), _SPEED * Time.deltaTime);
                _cs.transform.rotation = Quaternion.Slerp(_cs.transform.rotation, Quaternion.Euler(0, 270, 0), _SPEED * Time.deltaTime);
                _cs.transform.position = new Vector3(transform.position.x + 0.36f, transform.position.y + 0.9f, _cs.transform.position.z);
            } else if (_fX == -1 && _fZ == 0) { // X軸負方向
                _h.transform.rotation = Quaternion.Slerp(_h.transform.rotation, Quaternion.Euler(0, 90, 0), _SPEED * Time.deltaTime);
                _e.transform.rotation = Quaternion.Slerp(_e.transform.rotation, Quaternion.Euler(0, 90, 0), _SPEED * Time.deltaTime);
                _cs.transform.rotation = Quaternion.Slerp(_cs.transform.rotation, Quaternion.Euler(0, 90, 0), _SPEED * Time.deltaTime);
                _cs.transform.position = new Vector3(transform.position.x - 0.36f, transform.position.y + 0.9f, _cs.transform.position.z);
            }
        }

        private void doBackJump() { // 捕まり反転ジャンプ
            transform.Rotate(0, 180f, 0); // 180度反転
            doUpdate.climbing = false; // 登るフラグOFF
            doUpdate.lookBackJumping = true; // 反転ジャンプフラグON
            simpleAnime.Play("Jump"); // ジャンプアニメ
            soundSystem.PlayJumpClip();
            doFixedUpdate.reverseJump = true;
        }

        private void checkToClimb() {
            var _rayBox = transform.Find("RayBox").gameObject; // RayBoxから前方サーチする
            Ray _ray = new Ray(_rayBox.transform.position, transform.forward);
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.2f)) { // 前方にレイを投げて反応があった場合
                if (_hit.transform.name.Contains("Block") ||
                    _hit.transform.name.Contains("Ladder") ||
                    _hit.transform.name.Contains("Wall") ||
                    _hit.transform.name.Contains("Ground")) { // ブロック、ハシゴ、壁、地面で
                    if (_hit.transform.GetComponent<CommonController>().climbable) { // 登ることが可能なら
                        var _hitTop = getRaycastHitTop(_hit); // 前方オブジェクトのtop位置を取得
#if DEBUG
                        Debug.DrawRay(_ray.origin, _ray.direction * 0.2f, Color.green, 3, false); //レイを可視化
#endif
                        float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                        if (_distance < 0.15) { // 距離が近くなら
                            var _myY = transform.position.y; // 自分のy位置(0基点)を取得
                            if (_myY < _hitTop) { // 自分が前方オブジェクトより低かったら
                                doUpdate.climbing = true; // 登るフラグON
                            }
                        }
                    }
                }
            }
        }

        private void checkToClimbDownByLadder() { // ハシゴを降りるとき限定
            int _fps = Application.targetFrameRate;
            float _ADJUST = 0;
            if (_fps == 60) _ADJUST = 75.0f; // 調整値
            if (_fps == 30) _ADJUST = 37.5f; // 調整値
            var _rayBox = transform.Find("RayBox").gameObject; // RayBoxから前方サーチする
            Ray _ray = new Ray(_rayBox.transform.position, transform.forward);
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.3f)) { // 前方にレイを投げて反応があった場合
                if (_hit.transform.name.Contains("Ladder")) { // ハシゴなら
                    var _hitTop = getRaycastHitTop(_hit); // 前方オブジェクトのtop位置を取得
#if DEBUG
                    Debug.DrawRay(_ray.origin, _ray.direction * 0.3f, Color.white, 3, false); //レイを可視化
#endif 
                    float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                    if (_distance < 0.3f) { // 距離が近くなら
                        var _myY = transform.position.y; // 自分のy位置(0基点)を取得
                        if (_myY < _hitTop) { // 自分が前方オブジェクトより低かったら
                            transform.position += transform.forward * 0.2f * Time.deltaTime * _ADJUST; // 少し前に進む
                            doUpdate.climbing = true; // 登るフラグON
                        }
                    }
                }
            }
        }

        private void climb() { // ハシゴ上り降り
            var _rayBox = transform.Find("RayBox").gameObject; // RayBoxから前方サーチする
            Ray _ray = new Ray(
                new Vector3(_rayBox.transform.position.x, _rayBox.transform.position.y, _rayBox.transform.position.z),
                transform.forward
            );
            float _hitTop = 0f;
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.3f)) { // 前方にレイを投げて反応があった場合
                _hitTop = getRaycastHitTop(_hit); // 前方オブジェクトのtop位置を取得
#if DEBUG
                Debug.DrawRay(_ray.origin, _ray.direction * 0.3f, Color.blue, 1, false); // レイを可視化
#endif 
            }
            // 前方オブジェクトまでの距離を取得
            float _distance = _hit.distance;
            if (_distance != 0 && _distance < 0.3f) { // 距離が近くなら ※ここはこれで上手くいく…
                doFixedUpdate.climbUp = true; // 重力OFF
                var _myY = transform.position.y; // 自分のy位置(0基点)を取得
                if (Math.Round(_myY, 3) + 5f * Time.deltaTime < Math.Round(_hitTop, 3)) { // 自分が前方オブジェクトより低かったら TODO: 5f は調整値、デルタタイムを掛けて fps 調整
                    faceToFace(); // 面に正対する
                    if (dpadUp.isPressed) { // 上を押した時
                        transform.Translate(0, 1f * Time.deltaTime, 0); // 上る
                        doUpdate.grounded = false; // 接地フラグOFF
                        if (_rayBox.transform.position.y > transform.position.y) { // キャラ高さの範囲で
                            _rayBox.transform.localPosition = new Vector3(0, _rayBox.transform.localPosition.y - (1f * 1.5f * Time.deltaTime), 0.1f); // RayBoxは逆に動かす
                        }
                        soundSystem.PlayClimbClip();
                    } else if (dpadDown.isPressed) { // 下を押した時
                        transform.Translate(0, -1f * Time.deltaTime, 0); // 降りる
                        doUpdate.grounded = false; // 接地フラグOFF
                        if (_rayBox.transform.position.y < transform.position.y + 0.4f) { // キャラ高さの範囲で 0.4 は_rayBoxの元の位置
                            _rayBox.transform.localPosition = new Vector3(0, _rayBox.transform.localPosition.y + (1f * 1.5f * Time.deltaTime), 0.1f); // RayBoxは逆に動かす
                        }
                        soundSystem.PlayClimbClip();
                    } else { // 一時停止
                        // Bボタンを押したら反転ジャンプする
                        if (bButton.wasPressedThisFrame) {
                            doBackJump();
                        }
                    }
                } else { // 自分が対象オブジェクトのtop位置より高くなったら
                    Ray _ray2 = new Ray( // 少し上を確認する
                        new Vector3(_rayBox.transform.position.x, _rayBox.transform.position.y + 0.2f, _rayBox.transform.position.z),
                        transform.forward
                    );
                    if (Physics.Raycast(_ray2, out RaycastHit _hit2, 0.3f)) { // 前方にレイを投げて反応があった場合
#if DEBUG
                        Debug.DrawRay(_ray2.origin, _ray2.direction * 0.3f, Color.cyan, 5, false); //レイを可視化
#endif 
                        transform.position += transform.up * 1f * Time.deltaTime; // まだ続くので少し上に上げる
                    } else {
                        ///////////////////////////////////////////////////////////////////////////////////////////
                        _rayBox.transform.localPosition = new Vector3(0, 0.4f, 0.1f); // RayBoxローカルポジション
                        doUpdate.climbing = false; // 登るフラグOFF
                        doUpdate.grounded = true; // 接地フラグON
                        transform.position += transform.up * 8f * Time.deltaTime; // 少し上に上げる
                        transform.position += transform.forward * 8f * Time.deltaTime; // 少し前に進む
                        doFixedUpdate.idol = true;
                        simpleAnime.Play("Run"); // 走るアニメ※この方が自然に見える
                        soundSystem.PlayRunClip();
                    }
                }
            } else if (_distance == 0) { // 下にハシゴがなくなった時
                _rayBox.transform.localPosition = new Vector3(0, 0.4f, 0.1f); // RayBoxローカルポジション
                doUpdate.climbing = false; // 登るフラグOFF
                transform.position += transform.forward * 0.2f * Time.deltaTime; // 少し前に進む
                doFixedUpdate.cancelClimb = true;
            }
        }

        private void checkStairUp() { // 階段を上るフラグチェック ※【注意】Rayが捜査するオブジェクトが増えるだけでタイミングが破綻する
            var _rayBox = transform.Find("StepRayBox").gameObject; // StepRayBoxから前方サーチする
            Ray _ray = new Ray(
                new Vector3(_rayBox.transform.position.x, _rayBox.transform.position.y + 0.1f, _rayBox.transform.position.z),
                transform.forward
            );
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.35f)) { // 前方にレイを投げて反応があった場合
#if DEBUG
                Debug.DrawRay(_ray.origin, _ray.direction * 0.35f, Color.yellow, 3, false); //レイを可視化
#endif
                float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                if (_distance < 0.35f) { // 距離が近くなら
                    if (_hit.transform.name.Contains("Stair")) { // 上がれるのは階段のみ
                        doUpdate.stairUping = true; // 階段を上るフラグON
                        stairUped = _hit.transform.gameObject; // 階段を上がられるオブジェクトの参照保存
                    }
                }
            }
        }

        private void checkStairDown() { // 階段を下りるフラグチェック ※【注意】Rayが捜査するオブジェクトが増えるだけでタイミングが破綻する
            var _rayBox = transform.Find("StepRayBox").gameObject; // StepRayBoxから前方サーチする
            Ray _ray = new Ray(
                new Vector3(_rayBox.transform.position.x, _rayBox.transform.position.y + 0.1f, _rayBox.transform.position.z),
                transform.forward
            );
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.2f)) { // 前方にレイを投げて反応があった場合
#if DEBUG
                Debug.DrawRay(_ray.origin, _ray.direction * 0.2f, Color.cyan, 3, false); //レイを可視化
#endif 
                float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                if (_distance < 0.2f) { // 距離が近くなら
                    if (_hit.transform.name.Contains("Down_Point")) { // 下がれるのは階段に付けたBOXコライダーから判定する
                        doUpdate.stairDowning = true; // 階段を下りるフラグON
                        stairDowned = _hit.transform.gameObject; // 階段を下がられるオブジェクトの参照保存
                        transform.position = new Vector3( // BOXコライダーの位置に移動する
                            stairDowned.transform.position.x,
                            stairDowned.transform.position.y - 0.12f, // 調整値
                            stairDowned.transform.position.z
                        );
                    }
                }
            }
        }

        private void doStairDown() { // 階段を下りる
            doFixedUpdate.stairDown = true;
            // 上下を離した時
            if (dpadUp.isPressed == false && dpadDown.isPressed == false) {
                doUpdate.stairDowning = false;
                stairDowned = null; // 階段の参照解除
                doFixedUpdate.idol = true; // 念のため重力有効化
            } else {
                if (yButton.isPressed) { // Yボタン押しっぱなし
                    transform.Translate(0, -0.95f * Time.deltaTime, 0); // 下にさげる
                    transform.position += transform.forward * (0.8f * Time.deltaTime); // 前に進む
                } else {
                    transform.Translate(0, -0.57f * Time.deltaTime, 0); // 下にさげる
                    transform.position += transform.forward * (0.4f * Time.deltaTime); // 前に進む
                }
            }
            faceToFace(5f); // 面に正対する
        }

        private void doStairUp() { // 階段を上る
            doFixedUpdate.stairUp = true;
            if (getRendererTop(stairUped) > transform.position.y) {
                // 上下を離した時
                if (dpadUp.isPressed == false && dpadDown.isPressed == false) {
                    doUpdate.stairUping = false;
                    stairUped = null; // 階段の参照解除
                    doFixedUpdate.idol = true; // 念のため重力有効化
                } else {
                    if (yButton.isPressed) { // Yボタン押しっぱなし
                        transform.Translate(0, 0.8f * Time.deltaTime, 0); // 上にあげる
                        transform.position += transform.forward * (0.8f * Time.deltaTime); // 前に進む
                    } else {
                        transform.Translate(0, 0.4f * Time.deltaTime, 0); // 上にあげる
                        transform.position += transform.forward * (0.4f * Time.deltaTime); // 前に進む
                    }
                }
            } else {
                if (yButton.isPressed) { // Yボタン押しっぱなし
                    transform.position = new Vector3(transform.position.x, getRendererTop(stairUped) + 0.1f, transform.position.z);
                    transform.position += transform.forward * (0.2f * Time.deltaTime); // 前に進む
                } else {
                    transform.position = new Vector3(transform.position.x, getRendererTop(stairUped) + 0.1f, transform.position.z);
                    transform.position += transform.forward * (0.2f * Time.deltaTime); // 前に進む
                }
                doUpdate.stairUping = false;
                stairUped = null; // 階段の参照解除
                doFixedUpdate.idol = true; // 念のため重力有効化
            }
            faceToFace(5f); // 面に正対する
        }

        private bool checkToPushBlock() {
            var _rayBox = transform.Find("StepRayBox").gameObject; // StepRayBoxから前方サーチする
            Ray _ray = new Ray(
                new Vector3(_rayBox.transform.position.x, _rayBox.transform.position.y + 0.1f, _rayBox.transform.position.z),
                transform.forward
            );
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.35f)) { // 前方にレイを投げて反応があった場合
                var _hitTop = getRaycastHitTop(_hit); // 前方オブジェクトのtop位置を取得
#if DEBUG
                Debug.DrawRay(_ray.origin, _ray.direction * 0.35f, Color.magenta, 3, false); //レイを可視化
#endif 
                // TODO: 押し可能なブロックの判定
                if (_hit.transform.name.Contains("Block")) { // 押せるのはブロックのみ
                    if (_hit.transform.GetComponent<BlockController>().pushable) { // 押せるブロックの場合
                        float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                        if (_distance < 0.3) { // 距離が近くなら
                            if (!doUpdate.pushing) { // 押してない
                                doUpdate.pushing = true; // 押すフラグON
                                pushed = _hit.transform.gameObject; // 押されるオブジェクトの参照保存
                                transform.parent = pushed.transform; // プレイヤーを押されるオブジェクトの子にする
                                return true;
                            }
                            //Observable.EveryUpdate().Select(_ => !doUpdate.faceing && !doUpdate.pushing).Subscribe(_ => {
                            //    doUpdate.pushing = true; // 押すフラグON
                            //    pushed = _hit.transform.gameObject; // 押されるオブジェクトの参照保存
                            //    transform.parent = pushed.transform; // プレイヤーを押されるオブジェクトの子にする
                            //});
                            //return true; TODO: まだこれでは動かない
                        }
                    }
                }
            }
            return false;
        }

        private bool checkToHoldItem() {
            if (holded != null) { // 持つオブジェクトの参照があれば
                var _rayBox = transform.Find("StepRayBox").gameObject; // StepRayBoxから前方サーチする
                Ray _ray = new Ray(
                    new Vector3(_rayBox.transform.position.x, _rayBox.transform.position.y + 0.3f, _rayBox.transform.position.z),
                    transform.forward
                );
                if (Physics.Raycast(_ray, out RaycastHit _hit, 0.35f) || checkDownAsHoldableBlock()) { // 前方にレイを投げて反応があった場合
#if DEBUG
                    Debug.DrawRay(_ray.origin, _ray.direction * 0.35f, Color.magenta, 4, false); //レイを可視化
#endif
                    if (checkDownAsHoldableBlock() || _hit.transform.name.Contains("Item")) { // 持てるのはアイテムのみ TODO: 子のオブジェクト判定は？
                        float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                        if (_distance < 0.3f || checkDownAsHoldableBlock()) { // 距離が近くなら
                            //if (holded.tag.Equals("Item")) {
                            //    var _itemController = holded.GetComponent<ItemController>(); // TODO: holdable で共通化？
                            //    leftHandTransform = _itemController.GetLeftHandTransform(); // アイテムから左手のIK位置を取得
                            //    rightHandTransform = _itemController.GetRightHandTransform(); // アイテムから右手のIK位置を取得
                            //} else if (holded.tag.Equals("Block")) {
                            //    var _blockController = holded.GetComponent<BlockController>();
                            //    leftHandTransform = _blockController.GetLeftHandTransform(); // ブロックから左手のIK位置を取得
                            //    rightHandTransform = _blockController.GetRightHandTransform(); // ブロックから右手のIK位置を取得
                            //}
                            //holded.transform.parent = transform; // 自分の子オブジェクトにする
                            //doUpdate.holding = true; // 持つフラグON
                            Observable.EveryUpdate().Select(_ => !doUpdate.faceing && holded != null).Subscribe(_ => { // なぜ Where だとダメ？
                                if (holded.tag.Equals("Item")) {
                                    var _itemController = holded.GetComponent<ItemController>(); // TODO: holdable で共通化？
                                    leftHandTransform = _itemController.GetLeftHandTransform(); // アイテムから左手のIK位置を取得
                                    rightHandTransform = _itemController.GetRightHandTransform(); // アイテムから右手のIK位置を取得
                                } else if (holded.tag.Equals("Block")) {
                                    var _blockController = holded.GetComponent<BlockController>();
                                    leftHandTransform = _blockController.GetLeftHandTransform(); // ブロックから左手のIK位置を取得
                                    rightHandTransform = _blockController.GetRightHandTransform(); // ブロックから右手のIK位置を取得
                                }
                                holded.transform.parent = transform; // 自分の子オブジェクトにする
                                doUpdate.holding = true; // 持つフラグON
                            });
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool checkDownAsHoldableBlock() { // 足元の下が持てるブロックかどうか
            if (holded != null) {
                return true;
            } // TODO: 修正
            return false;
        }

        private bool checkIntoWater() { // 水中にいるかチェック TODO: 空間が水中でなはい時は？
            if (playerNeck.transform.position.y + 0.25f < waterLevel) { // 0.25f は体を水面に沈める為の調整値
                //transform.GetComponent<CapsuleCollider>().enabled = false; // コライダー切り替え
                //if (_level.transform.position.y < waterLevel) { bodyIntoWater.GetComponent<CapsuleCollider>().enabled = true; }
                doFixedUpdate.intoWater = true;
            } else {
                //transform.GetComponent<CapsuleCollider>().enabled = true; // コライダー切り替え
                //if (_level.transform.position.y < waterLevel) { bodyIntoWater.GetComponent<CapsuleCollider>().enabled = false; }
                doFixedUpdate.intoWater = false;
            }
            return doFixedUpdate.intoWater;
        }

        // レイを投げた対象のtop位置を取得
        private float getRaycastHitTop(RaycastHit hit) {
            float _hitHeight = hit.collider.GetComponent<Renderer>().bounds.size.y; // 対象オブジェクトの高さ取得 
            float _hitY = hit.transform.position.y; // 対象オブジェクトの(※中心)y座標取得
            return _hitHeight + _hitY; // 対象オブジェクトのtop位置取得
        }

        // 衝突したオブジェクトの側面に当たったか判定する
        private bool isHitSide(GameObject target) {
            float _targetHeight = target.GetComponent<Renderer>().bounds.size.y; // 対象オブジェクトの高さ取得 
            float _targetY = target.transform.position.y; // 対象オブジェクトの(※中心)y座標取得
            float _targetTop = _targetHeight + _targetY; // 衝突したオブジェクトのTOP取得
            var _y = transform.position.y; // 自分のy位置(0基点)を取得
            if (_y < (_targetTop - 0.1f)) { // 0.1fは誤差 // TODO: ← 【0.1f って結構大きすぎる誤差設定じゃない？】
                return true;
            } else {
                return false;
            }
        }

        // 衝突したブロックの下に当たったか判定する
        private bool isHitBlockBottom(GameObject target) {
            var _targetBottom = target.transform.position.y; // 当たったブロックの底面の高さ
            float _height = GetComponent<CapsuleCollider>().bounds.size.y; // 自分のコライダーの高さ
            float _y = transform.position.y; // 自分のy座標(※0基点)
            float _top = _height + _y; // 自分のTOP位置
            if (_top - 0.1f < _targetBottom) { // ブロックの底面が自分のTOP位置より低かったら※0.1fは誤差
                return true; // ブロックの底面に当たった
            }
            return false; // そうではない
        }

        #region DoUpdate

        /// <summary>
        /// Update() メソッド用の構造体。
        /// </summary>
        protected struct DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // フィールド(アンダースコアorキャメルケース)

            private bool _grounded; // 接地フラグ
            private bool _climbing; // 上り降りフラグ
            private bool _pushing; // 押すフラグ
            private bool _holding; // 持つフラグ
            private bool _faceing; // 正対するフラグ
            private bool _stairUping; // 階段上りフラグ
            private bool _stairDowning; // 階段下りフラグ
            private bool _lookBackJumping; // 捕まり反転ジャンプフラグ

            private float _secondsAfterJumped; // ジャンプして何秒経ったか

            private bool _throwing;
            private bool _throwed;
            private float _throwingTime;
            private float _throwedTime;
            private bool _bombing;
            private bool _bombed;

            private bool _damaged; // ダメージを受けるフラグ

            ///////////////////////////////////////////////////////////////////////////////////////////
            // プロパティ(キャメルケース)

            public bool grounded { get => _grounded; set => _grounded = value; }
            public bool climbing { get => _climbing; set => _climbing = value; }
            public bool pushing { get => _pushing; set => _pushing = value; }
            public bool faceing { get => _faceing; set => _faceing = value; }
            public bool holding { get => _holding; set => _holding = value; }
            public bool stairUping { get => _stairUping; set => _stairUping = value; }
            public bool stairDowning { get => _stairDowning; set => _stairDowning = value; }
            public bool lookBackJumping { get => _lookBackJumping; set => _lookBackJumping = value; }
            public bool throwing { get => _throwing; set => _throwing = value; }
            public bool throwed { get => _throwed; set => _throwed = value; }
            public bool bombing { get => _bombing; set => _bombing = value; }
            public bool bombed { get => _bombed; set => _bombed = value; }
            public float secondsAfterJumped { get => _secondsAfterJumped; set => _secondsAfterJumped = value; }
            public bool damaged { get => _damaged; set => _damaged = value; }

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

            public void ResetState() {
                _grounded = false;
                _climbing = false;
                _pushing = false;
                _holding = false;
                _faceing = false;
                _stairUping = false;
                _stairDowning = false;
                _lookBackJumping = false;
                _throwing = false;
                _throwed = false;
                _throwingTime = 0f;
                _throwedTime = 0f;
                _bombing = false;
                _bombed = false;
                _damaged = false;
            }

            public void IncrementTime(float time) {
                throwBomb(time);
            }

            public void InitThrowBomb() {
                _throwing = false;
                _throwed = false;
                _throwingTime = 0f;
                _throwedTime = 0f;
                _bombing = false;
                _bombed = false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // プライベートメソッド(キャメルケース)

            private void throwBomb(float time) {
                if (_throwing && !_bombed && _throwingTime > 0.5f) {
                    _bombing = true;
                } else if (_throwing && _bombed && _throwingTime > 0.5f) {
                    _bombing = false;
                }
                if (_throwing && _throwingTime > 0.8f) {
                    _throwing = false;
                    _throwed = true;
                    _throwingTime = 0f;
                    _bombed = false;
                } else if (_throwing) {
                    _throwingTime += time;
                }
                if (_throwed && _throwedTime > 0.5f) {
                    _throwed = false;
                    _throwedTime = 0f;
                } else if (_throwed) {
                    _throwedTime += time;
                }
            }
        }

        #endregion

        #region DoFixedUpdate

        /// <summary>
        /// FixedUpdate() メソッド用の構造体。
        /// </summary>
        protected struct DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // フィールド

            private bool _idol;
            private bool _run;
            private bool _walk;
            private bool _jump;
            private bool _reverseJump;
            private bool _backward;
            private bool _sideStepLeft;
            private bool _sideStepRight;
            private bool _climbUp;
            private bool _cancelClimb;
            private bool _jumpForward;
            private bool _jumpBackward;
            private bool _grounded;
            private bool _getItem;
            private bool _stairUp;
            private bool _stairDown;
            private bool _unintended; // 意図していない状況
            private bool _intoWater;
            private bool _virtualControllerMode;

            public bool idol { get => _idol; set => _idol = value; }
            public bool run { get => _run; set => _run = value; }
            public bool walk { get => _walk; set => _walk = value; }
            public bool jump { get => _jump; set => _jump = value; }
            public bool reverseJump { get => _reverseJump; set => _reverseJump = value; }
            public bool backward { get => _backward; set => _backward = value; }
            public bool sideStepLeft { get => _sideStepLeft; set => _sideStepLeft = value; }
            public bool sideStepRight { get => _sideStepRight; set => _sideStepRight = value; }
            public bool climbUp { get => _climbUp; set => _climbUp = value; }
            public bool cancelClimb { get => _cancelClimb; set => _cancelClimb = value; }
            public bool jumpForward { get => _jumpForward; set => _jumpForward = value; }
            public bool jumpBackward { get => _jumpBackward; set => _jumpBackward = value; }
            public bool grounded { get => _grounded; set => _grounded = value; }
            public bool getItem { get => _getItem; set => _getItem = value; }
            public bool stairUp { get => _stairUp; set => _stairUp = value; }
            public bool stairDown { get => _stairDown; set => _stairDown = value; }
            public bool unintended { get => _unintended; set => _unintended = value; }
            public bool intoWater { get => _intoWater; set => _intoWater = value; }
            public bool virtualControllerMode { get => _virtualControllerMode; set => _virtualControllerMode = value; }

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

            /// <summary>
            /// 全フィールドの初期化
            /// </summary>
            public void ResetMotion() {
                _idol = false;
                _run = false;
                _walk = false;
                _jump = false;
                _reverseJump = false;
                _backward = false;
                _sideStepLeft = false;
                _sideStepRight = false;
                _climbUp = false;
                _cancelClimb = false;
                _jumpForward = false;
                _jumpBackward = false;
                _grounded = false;
                _getItem = false;
                _stairUp = false;
                _stairDown = false;
                _unintended = false;
                // _intoWater は初期化しない
                // _virtualControllerMode は初期化しない
            }
        }

        #endregion

        #region BombAngle

        /// <summary>
        /// 弾道角度用の構造体。
        /// </summary>
        protected struct BombAngle {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // フィールド

            private float _value;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // パブリックメソッド

            public float Value {
                get { return _value; }
                set {
                    if (_value + value > -1.0f && _value + value < 2.5f) { // 角度制限 -0.5, 1.25 をスライダーUIに設定する
                        _value = value;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // コンストラクタ

            public static BombAngle GetInstance() {
                var _instance = new BombAngle();
                _instance.init();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // プライベートメソッド

            private void init() {
                _value = 1f;
            }

        }

        #endregion

        #region AxisToggle

        private class AxisToggle {
            public static bool Up = false;
            public static bool Down = false;
            public static bool Left = false;
            public static bool Right = false;
        }

        #endregion
    }

}
