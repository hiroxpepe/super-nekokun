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

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// プレイヤーの処理
    /// @author h.adachi
    /// </summary>
    public class Player : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        float jumpPower = 5.0f;

        [SerializeField]
        float rotationalSpeed = 5.0f;

        [SerializeField]
        SimpleAnimation simpleAnime;

        [SerializeField]
        GameObject bullet; // 弾の元

        [SerializeField]
        float bulletSpeed = 5000.0f; // 弾の速度

        [SerializeField]
        GameObject speechObject; // セリフオブジェクト

        [SerializeField]
        Vector3 speechOffset = new Vector3(0f, 0f, 0f); // セリフ位置オフセット

        [SerializeField]
        Sprite speechRightSprite;

        [SerializeField]
        Sprite speechGizaSprite;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        GameSystem gameSystem; // ゲームシステム

        SoundSystem soundSystem; // サウンドシステム

        CameraSystem cameraSystem; // カメラシステム

        GameObject rayBox; // ブロック、ハシゴ、壁 つかまり判定オブジェクト

        GameObject stepRayBox; // ブロック押し、階段上り下り判定オブジェクト

        GameObject behind; // 振り返り用オブジェクト

        BombAngle bombAngle; // 弾道角度

        GameObject pushed; // 押されるオブジェクト

        GameObject holded; // 持たれるオブジェクト

        GameObject stairUped; // 階段を上られるオブジェクト

        GameObject stairDowned; // 階段を下りられるオブジェクト

        DoUpdate doUpdate; // Update() メソッド用フラグクラス

        DoFixedUpdate doFixedUpdate; // FixedUpdate() メソッド用フラグクラス

        int life; // ヒットポイント

        Transform leftHandTransform; // IK左手位置用のTransform

        Transform rightHandTransform; // IK右手位置用のTransform

        float waterLevel; // 水面の高さ TODO:プレイヤーのフィールドでOK?

        GameObject playerNeck; // プレイヤーの水面判定用

        GameObject intoWaterFilter; // 水中でのカメラエフェクト用

        GameObject bodyIntoWater; // 水中での体

        Vector3 normalVector = Vector3.up; // 法線用

        Text speechText; // セリフ用吹き出しテキスト

        Image speechImage; // セリフ用吹き出し画像

        float speed; // 速度ベクトル  TODO: speed・position オブジェクト化する

        float previousSpeed; // 1フレ前の速度ベクトル

        Vector3[] previousPosition = new Vector3[30]; // 30フレ分前のポジション保存用

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        public bool Faceing { get => doUpdate.faceing; } // オブジェクトに正対中かどうか

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// HPデクリメント。
        /// </summary>
        public void DecrementLife() {
            life--;
            speechImage.sprite = speechGizaSprite;
            say("Ouch!", 65);
        }

        /// <summary>
        /// エネミーから攻撃を受ける。
        /// </summary>
        public void DamagedByEnemy(Vector3 forward) {
            moveByShocked(forward);
            doUpdate.damaged = true;
            simpleAnime.Play("ClimbUp"); // FIXME: ダメージアニメ
            Observable.TimerFrame(9) // FIXME: 60fpsの時は？
                .Subscribe(_ => {
                    doUpdate.damaged = false;
                });
            speechImage.sprite = speechGizaSprite;
            say("Oh\nmy God!", 65);
        }

        /// <summary>
        /// 攻撃の衝撃で少し後ろ上に動く。
        /// </summary>
        void moveByShocked(Vector3 forward) {
            var _ADJUST = 2.5f; // 調整値
            transform.position += (forward + new Vector3(0f, 0.5f, 0)) / _ADJUST; // 0.5fは上方向調整値
        }

        /// <summary>
        /// 爆弾の爆発時に持ち手を強制パージ
        /// </summary>
        public void PurgeFromBomb() {
            if (holded.name.Contains("Bomb")) {
                holded.transform.parent = null; // 子オブジェクト解除
                doUpdate.holding = false; // 持つフラグOFF
                holded = null; // 持つオブジェクト参照解除
            }
        }

        ///////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            doUpdate = DoUpdate.GetInstance(); // 状態フラグクラス
            doFixedUpdate = DoFixedUpdate.GetInstance(); // 物理挙動フラグクラス
            bombAngle = BombAngle.GetInstance(); // 弾道角度用クラス

            gameSystem = gameObject.GetGameSystem(); // GameSystem 取得
            soundSystem = gameObject.GetSoundSystem(); // SoundSystem 取得
            cameraSystem = gameObject.GetCameraSystem(); // CameraSystem 取得
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            life = 10; // HP初期化
            speed = 0; // 速度初期化

            var _rb = transform.GetComponent<Rigidbody>();
            var _fps = Application.targetFrameRate;

            bool _r2Hold = false; // R2ボタンで持っているかどうか
            bool _r2HoldTmp = false; // R2ボタンで持っているかどうか ※一時フラグ

            // 初期化
            if (SceneManager.GetActiveScene().name != "Start") { // TODO: 再検討
                waterLevel = GameObject.Find("Water").transform.position.y; // 水面の高さを取得 TODO: x, z 軸で水面(水中の範囲取得)
                intoWaterFilter = GameObject.Find("/Canvas"); // 水中カメラエフェクト取得
                bodyIntoWater = transform.Find("Body").gameObject; // 水中での体取得
                playerNeck = transform.Find("Bell").gameObject; // 水面判定用
                speechText = speechObject.GetComponentInChildren<Text>(); // セリフ吹き出しテキスト取得
                speechImage = speechObject.GetComponent<Image>(); // セリフ吹き出し画像取得

                rayBox = GameObject.Find("RayBox").gameObject;
                stepRayBox = GameObject.Find("StepRayBox").gameObject;
                behind = GameObject.Find("Behind").gameObject;
            }

            // 物理挙動: 初期化
            this.FixedUpdateAsObservable().Subscribe(_ => {
                // Time.deltaTime は一定である
                previousSpeed = speed; // 速度ベクトル保存
                speed = _rb.velocity.magnitude; // 速度ベクトル取得
                gameSystem.playerSpeed = speed;
                if (speed > 5.0f) { // 加速度リミッター TODO: リミッター解除機能
                    _rb.velocity = new Vector3(
                        _rb.velocity.x - (_rb.velocity.x / 10),
                        _rb.velocity.y - (_rb.velocity.y / 10),
                        _rb.velocity.z - (_rb.velocity.z / 10)
                    );
                }
            });

            #region situation

            // ポーズ中
            this.UpdateAsObservable().Where(_ => Time.timeScale == 0f)
                .Subscribe(_ => {
                    beSilent();
                });

            // イベント中
            this.UpdateAsObservable().Where(_ => gameSystem.eventView)
                .Subscribe(_ => {
                    beSilent();
                });

            // ステージをクリアした・GAMEオーバーした場合
            this.UpdateAsObservable().Where(_ => (SceneManager.GetActiveScene().name != "Start") &&
                gameSystem.levelClear || gameSystem.gameOver)
                .Subscribe(_ => {
                    beSilent();
                });

            // HP・弾角度を通知 FIXME: リアクティブ
            this.UpdateAsObservable().Where(_ => SceneManager.GetActiveScene().name != "Start")
                .Subscribe(_ => {
                    gameSystem.playerLife = life; // HP設定
                    gameSystem.bombAngle = bombAngle.Value; // 弾角度
                });

            #endregion

            #region hold Balloon

            // 風船につかまり浮遊中
            this.UpdateAsObservable().Where(_ => continueUpdate() && doFixedUpdate.holdBalloon)
                .Subscribe(_ => {
                    doUpdate.grounded = false;
                });

            // 物理挙動: 風船につかまっている
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.holdBalloon)
                .Subscribe(_ => {
                    _rb.drag = 5f; // 抵抗を増やす(※大きな挙動変化をもたらす)
                    _rb.angularDrag = 5f; // 回転抵抗を増やす(※大きな挙動変化をもたらす)
                    _rb.useGravity = false;
                    _rb.AddForce(new Vector3(0, 1.8f, 0), ForceMode.Acceleration); // 1.8f は調整値
                });

            // 物理挙動: 風船を離した ※このコードの位置でないとなぜかNG
            this.FixedUpdateAsObservable().Where(_ => !doFixedUpdate.holdBalloon && !doFixedUpdate.intoWater)
                .Subscribe(_ => {
                    _rb.drag = 0f;
                    _rb.angularDrag = 0f;
                    _rb.useGravity = true;
                });

            #endregion

            #region Default

            // (NOT 上下ボタン) アイドル状態
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && !upButton.isPressed && !downButton.isPressed)
                .Subscribe(_ => {
                    if (!doUpdate.lookBackJumping) { // 捕まり反転ジャンプ中でなければ
                        if (!doUpdate.throwing && !doUpdate.throwed) {
                            if (aButton.isPressed) { // (Aボタン) 押しっぱなし
                                simpleAnime.CrossFade("Push", 0.1f); // しゃがむ(代用)アニメ
                            } else {
                                simpleAnime.Play("Default"); // デフォルトアニメ
                            }
                        }
                        soundSystem.StopClip();
                        doFixedUpdate.idol = true;
                    }
                });

            //  物理挙動: アイドル状態
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.idol)
                .Subscribe(_ => {
                    _rb.useGravity = true; // 重力有効化
                    doFixedUpdate.idol = false;
                });

            #endregion

            #region Run, Walk

            // (上ボタン) 歩く・走る
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && upButton.isPressed)
                .Subscribe(_ => {
                    if (yButton.isPressed) { // (Yボタン) 押しっぱなし
                        if (!doUpdate.throwing && !doUpdate.throwed) {
                            simpleAnime.Play("Run"); // 走るアニメ
                            soundSystem.PlayRunClip();
                        }
                        doFixedUpdate.run = true;
                    } else {
                        if (!doUpdate.throwing && !doUpdate.throwed) {
                            if (aButton.isPressed) { // (Aボタン) 押しっぱなし
                                simpleAnime.CrossFade("Push", 0.1f); // しゃがむ(代用)アニメ
                            } else {
                                simpleAnime.Play("Walk"); // 歩くアニメ
                                soundSystem.PlayWalkClip();
                            }
                        }
                        doFixedUpdate.walk = true;
                    }
                });

            // 物理挙動: 走る
            var _ADJUST1 = 0f;
            if (_fps == 60) _ADJUST1 = 8f;
            if (_fps == 30) _ADJUST1 = 16f;
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.run)
                .Subscribe(_ => {
                    // FIXME: 二段階加速
                    _rb.useGravity = true; // 重力再有効化 
                    if (speed < 3.25f) { // ⇒ フレームレートに依存する 60fps,8f, 30fps:16f, 20fps:24f, 15fps:32f
                        var onPlane = Vector3.ProjectOnPlane(Utils.TransformForward(transform.forward, speed), normalVector);
                        if (normalVector != Vector3.up) {
                            _rb.AddFor​​ce(onPlane * _ADJUST1 / 12f, ForceMode.Impulse); // 12fは調整値
                        } else {
                            _rb.AddFor​​ce(onPlane * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                        }
                    }
                    doFixedUpdate.run = false;
                });

            // 物理挙動: 歩く
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.walk)
                .Subscribe(_ => {
                    _rb.useGravity = true; // 重力再有効化 
                    if (speed < 1.1f) {
                        var onPlane = Vector3.ProjectOnPlane(Utils.TransformForward(transform.forward, speed), normalVector);
                        if (normalVector != Vector3.up) {
                            _rb.AddFor​​ce(onPlane * _ADJUST1 / 12f, ForceMode.Impulse); // 12fは調整値
                        } else {
                            _rb.AddFor​​ce(onPlane * _ADJUST1, ForceMode.Acceleration); // 前に移動させる
                        }
                    }
                    doFixedUpdate.walk = false;
                });

            #endregion

            #region Backward

            // (下ボタン) 後ろ歩き
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && downButton.isPressed)
                .Subscribe(_ => {
                    if (!doUpdate.throwing && !doUpdate.throwed) {
                        if (aButton.isPressed) { // (Aボタン) 押しっぱなし
                            simpleAnime.CrossFade("Push", 0.1f); // しゃがむ(代用)アニメ
                        } else {
                            simpleAnime.Play("Backward"); // 後ろアニメ
                        }
                    }
                    soundSystem.PlayWalkClip();
                    doFixedUpdate.backward = true;
                });

            // 物理挙動: 後ろ歩き
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.backward)
                .Subscribe(_ => {
                    _rb.useGravity = true; // 重力再有効化 
                    if (speed < 0.75f) {
                        var onPlane = Vector3.ProjectOnPlane(-Utils.TransformForward(transform.forward, speed), normalVector);
                        if (normalVector != Vector3.up) {
                            _rb.AddFor​​ce(onPlane * _ADJUST1 / 12f, ForceMode.Impulse); // 12fは調整値
                        } else {
                            _rb.AddFor​​ce(onPlane * _ADJUST1, ForceMode.Acceleration); // 後ろに移動させる
                        }
                    }
                    doFixedUpdate.backward = false;
                });

            #endregion

            #region Rotate

            // (左右ボタン) 回転
            this.UpdateAsObservable().Where(_ => continueUpdate() && !doUpdate.climbing && !aButton.isPressed)
                .Subscribe(_ => {
                    // TODO: 入力の遊びを持たせる？ TODO: 左右2回押しで180度回転？ TODO: 左右2回押しで90度ずつ回転の実装
                    var _ADJUST = 20; // 調整値
                    var _axis = rightButton.isPressed ? 1 : leftButton.isPressed ? -1 : 0;
                    if (Math.Round(speed, 2) == 0) { // 静止時回転は速く
                        transform.Rotate(0, _axis * (rotationalSpeed * Time.deltaTime) * _ADJUST * 1.5f, 0);
                    } else if (speed < 4.5f) { // 加速度制御
                        if (doUpdate.grounded) {
                            transform.Rotate(0, _axis * (rotationalSpeed * Time.deltaTime) * _ADJUST, 0); // 回転は transform.rotate の方が良い
                        } else {
                            transform.Rotate(0, _axis * (rotationalSpeed * Time.deltaTime) * _ADJUST / 1.2f, 0); // ジャンプ中は回転控えめに
                        }
                    }
                });

            #endregion

            #region SideStep

            // (左右ボタン + Aボタン) 左右サイドステップ
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && !doUpdate.climbing && aButton.isPressed)
                .Subscribe(_ => {
                    var _axis = rightButton.isPressed ? 1 : leftButton.isPressed ? -1 : 0;
                    if (_axis == -1) {
                        if (speed < 2.0f) {
                            doFixedUpdate.sideStepLeft = true; // 左ステップ
                        }
                    } else if (_axis == 1) {
                        if (speed < 2.0f) {
                            doFixedUpdate.sideStepRight = true; // 右ステップ
                        }
                    }
                    faceToFace(5); // 面に正対する FIXME: 斜めが有効になる
                });

            // 物理挙動: 左サイドステップ
            var _ADJUST2 = 0f;
            if (_fps == 60) _ADJUST2 = 18f;
            if (_fps == 30) _ADJUST2 = 36f;
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.sideStepLeft)
                .Subscribe(_ => {
                    _rb.AddRelativeFor​​ce(Vector3.left * _ADJUST2, ForceMode.Acceleration); // 左に移動させる
                    doFixedUpdate.sideStepLeft = false;
                });

            // 物理挙動: 右サイドステップ
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.sideStepRight)
                .Subscribe(_ => {
                    _rb.AddRelativeFor​​ce(Vector3.right * _ADJUST2, ForceMode.Acceleration); // 右に移動させる
                    doFixedUpdate.sideStepRight = false;
                });

            #endregion

            #region Jump

            // (Bボタン) ジャンプ
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && !doUpdate.climbing && bButton.wasPressedThisFrame)
                .Subscribe(_ => {
                    doUpdate.InitThrowBomb(); // 撃つフラグOFF
                    simpleAnime.Play("Jump"); // ジャンプアニメ
                    soundSystem.PlayJumpClip();
                    doUpdate.grounded = false;
                    doUpdate.secondsAfterJumped = 0f; // ジャンプ後経過秒リセット
                    doFixedUpdate.jump = true;
                });

            // 物理挙動: ジャンプ
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.jump)
                .Subscribe(_ => {
                    // FIXME: ジャンプボタンを押し続けると飛距離が伸びるように
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
                    //_rb.velocity += Vector3.up * _ADJUST;
                    _rb.AddRelativeFor​​ce(Vector3.up * _ADJUST * 40f, ForceMode.Acceleration); // TODO: こちらの方がベター？
                    doFixedUpdate.jump = false;
                });

            // ジャンプ中移動 (※水中もここに来る)
            this.UpdateAsObservable().Where(_ => continueUpdate() && !doUpdate.grounded && !doUpdate.climbing)
                .Subscribe(_ => {
                    doUpdate.secondsAfterJumped += Time.deltaTime; // ジャンプ後経過秒インクリメント
                    // (上下ボタン) 空中で移動
                    var _axis = upButton.isPressed ? 1 : downButton.isPressed ? -1 : 0;
                    if (_axis == 1) { // 前移動
                        doFixedUpdate.jumpForward = true;
                        if (checkIntoWater()) { soundSystem.PlayWaterForwardClip(); }
                    } else if (_axis == -1) { // 後ろ移動
                        doFixedUpdate.jumpBackward = true;
                        if (checkIntoWater()) { soundSystem.StopClip(); }
                    } else {
                        if (checkIntoWater()) { soundSystem.StopClip(); }
                    }
                    if (!checkIntoWater() && !bButton.isPressed && doUpdate.secondsAfterJumped > 5.0f && !doFixedUpdate.holdBalloon) { // TODO: checkIntoWater 重くない？
#if DEBUG
                        Debug.Log("JUMP後に空中停止した場合 speed:" + speed); // FIXME: 水面で反応 TODO: 何かボタンを押したら復帰させる
#endif
                        transform.Translate(0, -5.0f * Time.deltaTime, 0); // 下げる
                        doUpdate.grounded = true; // 接地
                        doFixedUpdate.unintended = true; // 意図しない状況フラグON
                    }
                    // モバイル動作時に面に正対する FIXME: ジャンプ後しばらくたってから: UniRx
                    if (useVirtualController && !checkIntoWater() && !doFixedUpdate.holdBalloon) { // FIXME: checkHoldBalloon()
                        faceToFace(5f);
                    }
                });

            // 物理挙動: ジャンプ中前移動
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.jumpForward)
                .Subscribe(_ => {
                    if (!checkIntoWater()) {
                        if (speed < 3.25f) {
                            _rb.AddRelativeFor​​ce(Vector3.forward * 6.5f, ForceMode.Acceleration);
                        }
                    } else { // 水中移動
                        if (speed < 3.25f) {
                            _rb.AddRelativeFor​​ce(Vector3.forward * 13.0f, ForceMode.Acceleration);
                        }
                    }
                    doFixedUpdate.jumpForward = false;
                });

            // 物理挙動: ジャンプ中後ろ移動
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.jumpBackward)
                .Subscribe(_ => {
                    if (!checkIntoWater()) {
                        if (speed < 1.5f) {
                            _rb.AddRelativeFor​​ce(Vector3.back * 4.5f, ForceMode.Acceleration);
                        }
                    } else { // 水中移動
                        if (speed < 1.5f) {
                            _rb.AddRelativeFor​​ce(Vector3.back * 9.0f, ForceMode.Acceleration);
                        }
                    }
                    doFixedUpdate.jumpBackward = false;
                });

            // 物理挙動: 意図していない状況
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.unintended)
                .Subscribe(_ => {
                    _rb.useGravity = true; // 重力有効化
                    _rb.velocity = Vector3.zero; // 速度0にする
                    doFixedUpdate.unintended = false;
                });

            #endregion

            #region into Water

            // 水面に接触したら
            this.OnTriggerEnterAsObservable().Where(x => x.gameObject.LikeWater())
                .Subscribe(_ => {
                    if (transform.localPosition.y + 0.75f > waterLevel) { // 0.75f は調整値 ⇒ TODO:再検討
                        soundSystem.PlayWaterInClip();
                    }
                });

            // 水中である
            this.UpdateAsObservable().Where(_ => continueUpdate() && checkIntoWater())
                .Subscribe(_ => {
                    if (upButton.isPressed) {
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
                });

            // 水中ではない
            this.UpdateAsObservable().Where(_ => continueUpdate() && !checkIntoWater())
                .Subscribe(_ => {
                    intoWaterFilter.GetComponent<Image>().enabled = false; // TODO: GetComponent をオブジェクト参照に
                });

            // (Yボタン) 水中で押した
            this.UpdateAsObservable().Where(_ => continueUpdate() && checkIntoWater() && yButton.wasPressedThisFrame)
                .Subscribe(_ => {
                    soundSystem.PlayWaterSinkClip(); // 水中で沈む音
                });

            // 物理挙動: 水に入ったら
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.intoWater)
                .Subscribe(_ => {
                    _rb.drag = 5f; // 抵抗を増やす(※大きな挙動変化をもたらす)
                    _rb.angularDrag = 5f; // 回転抵抗を増やす(※大きな挙動変化をもたらす)
                    _rb.useGravity = false;
                    _rb.AddForce(new Vector3(0, 3.8f, 0), ForceMode.Acceleration); // 3.8f は調整値
                    _rb.mass = 2f;
                });

            // 物理挙動: 水から出たら
            this.FixedUpdateAsObservable().Where(_ => !doFixedUpdate.intoWater && !doFixedUpdate.holdBalloon)
                .Subscribe(_ => {
                    _rb.drag = 0f;
                    _rb.angularDrag = 0f;
                    _rb.useGravity = true;
                    _rb.mass = 3.5f;
                });

            #endregion

            #region push Block

            // (Aボタン) しゃがむ ※アニメはここではない
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && aButton.isPressed)
                .Subscribe(_ => {
                    // しゃがむ時、持ってるモノを離す
                    if (holded != null) {
                        holded.transform.parent = null; // 子オブジェクト解除
                        doUpdate.holding = false; // 持つフラグOFF
                        holded = null; // 持つオブジェクト参照解除
                    }
                });

            // (Aボタン + 上ボタン) ブロックを押す
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && !doUpdate.climbing && aButton.wasPressedThisFrame && upButton.isPressed)
                .Subscribe(_ => {
                    if (checkToPushBlock()) {
                        //startFaceing(); // オブジェクトに正対する開始
                        faceToObject(pushed); // オブジェクトに正対する
                    }
                });

            #endregion

            #region Y Button

            // (Yボタン) 押しっぱなし: 登り降り発動
            this.UpdateAsObservable().Where(_ => continueUpdate() && yButton.isPressed && !doUpdate.holding && !doUpdate.climbing)
                .Subscribe(_ => {
                    if (!doUpdate.lookBackJumping) { // 捕まり反転ジャンプが発動してなかったら
                        if (downButton.isPressed) { // ハシゴを降りる
                            if (previousPosition[0].y - (0.1f * Time.deltaTime) > transform.position.y) {
                                checkToClimbDownByLadder();
                            }
                        }
                    }
                    checkToClimb(); // よじ登り可能かチェック
                });

            // 物理挙動: 登り降り発動
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.climbUp)
                .Subscribe(_ => {
                    _rb.useGravity = false; // 重力無効化 ※重力に負けるから
                    _rb.velocity = Vector3.zero;
                    doFixedUpdate.climbUp = false;
                });

            // 物理挙動: 登り降りキャンセル
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.cancelClimb)
                .Subscribe(_ => {
                    _rb.useGravity = true; // 重力再有効化
                    _rb.AddRelativeFor​​ce(Vector3.down * 3f, ForceMode.Impulse); // 落とす
                    doFixedUpdate.cancelClimb = false;
                });

            // (Yボタン) 押しっぱなし: 登り降り中
            this.UpdateAsObservable().Where(_ => continueUpdate() && yButton.isPressed && !doUpdate.holding && doUpdate.climbing)
                .Subscribe(_ => {
                    simpleAnime.Play("ClimbUp"); // よじ登るアニメ
                    if (l1Button.isPressed) { // (Lボタン) 押しっぱなしなら
                        simpleAnime.Play("Default"); // 捕まり反転ジャンプ準備
                    }
                    climb(); // 登り降り
                    if (r1Button.isPressed) { // (Rボタン) 押しっぱなしなら
                        moveSide(); // 横に移動
                    }
                });

            // 物理挙動: 登り降り中
            this.FixedUpdateAsObservable().Where(_ => doUpdate.climbing)
                .Subscribe(_ => {
                    _rb.useGravity = false; // 重力無効化 ※重力に負けるから
                    _rb.velocity = Vector3.zero;
                });

            // 物理挙動: 捕まり反転ジャンプ
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (doFixedUpdate.reverseJump) {
                        _rb.useGravity = true;
                        _rb.velocity += Vector3.up * jumpPower / 2.0f;
                        _rb.velocity += transform.forward * jumpPower / 3.5f;
                        doFixedUpdate.reverseJump = false;
                    }
                });

            // (Yボタン) 離した
            this.UpdateAsObservable().Where(_ => continueUpdate() && yButton.wasReleasedThisFrame)
                .Subscribe(_ => {
                    if (doUpdate.climbing) {
                        simpleAnime.Play("Default"); // デフォルトアニメ
                        soundSystem.StopClip();
                    }
                    // RayBox位置の初期化
                    rayBox.transform.localPosition = new Vector3(0, 0.4f, 0.1f); // RayBoxローカルポジション
                    doUpdate.climbing = false; // 登るフラグOFF
                    doFixedUpdate.cancelClimb = true;
                });

            // (Yボタン) モバイル用モード
            this.UpdateAsObservable().Where(_ => continueUpdate() && yButton.wasReleasedThisFrame && useVirtualController)
                .Subscribe(_ => {
                    doFixedUpdate.virtualControllerMode = true;
                    Observable.TimerFrame(45) // 45フレ後に
                        .Subscribe(__ => {
                            doFixedUpdate.virtualControllerMode = false;
                        });
                });

            #endregion

            #region L1 Button

            // (Lボタン) 敵ロック(※敵に注目)
            this.UpdateAsObservable().Where(_ => continueUpdate() && l1Button.isPressed)
                .Subscribe(_ => {
                    //lockOnTarget(); // TODO: 捕まり反転ジャンプの時に向いてしまう…
                });

            // (Lボタン) 離した(※捕まり反転ジャンプ準備のカメラリセット)
            this.UpdateAsObservable().Where(_ => continueUpdate() && l1Button.wasReleasedThisFrame)
                .Subscribe(_ => {
                    cameraSystem.ResetLookAround(); // カメラ初期化
                });

            // (Lボタン + 上ボタン) 弾道角度調整※*反応速度
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && upButton.isPressed && l1Button.isPressed)
                .Subscribe(_ => {
                    bombAngle.Value -= Time.deltaTime * 2.5f;
                });

            // (Lボタン + 下ボタン) 弾道角度調整※*反応速度
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && downButton.isPressed && l1Button.isPressed)
                .Subscribe(_ => {
                    bombAngle.Value += Time.deltaTime * 2.5f;
                });

            #endregion

            #region R1, R2 Button

            // (Rボタン) 持つ・撃つ
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && (r1Button.wasPressedThisFrame || (r2Button.wasPressedThisFrame && !_r2Hold)))
                .Subscribe(_ => {
                    if (checkToFace() && checkToHoldItem()) { // アイテムが持てるかチェック
                        startFaceing(); // オブジェクトに正対する開始
                        faceToObject(holded); // オブジェクトに正対する
                        if (r2Button.wasPressedThisFrame) { _r2HoldTmp = true; } // (R2ボタン) R2ホールドフラグON
                        if (holded.gameObject.name.Contains("Balloon")) { // 風船を持った
                            doUpdate.grounded = false; // 浮遊
                            doFixedUpdate.holdBalloon = true;
                        }
                    } else { // アイテムが持てない場合、弾を撃つフラグON
                        if (!doUpdate.throwing && !doUpdate.throwed && (!doUpdate.holding || !doUpdate.faceing)) {
                            simpleAnime.CrossFade("Throw", 0.3f); // 投げるアニメ
                            doUpdate.throwing = true; // 投げるフラグON
                        }
                    }
                });

            // 撃つ
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && !doUpdate.climbing && !doUpdate.holding && doUpdate.bombing)
                .Subscribe(_ => {
                    bomb(); // 弾を撃つ
                    doUpdate.bombed = true;
                });

            // 投げた後のモーション
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && !doUpdate.climbing && !doUpdate.holding)
                .Subscribe(_ => {
                    if (aButton.isPressed && doUpdate.throwed) { // (Aボタン) 押した時
                        simpleAnime.CrossFade("Push", 0.2f); // 投げるからしゃがむ(代用)アニメ
                    } else if (yButton.isPressed && doUpdate.throwed) { // (Yボタン) 押した時
                        simpleAnime.CrossFade("Run", 0.2f); // 投げるから走るアニメ
                    } else if (doUpdate.throwed) {
                        simpleAnime.CrossFade("Walk", 0.2f); // 投げるから歩くアニメ
                    }
                });

            // (Rボタン) 離した:R2ホールド
            this.UpdateAsObservable().Where(_ => continueUpdate() && _r2Hold && r2Button.wasPressedThisFrame)
                .Subscribe(_ => {
                    if (holded != null) {
                        if (holded.gameObject.name.Contains("Balloon")) { doFixedUpdate.holdBalloon = false; } // 風船を離した
                        if (holded.LikeKey()) { gameSystem.hasKey = false; } // キー保持フラグOFF
                        holded.transform.parent = null; // 子オブジェクト解除
                        doUpdate.holding = false; // 持つフラグOFF
                        holded = null; // 持つオブジェクト参照解除
                        _r2Hold = false; // R2ホールドフラグOFF
                    }
                });

            // (Rボタン) 離した
            this.UpdateAsObservable().Where(_ => continueUpdate() && (r1Button.wasReleasedThisFrame || (!r1Button.isPressed && doUpdate.holding)) && !_r2Hold)
                .Subscribe(_ => {
                    if (holded != null) {
                        if (holded.gameObject.name.Contains("Balloon")) { doFixedUpdate.holdBalloon = false; } // 風船を離した
                        if (holded.LikeKey()) { gameSystem.hasKey = false; } // キー保持フラグOFF
                        holded.transform.parent = null; // 子オブジェクト解除
                        doUpdate.holding = false; // 持つフラグOFF
                        holded = null; // 持つオブジェクト参照解除
                    }
                });

            #endregion

            #region StairUp, StairDown

            // 階段チェック
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.grounded && upButton.isPressed)
                .Subscribe(_ => {
                    // 階段を上がるかチェック
                    checkStairUp();
                    if (doUpdate.stairUping != true) {
                        // 階段を下がるかチェック
                        checkStairDown();
                    }
                });

            // 階段を上る
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.stairUping)
                .Subscribe(_ => {
                    doStairUp();
                });

            // 階段を下りる
            this.UpdateAsObservable().Where(_ => continueUpdate() && doUpdate.stairDowning)
                .Subscribe(_ => {
                    doStairDown();
                });

            // 物理挙動: 階段を上る
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.stairUp)
                .Subscribe(_ => {
                    _rb.useGravity = false; // 重力無効化 ※重力に負けるから
                    _rb.velocity = Vector3.zero;
                    doFixedUpdate.stairUp = false;
                });

            // 物理挙動: 階段を下りる
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.stairDown)
                .Subscribe(_ => {
                    _rb.useGravity = false; // 重力無効化 ※重力に負けるから
                    _rb.velocity = Vector3.zero;
                    doFixedUpdate.stairDown = false;
                });

            #endregion

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(_ => {
                if (doUpdate.climbing) { // 捕まり反転ジャンプの準備
                    if (leftButton.isPressed) {
                        AxisToggle.Left = AxisToggle.Left == true ? false : true;
                    } else if (rightButton.isPressed) {
                        AxisToggle.Right = AxisToggle.Right == true ? false : true;
                    }
                    if (l1Button.isPressed) { // Lボタン押しっぱなし TODO: ボタンの変更
                        readyForBackJump();
                    }
                }
                if (_r2HoldTmp) { _r2Hold = true; _r2HoldTmp = false; } // R2ホールドフラグON 
                cashPreviousPosition(); // 10フレ前分の位置情報保存
                gameSystem.playerAlt = transform.position.y;
            });

            // ブロックに接触したら
            this.OnCollisionEnterAsObservable().Where(x => x.gameObject.LikeBlock())
                .Subscribe(x => {
                    if (isUpOrDown()) { // 上下変動がある場合
                        // 上に乗った状況
                        if (!isHitSide(x.gameObject)) {
                            simpleAnime.Play("Default"); // デフォルトアニメ
                            soundSystem.PlayGroundedClip();
                            doUpdate.grounded = true; // 接地フラグON
                            doUpdate.stairUping = false; // 階段上りフラグOFF
                            doUpdate.stairDowning = false; // 階段下りフラグOFF
                            doFixedUpdate.grounded = true;
                            cameraSystem.ResetLookAround(); // カメラ初期化
                            doUpdate.secondsAfterJumped = 0f; // ジャンプ後経過秒リセット TODO:※試験的
                            flatToFace(); // 面に合わせる TODO:※試験的
                        } else if (isHitSide(x.gameObject)) {
                            // 下に当たった場合
                            if (isHitBlockBottom(x.gameObject)) {
                                soundSystem.PlayKnockedupClip();
                                x.gameObject.GetComponent<Common>().shockedBy = transform; // 下から衝撃を与える
                            }
                            // 横に当たった場合
                            else {
                            }
                        }
                    }
                });

            // 地上・壁・坂に接地したら
            this.OnCollisionEnterAsObservable().Where(x => !x.gameObject.LikeBlock() && (x.gameObject.LikeGround() || x.gameObject.LikeWall()) && !checkIntoWater())
                .Subscribe(_ => {
                    simpleAnime.Play("Default"); // デフォルトアニメ
                    soundSystem.PlayGroundedClip();
                    doUpdate.grounded = true; // 接地フラグON
                    doFixedUpdate.grounded = true;
                    cameraSystem.ResetLookAround(); // カメラ初期化
                    doUpdate.secondsAfterJumped = 0f; // ジャンプ後経過秒リセット TODO:※試験的
                    flatToFace(); // 面に合わせる TODO:※試験的
                });

            // 物理挙動: 上に乗った状況・接地
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.grounded)
                .Subscribe(_ => {
                    _rb.useGravity = true; // 重力再有効化 
                    _rb.velocity = Vector3.zero; // FIXME: ここで接地フリーズさせている
                    doFixedUpdate.grounded = false;
                });

            // 持てるアイテム、ブロック、"Holdable" と接触したら
            this.OnCollisionEnterAsObservable().Where(x => (x.gameObject.LikeItem() || x.gameObject.Holdable()) && !doUpdate.holding)
                .Subscribe(x => {
                    doUpdate.grounded = true; // 接地フラグON
                    doFixedUpdate.grounded = true;
                    holded = x.gameObject; // 持てるアイテムの参照を保持する
                });

            // ブロックに接触し続けている
            this.OnCollisionStayAsObservable().Where(x => x.gameObject.LikeBlock())
                .Subscribe(x => {
                    // 横に当たった場合
                    if (isHitSide(x.gameObject)) {
                        // TODO:
                    }
                });

            // アイテムブロックから離れたら
            this.OnCollisionExitAsObservable().Where(x => x.gameObject.LikeBlock() && x.gameObject.LikeItem())
                .Subscribe(_ => {
                    if (!doUpdate.holding) { // 持つ(Rボタン)を離した
                        holded = null; // 持てるブロックの参照を解除する
                    }
                });

            // 持てるアイテム、ブロック、"Holdable"  から離れたら
            this.OnCollisionExitAsObservable().Where(x => (x.gameObject.LikeItem() || x.gameObject.Holdable()) && !doUpdate.holding)
                .Subscribe(_ => {
                    holded = null; // 持てるブロックの参照を解除する
                });

            #region get damaged

            // 被弾したら FIXME: 音はここでOK？ バウンドした弾でも音が鳴る
            this.OnCollisionEnterAsObservable().Where(x => x.gameObject.LikeBullet())
                .Subscribe(_ => {
                    soundSystem.PlayHitClip();
                });

            #endregion

            #region get Item

            // アイテムと接触したら
            this.OnTriggerEnterAsObservable().Where(x => x.gameObject.Getable())
                .Subscribe(x => {
                    soundSystem.PlayItemClip(); // 効果音を鳴らす
                    gameSystem.DecrementItem(); // アイテム数デクリメント
                    doFixedUpdate.getItem = true;
                    Destroy(x.gameObject); // 削除
                    speechImage.sprite = speechRightSprite;
                    say("I got\na item."); // FIXME: 吹き出しの種類
                    gameSystem.AddScore(50000); // FIXME: 一時的 ⇒ サウンド
                });

            // 物理挙動: アイテム取得
            this.FixedUpdateAsObservable().Where(_ => doFixedUpdate.getItem)
                .Subscribe(_ => {
                    _rb.velocity = Vector3.zero; // アイテム取得時停止 FIXME: 動作してる？
                    doFixedUpdate.getItem = false;
                });

            #endregion

            #region Slope

            // スロープに接触したら
            this.OnCollisionEnterAsObservable().Where(x => x.gameObject.LikeSlope())
                .Subscribe(x => {
                    normalVector = x.GetContact(0).normal; // 法線取得
                    doUpdate.grounded = true; // 接地フラグON
                });

            // スロープに接触し続けている
            this.OnCollisionStayAsObservable()
                .Where(x => x.gameObject.name.Contains("Slope"))
                .Subscribe(x => {
                    normalVector = x.GetContact(0).normal; // 法線取得
                });

            // スロープから離脱したら
            this.OnCollisionExitAsObservable().Where(x => x.gameObject.LikeSlope())
                .Subscribe(_ => {
                    normalVector = Vector3.up; // 法線を戻す
                });

            #endregion

            #region Speech Bubble

            // セリフ追従
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    speechObject.transform.position = RectTransformUtility.WorldToScreenPoint(
                        Camera.main,
                        transform.position + getSpeechOffset(transform.forward)
                    );
                });

            #endregion
        }

        // Update is called once per frame.
        void Update() {
            doUpdate.IncrementTime(Time.deltaTime); // 時間インクリメント
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Event handler

        // OnAnimatorIK is called by the Animator Component immediately before it updates its internal IK system.
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

        // OnGUI is called for rendering and handling GUI events.
        void OnGUI() {
            if (SceneManager.GetActiveScene().name != "Start") { // TODO: 再検討
                //// デバッグ表示
                //var _y = string.Format("{0:F3}", Math.Round(transform.position.y, 3, MidpointRounding.AwayFromZero));
                //var _s = string.Format("{0:F3}", Math.Round(speed, 3, MidpointRounding.AwayFromZero));
                //var _aj = string.Format("{0:F3}", Math.Round(doUpdate.secondsAfterJumped, 3, MidpointRounding.AwayFromZero));
                //var _rb = transform.GetComponent<Rigidbody>();
                //gameSystem.TRACE("Hight: " + _y + "m \r\nSpeed: " + _s + "m/s" +
                //    "\r\nGrounded: " + doUpdate.grounded +
                //    "\r\nClimbing: " + doUpdate.climbing +
                //    "\r\nHolding: " + doUpdate.holding +
                //    "\r\nStairUp: " + doUpdate.stairUping +
                //    "\r\nStairDown: " + doUpdate.stairDowning +
                //    //"\r\nThrowing: " + doUpdate.throwing +
                //    //"\r\nThrowed: " + doUpdate.throwed +
                //    "\r\nGravity: " + _rb.useGravity +
                //    "\r\nJumped: " + _aj + "sec",
                //    speed, 3.0f
                //);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Update 操作を続けるかどうかを返す。
        /// </summary>
        bool continueUpdate() {
            // ポーズ中
            if (Time.timeScale == 0f) {
                return false;
            }
            // ダメージ受け期間
            if (doUpdate.damaged) {
                return false;
            }
            // ゲームイベント中・レベルクリア・ゲームオーバー
            if (gameSystem.eventView || gameSystem.levelClear || gameSystem.gameOver) {
                return false;
            }
            // (Xボタン) 押しっぱなし: 視点操作中
            if (xButton.isPressed) {
                return false;
            }
            // オブジェクトに正対中キャラ操作無効
            if (doUpdate.faceing) {
                if (holded != null) {
                    faceToObject(holded);
                }
                return false;
            }
            // ブロックを押してる時キャラ操作無効
            if (doUpdate.pushing && transform.parent != null) {
                simpleAnime.Play("Push"); // 押すアニメ
                pushed.GetComponent<Block>().pushed = true; // ブロックを押すフラグON
                return false;
            // 押してるブロックの子オブジェクト化が解除されたら
            } else if (doUpdate.pushing && transform.parent == null) {
                doUpdate.pushing = false; // 押すフラグOFF
                pushed = null; // 押すブロックの参照解除
                simpleAnime.Play("Default"); // デフォルトアニメ
            }
            return true;
        }

        // TODO: 一時凍結
        //if (Input.GetButtonUp("L1")) {
        //    lookBack(); // TODO: 未完成
        //}
        /// <summary>
        /// 後ろをふりかえる。
        /// </summary>
        void lookBack() {
            float _SPEED = 10.01f; // 回転スピード
            //transform.LookAt(_behind.transform);
            // TODO: gameobject を new ?
            var _target = behind.transform;
            var _relativePos = _target.position - transform.position;
            var _rotation = Quaternion.LookRotation(_relativePos);
            transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, Time.deltaTime * _SPEED);
            //cameraController.LookPlayer();
        }

        /// <summary>
        /// nフレ前分の位置情報保存する。
        /// </summary>
        void cashPreviousPosition() {
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

        /// <summary>
        /// 弾を撃つ。
        /// </summary>
        void bomb() { // TODO: fps で加える値を変化？
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
            speechImage.sprite = speechRightSprite;
            say("Shot!", 65);
        }

        /// <summary>
        /// TBA
        /// </summary>
        float getRendererTop(GameObject target) { // TODO: Player が測っても良いのでは？
            float _height = target.GetComponent<Renderer>().bounds.size.y; // オブジェクトの高さ取得 
            float _y = target.transform.position.y; // オブジェクトのy座標取得(※0基点)
            float _top = _height + _y; // オブジェクトのTOP取得
            return _top;
        }

        /// <summary>
        /// 上下変動があったかどうか。
        /// </summary>
        bool isUpOrDown() {
            var _fps = Application.targetFrameRate;
            var _ADJUST1 = 0;
            if (_fps == 60) _ADJUST1 = 9;
            if (_fps == 30) _ADJUST1 = 20; // FIXME: 正直ここは詳細な検討が必要 TODO: ⇒ x, z 軸でも変動があったか調べる？
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

        /// <summary>
        /// 登り降り中に横に移動する。
        /// </summary>
        void moveSide() {
            faceToFace();
            var _MOVE = 0.8f;
            var _fX = (float) Math.Round(transform.forward.x);
            var _fZ = (float) Math.Round(transform.forward.z);
            var _axis = rightButton.isPressed ? 1 : leftButton.isPressed ? -1 : 0;
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

        /// <summary>
        /// 面に高さを合わせる。
        /// </summary>
        void flatToFace() { // TODO:※試験中
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                (float) Math.Round(transform.position.y, 2, MidpointRounding.AwayFromZero),
                transform.localPosition.z
            );
        }

        /// <summary>
        /// 面に正対する。
        /// </summary>
        void faceToFace(float speed = 20.0f) {
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
        bool checkToFace() {
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
            return false; // 判定不可 FIXME: この部分に関連する実装を再検討する
        }

        /// <summary>
        /// オブジェクトに正対する。
        /// </summary>
        void faceToObject(GameObject target, float speed = 2.0f) {
            if (!target.name.Contains("Block")) { // FIXME: ブロック以外は取り合えず無効
                doUpdate.faceing = false;
            }
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
            //Debug.Log("1034 faceToObject doUpdate.faceing: " + doUpdate.faceing + "_fx: " + _fx + " _fz: " + _fz);
        }

        /// <summary>
        /// オブジェクトに正対するフラグの開始。
        /// </summary>
        void startFaceing() {
            doUpdate.faceing = true;
        }

        ///// <summary>
        ///// オブジェクトに正対するフラグの解除。
        ///// </summary>
        //void doneFaceing() {
        //    doUpdate.faceing = false;
        //}

        /// <summary>
        /// ロックオン対象の方向に回転。
        /// </summary>
        void lockOnTarget() {
            var target = gameSystem.SerchNearTargetByTag(gameObject, "Block");
            if (target != null) {
                float _SPEED = 3.0f; // 回転スピード
                Vector3 _look = target.transform.position - transform.position; // ターゲット方向へのベクトル
                Quaternion _rotation = Quaternion.LookRotation(new Vector3(_look.x, 0, _look.z)); // 回転情報に変換※Y軸はそのまま
                transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, _SPEED * Time.deltaTime); // 徐々に回転
            }
        }

        /// <summary>
        /// 捕まり反転ジャンプの準備。
        /// </summary>
        void readyForBackJump() {
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

        /// <summary>
        /// 捕まり反転ジャンプ。
        /// </summary>
        void doBackJump() {
            transform.Rotate(0, 180f, 0); // 180度反転
            doUpdate.climbing = false; // 登るフラグOFF
            doUpdate.lookBackJumping = true; // 反転ジャンプフラグON
            simpleAnime.Play("Jump"); // ジャンプアニメ
            soundSystem.PlayJumpClip();
            doFixedUpdate.reverseJump = true;
        }

        /// <summary>
        /// TBA
        /// </summary>
        void checkToClimb() {
            Ray _ray = new Ray(rayBox.transform.position, transform.forward); // RayBoxから前方サーチする
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.2f)) { // 前方にレイを投げて反応があった場合
                if (_hit.transform.name.Contains("Block") ||
                    _hit.transform.name.Contains("Ladder") ||
                    _hit.transform.name.Contains("Wall") ||
                    _hit.transform.name.Contains("Ground")) { // ブロック、ハシゴ、壁、地面で
                    if (_hit.transform.GetComponent<Common>().climbable) { // 登ることが可能なら
                        var _hitTop = getRaycastHitTop(_hit); // 前方オブジェクトのtop位置を取得
#if DEBUG
                        Debug.DrawRay(_ray.origin, _ray.direction * 0.2f, Color.green, 3, false); //レイを可視化
#endif
                        float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                        if (_distance < 0.15) { // 距離が近くなら
                            var _myY = transform.position.y; // 自分のy位置(0基点)を取得
                            if (_myY < _hitTop) { // 自分が前方オブジェクトより低かったら
                                doUpdate.climbing = true; // 登るフラグON
                                speechImage.sprite = speechRightSprite;
                                say("I grabbed\nthat."); // FIXME: 種別
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ハシゴを降りるとき限定。
        /// </summary>
        void checkToClimbDownByLadder() {
            int _fps = Application.targetFrameRate;
            float _ADJUST = 0;
            if (_fps == 60) _ADJUST = 75.0f; // 調整値
            if (_fps == 30) _ADJUST = 37.5f; // 調整値
            Ray _ray = new Ray(rayBox.transform.position, transform.forward); // RayBoxから前方サーチする
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
                            speechImage.sprite = speechRightSprite;
                            say("I grabbed\nthat."); // FIXME: 種別
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ハシゴ登り降り。
        /// </summary>
        void climb() {
            Ray _ray = new Ray(
                new Vector3(rayBox.transform.position.x, rayBox.transform.position.y, rayBox.transform.position.z), // RayBoxから前方サーチする
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
                    if (upButton.isPressed) { // (上ボタン) を押した時
                        transform.Translate(0, 1f * Time.deltaTime, 0); // 登る
                        doUpdate.grounded = false; // 接地フラグOFF
                        if (rayBox.transform.position.y > transform.position.y) { // キャラ高さの範囲で
                            rayBox.transform.localPosition = new Vector3(0, rayBox.transform.localPosition.y - (1f * 1.5f * Time.deltaTime), 0.1f); // RayBoxは逆に動かす
                        }
                        soundSystem.PlayClimbClip();
                    } else if (downButton.isPressed) { // (下ボタン) を押した時
                        transform.Translate(0, -1f * Time.deltaTime, 0); // 降りる
                        doUpdate.grounded = false; // 接地フラグOFF
                        if (rayBox.transform.position.y < transform.position.y + 0.4f) { // キャラ高さの範囲で 0.4 は_rayBoxの元の位置
                            rayBox.transform.localPosition = new Vector3(0, rayBox.transform.localPosition.y + (1f * 1.5f * Time.deltaTime), 0.1f); // RayBoxは逆に動かす
                        }
                        soundSystem.PlayClimbClip();
                    } else { // 一時停止
                        // (Bボタン) を押したら反転ジャンプする
                        if (bButton.wasPressedThisFrame) {
                            doBackJump();
                        }
                    }
                } else { // 自分が対象オブジェクトのtop位置より高くなったら
                    Ray _ray2 = new Ray( // 少し上を確認する
                        new Vector3(rayBox.transform.position.x, rayBox.transform.position.y + 0.2f, rayBox.transform.position.z),
                        transform.forward
                    );
                    if (Physics.Raycast(_ray2, out RaycastHit _hit2, 0.3f)) { // 前方にレイを投げて反応があった場合
#if DEBUG
                        Debug.DrawRay(_ray2.origin, _ray2.direction * 0.3f, Color.cyan, 5, false); //レイを可視化
#endif 
                        transform.position += transform.up * 1f * Time.deltaTime; // まだ続くので少し上に上げる
                    } else {
                        ///////////////////////////////////////////////////////////////////////////////////////////
                        rayBox.transform.localPosition = new Vector3(0, 0.4f, 0.1f); // RayBoxローカルポジション
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
                rayBox.transform.localPosition = new Vector3(0, 0.4f, 0.1f); // RayBoxローカルポジション
                doUpdate.climbing = false; // 登るフラグOFF
                transform.position += transform.forward * 0.2f * Time.deltaTime; // 少し前に進む
                doFixedUpdate.cancelClimb = true; // 登り降りキャンセル
            }
        }

        /// <summary>
        /// 階段を上るフラグチェック。
        /// </summary>
        void checkStairUp() { // FIXME: Rayが捜査するオブジェクトが増えるだけでタイミングが破綻する
            Ray _ray = new Ray(
                new Vector3(stepRayBox.transform.position.x, stepRayBox.transform.position.y + 0.1f, stepRayBox.transform.position.z), // StepRayBoxから前方サーチする
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
                        doUpdate.stairDowning = false; // 階段を下りるフラグOFF
                        stairUped = _hit.transform.gameObject; // 階段を上がられるオブジェクトの参照保存
                    }
                }
            }
        }

        /// <summary>
        /// 階段を下りるフラグチェック。
        /// </summary>
        void checkStairDown() { // FIXME: Rayが捜査するオブジェクトが増えるだけでタイミングが破綻する
            Ray _ray = new Ray(
                new Vector3(stepRayBox.transform.position.x, stepRayBox.transform.position.y + 0.1f, stepRayBox.transform.position.z), // StepRayBoxから前方サーチする
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
                        doUpdate.stairUping = false; // 階段を上るフラグOFF
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

        /// <summary>
        /// 階段を下りる。
        /// </summary>
        void doStairDown() {
            doFixedUpdate.stairDown = true;
            // 上下を離した時
            if (upButton.isPressed == false && downButton.isPressed == false) {
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

        /// <summary>
        /// 階段を上る。
        /// </summary>
        void doStairUp() {
            doFixedUpdate.stairUp = true;
            if (getRendererTop(stairUped) > transform.position.y) {
                // 上下を離した時
                if (upButton.isPressed == false && downButton.isPressed == false) {
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

        /// <summary>
        /// TBA
        /// </summary>
        bool checkToPushBlock() {
            Ray _ray = new Ray(
                new Vector3(stepRayBox.transform.position.x, stepRayBox.transform.position.y + 0.1f, stepRayBox.transform.position.z), // StepRayBoxから前方サーチする
                transform.forward
            );
            if (Physics.Raycast(_ray, out RaycastHit _hit, 0.35f)) { // 前方にレイを投げて反応があった場合
                var _hitTop = getRaycastHitTop(_hit); // 前方オブジェクトのtop位置を取得
#if DEBUG
                Debug.DrawRay(_ray.origin, _ray.direction * 0.35f, Color.magenta, 3, false); //レイを可視化
#endif 
                // TODO: 押し可能なブロックの判定
                if (_hit.transform.name.Contains("Block")) { // 押せるのはブロックのみ
                    if (_hit.transform.GetComponent<Block>().pushable) { // 押せるブロックの場合
                        float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                        if (_distance < 0.3) { // 距離が近くなら
                            if (!doUpdate.pushing) { // 押してない
                                doUpdate.pushing = true; // 押すフラグON
                                pushed = _hit.transform.gameObject; // 押されるオブジェクトの参照保存
                                transform.parent = pushed.transform; // プレイヤーを押されるオブジェクトの子にする
                                speechImage.sprite = speechRightSprite;
                                say("I'm going\nto push.");
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

        /// <summary>
        /// TBA
        /// </summary>
        bool checkToHoldItem() {
            if (holded != null) { // 持つオブジェクトの参照があれば
                Ray _ray = new Ray(
                    new Vector3(stepRayBox.transform.position.x, stepRayBox.transform.position.y + 0.3f, stepRayBox.transform.position.z), // StepRayBoxから前方サーチする
                    transform.forward
                );
                if (Physics.Raycast(_ray, out RaycastHit _hit, 0.35f) || checkDownAsHoldableBlock()) { // 前方にレイを投げて反応があった場合
#if DEBUG
                    Debug.DrawRay(_ray.origin, _ray.direction * 0.35f, Color.magenta, 4, false); //レイを可視化
#endif
                    if (checkDownAsHoldableBlock() || _hit.transform.name.Contains("Item")) { // 持てるのはアイテムのみ TODO: 子のオブジェクト判定は？
                        float _distance = _hit.distance; // 前方オブジェクトまでの距離を取得
                        if (_distance < 0.3f || checkDownAsHoldableBlock()) { // 距離が近くなら
                            Observable.EveryUpdate() // MEMO: Observable.EveryUpdate() だと "checkToHoldItem()" から呼ばれる以外でも発火する！
                                .Where(_ => !doUpdate.holding)
                                .Select(_ => !doUpdate.faceing && holded != null && !doUpdate.holding) // なぜ Where だとダメ？
                                .Subscribe(_ => {
                                    if (holded.tag.Equals("Block")) {
                                        var _blockController = holded.GetComponent<Block>();
                                        leftHandTransform = _blockController.GetLeftHandTransform(); // ブロックから左手のIK位置を取得
                                        rightHandTransform = _blockController.GetRightHandTransform(); // ブロックから右手のIK位置を取得
                                    } else if (holded.tag.Equals("Holdable")) {
                                        var _holdable = holded.GetComponent<Holdable>();
                                        leftHandTransform = _holdable.GetLeftHandTransform(); // ブロックから左手のIK位置を取得
                                        rightHandTransform = _holdable.GetRightHandTransform(); // ブロックから右手のIK位置を取得
                                    }
                                    holded.transform.parent = transform; // 自分の子オブジェクトにする
                                    doUpdate.holding = true; // 持つフラグON
                                    if (holded.LikeKey() && !gameSystem.levelClear) { // TODO: なぜここがクリア時に呼ばれる？
                                        gameSystem.hasKey = true; // キー保持フラグON
                                        speechImage.sprite = speechRightSprite;
                                        say("Yeah~\nI got\n the Key!", 60, 2d); // FIXME: 種別
                                    }
                                });
                            speechImage.sprite = speechRightSprite;
                            say("I'm going\nto have.");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 足元の下が持てるブロックかどうか。
        /// </summary>
        bool checkDownAsHoldableBlock() {
            if (holded != null) {
                return true;
            } // TODO: 修正
            return false;
        }

        /// <summary>
        /// 水中にいるかチェック。
        /// </summary>
        /// <returns></returns>
        bool checkIntoWater() { // TODO 空間が水中でなはい時は？
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

        /// <summary>
        /// レイを投げた対象のtop位置を取得。
        /// </summary>
        float getRaycastHitTop(RaycastHit hit) {
            float _hitHeight = hit.collider.GetComponent<Renderer>().bounds.size.y; // 対象オブジェクトの高さ取得 
            float _hitY = hit.transform.position.y; // 対象オブジェクトの(※中心)y座標取得
            return _hitHeight + _hitY; // 対象オブジェクトのtop位置取得
        }

        /// <summary>
        /// 衝突したオブジェクトの側面に当たったか判定する。
        /// </summary>
        bool isHitSide(GameObject target) {
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

        /// <summary>
        /// 衝突したブロックの下に当たったか判定する。
        /// </summary>
        bool isHitBlockBottom(GameObject target) {
            var _targetBottom = target.transform.position.y; // 当たったブロックの底面の高さ
            float _height = GetComponent<CapsuleCollider>().bounds.size.y; // 自分のコライダーの高さ
            float _y = transform.position.y; // 自分のy座標(※0基点)
            float _top = _height + _y; // 自分のTOP位置
            if (_top - 0.1f < _targetBottom) { // ブロックの底面が自分のTOP位置より低かったら※0.1fは誤差
                return true; // ブロックの底面に当たった
            }
            return false; // そうではない
        }

        /// <summary>
        /// Player の方向を列挙体で返す。
        /// </summary>
        Direction getDirection(Vector3 forwardVector) {
            var _fX = (float) Math.Round(forwardVector.x);
            var _fY = (float) Math.Round(forwardVector.y);
            var _fZ = (float) Math.Round(forwardVector.z);
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                return Direction.PositiveZ;
            }
            if (_fX == 0 && _fZ == -1) { // Z軸負方向
                return Direction.NegativeZ;
            }
            if (_fX == 1 && _fZ == 0) { // X軸正方向
                return Direction.PositiveX;
            }
            if (_fX == -1 && _fZ == 0) { // X軸負方向
                return Direction.NegativeX;
            }
            // ここに来たら二軸の差を判定する TODO: ロジック再確認
            float _abX = Math.Abs(forwardVector.x);
            float _abZ = Math.Abs(forwardVector.z);
            if (_abX > _abZ) {
                if (_fX == 1) { // X軸正方向
                    return Direction.PositiveX;
                }
                if (_fX == -1) { // X軸負方向
                    return Direction.NegativeX;
                }
            } else if (_abX < _abZ) {
                if (_fZ == 1) { // Z軸正方向
                    return Direction.PositiveZ;
                }
                if (_fZ == -1) { // Z軸負方向
                    return Direction.NegativeZ;
                }
            }
            return Direction.None; // 判定不明
        }

        /// <summary>
        /// セリフ吹き出しのオフセット値を返す。MEMO: Z軸正方向が基準
        /// </summary>
        Vector3 getSpeechOffset(Vector3 forwardVector) {
            var _direction = getDirection(forwardVector);
            if (_direction == Direction.PositiveX) {
                return new Vector3(speechOffset.z, speechOffset.y, -speechOffset.x);
            } else if (_direction == Direction.NegativeX) {
                return new Vector3(speechOffset.z, speechOffset.y, speechOffset.x);
            } else if (_direction == Direction.PositiveZ) {
                return speechOffset;
            } else if (_direction == Direction.NegativeZ) {
                return new Vector3(-speechOffset.x, speechOffset.y, speechOffset.z);
            }
            return speechOffset;
        }

        /// <summary>
        /// セリフ用吹き出しにセリフを表示する。 // FIXME: 吹き出しの形
        /// </summary>
        void say(string text, int size = 60, double time = 0.5d) {
            speechText.text = text;
            speechText.fontSize = size;
            speechObject.SetActive(true);
            Observable.Timer(TimeSpan.FromSeconds(time))
                .First()
                .Subscribe(_ => {
                    speechObject.SetActive(false);
                });
        }

        /// <summary>
        /// セリフ用吹き出しを非表示にする。
        /// </summary>
        void beSilent() {
            speechObject.SetActive(false);
        }

        #region DoUpdate

        /// <summary>
        /// Update() メソッド用のクラス。
        /// </summary>
        protected class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            bool _grounded; // 接地フラグ
            bool _climbing; // 登り降りフラグ
            bool _pushing; // 押すフラグ
            bool _holding; // 持つフラグ
            bool _faceing; // 正対するフラグ
            bool _stairUping; // 階段上りフラグ
            bool _stairDowning; // 階段下りフラグ
            bool _lookBackJumping; // 捕まり反転ジャンプフラグ

            float _secondsAfterJumped; // ジャンプして何秒経ったか

            bool _throwing;
            bool _throwed;
            float _throwingTime;
            float _throwedTime;
            bool _bombing;
            bool _bombed;

            bool _damaged; // ダメージを受けるフラグ

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool grounded {
                get => _grounded;
                set {
                    _grounded = value;
                    if (_grounded) { lookBackJumping = false; }
                }
            }
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

            public void InitThrowBomb() { // FIXME: rename
                _throwing = false;
                _throwed = false;
                _throwingTime = 0f;
                _throwedTime = 0f;
                _bombing = false;
                _bombed = false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // private Methods [verb]

            void throwBomb(float time) {
                if (_throwing && !_bombed && _throwingTime > 0.5f) {
                    _bombing = true;
                } else if (_throwing && _bombed && _throwingTime > 0.5f) {
                    _bombing = false;
                }
                if (_throwing && _throwingTime > 0.8f) { // 投げるフラグONから時間が経った
                    _throwing = false; // 投げるフラグOFF
                    _throwed = true; // 投げたフラグON
                    _throwingTime = 0f; // 経過時間リセット
                    _bombed = false;
                } else if (_throwing) {
                    _throwingTime += time; // 経過時間加算
                }
                if (_throwed && _throwedTime > 0.5f) { // 投げたフラグから時間が経った
                    _throwed = false; // 投げたフラグOFF
                    _throwedTime = 0f; // 経過時間リセット
                } else if (_throwed) {
                    _throwedTime += time; // 経過時間加算
                }
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

            bool _idol;
            bool _run;
            bool _walk;
            bool _jump;
            bool _reverseJump;
            bool _backward;
            bool _sideStepLeft;
            bool _sideStepRight;
            bool _climbUp;
            bool _cancelClimb;
            bool _jumpForward;
            bool _jumpBackward;
            bool _grounded;
            bool _getItem;
            bool _stairUp;
            bool _stairDown;
            bool _unintended; // 意図していない状況
            bool _intoWater;
            bool _holdBalloon;
            bool _virtualControllerMode;

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
            public bool holdBalloon { get => _holdBalloon; set => _holdBalloon = value; }
            public bool virtualControllerMode { get => _virtualControllerMode; set => _virtualControllerMode = value; }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static DoFixedUpdate GetInstance() {
                return new DoFixedUpdate();
            }

        }

        #endregion

        #region BombAngle

        /// <summary>
        /// 弾道角度用のクラス。
        /// </summary>
        protected class BombAngle {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            float _value;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public float Value {
                get { return _value; }
                set {
                    if (_value + value > -1.0f && _value + value < 2.5f) { // 角度制限 -0.5, 1.25 をスライダーUIに設定する
                        _value = value;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            public static BombAngle GetInstance() {
                var _instance = new BombAngle();
                _instance.init();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // private Methods [verb]

            void init() {
                _value = 1.25f;
            }

        }

        #endregion

        #region AxisToggle

        class AxisToggle {
            public static bool Up = false;
            public static bool Down = false;
            public static bool Left = false;
            public static bool Right = false;
        }

        #endregion
    }

}
