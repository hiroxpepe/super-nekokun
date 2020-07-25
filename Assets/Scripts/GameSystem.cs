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
    /// ゲームシステムの処理
    /// @author h.adachi
    /// </summary>
    public class GameSystem : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string MESSAGE_LEVEL_START = "Get items!";
        const string MESSAGE_LEVEL_CLEAR = "Level Clear!";
        const string MESSAGE_GAME_OVER = "Game Over!";
        const string MESSAGE_GAME_PAUSE = "Pause";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Text messageUI; // ゲーム メッセージ テキストUI

        [SerializeField]
        Text fpsUI; // ゲーム FPS テキストUI

        [SerializeField]
        Text timeUI; // ゲーム TIME テキストUI

        [SerializeField]
        Text scoreUI; // ゲーム スコア テキストUI

        [SerializeField]
        Text itemUI; // ゲーム アイテム テキストUI

        [SerializeField]
        Text keyUI; // ゲーム キー テキストUI

        [SerializeField]
        Text hpUI; // プレイヤー HP テキストUI

        [SerializeField]
        Text speedUI; // プレイヤー 速度 テキストUI

        [SerializeField]
        Text altUI; // プレイヤー 高度 テキストUI

        [SerializeField]
        Slider bombAngleUI; // プレイヤー 弾道角度 スライダーUI

        [SerializeField]
        Text debugUI; // デバッグ テキストUI

        [SerializeField]
        Material lockonFocusMaterial; // ロックオン用マテリアル

        [SerializeField]
        Camera mainCamera; // メインカメラ

        [SerializeField]
        Camera eventCamera; // イベントカメラ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        int itemTotalCount = 0; // アイテム総数

        int itemRemainCount = 0; // アイテム残数

        bool isGameOver = false; // ゲームオーバーフラグ

        bool isLevelClear = false; // レベルクリアフラグ

        float lifeValue = 10f; // プレイヤー HP

        float speedValue = 0f; // プレイヤー 速度

        float altValue = 0f; // プレイヤー 高度 

        int scoreValue = 0; // ゲーム スコア

        int hasKeyValue = 0; // キー保持

        float bombAngleValue = 0f; // プレイヤー 弾道角度

        GameObject lockonTarget = null; // ロックオン対象

        Material lockonOriginalMaterial; // ロックオン対象の元のマテリアル

        bool useVibration = true; // スマホ時に振動を使うかどうか

        bool isPausing = false; // ポーズ(一時停止)

        bool isEventView = false; // イベント中かどうか

        int fpsForUpdateFrameCount; // FPSフレームカウント

        float fpsForUpdatePreviousTime; // FPS前フレーム秒

        float fpsForUpdate = 0f; // FPS

        int fpsForFixedUpdateFrameCount; // FixedUpdate() FPSフレームカウント

        float fpsForFixedUpdatePreviousTime; // FixedUpdate() FPS前フレーム秒

        float fpsForFixedUpdate = 0f; // FixedUpdate() FPS

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        public bool gameOver { // GAMEオーバーかどうかを返す
            get => isGameOver;
        }

        public bool levelClear { // レベルをクリアしたかどうかを返す
            get => isLevelClear;
        }

        public int playerLife {
            set => lifeValue = (float) value / 10; // UIの仕様が 0～1 なので
        }

        public float playerSpeed {
            set => speedValue = value;
        }

        public float playerAlt {
            set => altValue = value;
        }

        public bool hasKey {
            set {
                if (value) { hasKeyValue = 1; } else { hasKeyValue = 0; }
            }
        }

        public float bombAngle {
            // min: -0.125
            // max: 1.25
            // ⇒ スライダーにこの値を設定する方が早い
            set => bombAngleValue = value; // UIの仕様が 0～1 なので
        }
        
        public bool eventView { // イベント中かどうかを返す
            get => isEventView;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Level スタート
        /// </summary>
        public void StartLevel() {
            SceneManager.LoadScene("Level1");
        }

        /// <summary>
        /// スタート画面に戻る
        /// </summary>
        public void ReturnStart() {
            SceneManager.LoadScene("Start");
        }

        /// <summary>
        /// アイテム数デクリメント
        /// </summary>
        public void DecrementItem() {
            itemRemainCount--;
            updateGameStatus();
        }

        /// <summary>
        /// スコア加算
        /// </summary>
        public void AddScore(int value) {
            scoreValue += value;
        }

        /// <summary>
        /// Level クリア
        /// </summary>
        public void ClearLevel() {
            Time.timeScale = 0f;
            isLevelClear = true;
            messageUI.text = MESSAGE_LEVEL_CLEAR;
        }

        /// <summary>
        /// 指定されたタグの中で最も近いオブジェクトを返す。(ロックオンシステム)
        /// </summary>
        public GameObject SerchNearTargetByTag(GameObject player, string tag) {
            float _tmp = 0;
            float _near = 0;
            var _previousTarget = lockonTarget;
            foreach (GameObject _obj in GameObject.FindGameObjectsWithTag(tag)) { // タグで管理する方がシンプル
                if (_obj.GetBlock().IsRenderedInMainCamera()) { // カメラに写っている場合
                    _tmp = Vector3.Distance(_obj.transform.position, player.transform.position); // 自分とオブジェクトの距離を計測
                    if (_near == 0 || _near > _tmp) { // 距離の近いオブジェクトを保存
                        _near = _tmp;
                        lockonTarget = _obj;
                    }
                }
            }
            if (lockonTarget) {
                if (lockonOriginalMaterial != null) {
                    _previousTarget.GetRenderer().material = lockonOriginalMaterial; // 前回のターゲットを元のマテリアルに戻す
                }
                lockonOriginalMaterial = lockonTarget.GetRenderer().material; // 新しいターゲットのマテリアル保存
                lockonTarget.GetRenderer().material = lockonFocusMaterial; // 新しいターゲットにフォーカスマテリアルを適用
            }
            return lockonTarget;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            // フレームレートを設定
            Application.targetFrameRate = 30; // 60fps と 30fps のみ仕上げる
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            // レベル名取得
            var _activeSceneName = SceneManager.GetActiveScene().name;

            #region time.

            // 経過時間測定
            System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
            _stopwatch.Start();
            this.UpdateAsObservable()
                .Where(_ => _activeSceneName.Contains("Level"))
                .Subscribe(_ => {
                    timeUI.text = string.Format("Time\n{0:000}", 999f - Math.Round(_stopwatch.Elapsed.TotalSeconds, 0, MidpointRounding.AwayFromZero));
                });

            // ポーズ: 実行・解除
            this.UpdateAsObservable()
                .Where(_ => _activeSceneName.Contains("Level") && startButton.wasPressedThisFrame && !isLevelClear)
                .Subscribe(_ => {
                    if (isPausing) { Time.timeScale = 1f; messageUI.text = ""; } else { Time.timeScale = 0f; messageUI.text = MESSAGE_GAME_PAUSE; }
                    isPausing = !isPausing;
                });

            // ポーズ: 実行中、経過時間測定 一時停止
            this.UpdateAsObservable()
                .Where(_ => _activeSceneName.Contains("Level") && startButton.isPressed && !isLevelClear && isPausing)
                .Subscribe(_ => {
                    _stopwatch.Stop();
                });

            // ポーズ: 解除、経過時間測定 再開
            this.UpdateAsObservable()
                .Where(_ => _activeSceneName.Contains("Level") && startButton.isPressed && !isLevelClear && !isPausing)
                .Subscribe(_ => {
                    _stopwatch.Start();
                });

            #endregion

            #region update screen.

            // 画面情報表示の更新
            this.UpdateAsObservable().Where(_ => !_activeSceneName.Equals("Start"))
                .Subscribe(_ => {
                    checkGameStatus(); // ゲーム ステイタス確認
                    updateGameStatus(); // ゲーム ステイタス更新
                    updatePlayerStatus(); // プレイヤー ステイタス更新
                });

            #endregion

            // レベル Start での遷移操作
            this.UpdateAsObservable().Where(_ => _activeSceneName == "Start")
                .Subscribe(_ => {
                    if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                        SceneManager.LoadScene("Level1"); // TODO: メソッド
                    }
                });

            // レベル Level1 での遷移操作
            this.UpdateAsObservable().Where(_ => _activeSceneName == "Level1")
                .Subscribe(_ => {
                    if (selectButton.wasPressedThisFrame) {
                        SceneManager.LoadScene("Start"); // TODO: メソッド
                    }
                });

            // レベルをクリアした・ゲームオーバーした場合
            this.UpdateAsObservable().Where(_ => (_activeSceneName != "Start") &&
                this.levelClear || this.gameOver)
                .Subscribe(_ => {
                    isPausing = !isPausing;
                });

            #region all cannons are destroyed, field enemies emerge.

            // 砲台全破壊したら、フィールド敵出現
            var _cannon = GameObject.Find("Cannon");
            this.UpdateAsObservable()
                .First(_ => _activeSceneName.Contains("Level") && _cannon.transform.childCount == 0)
                .Subscribe(_ => {
                    var _enemy = GameObject.Find("FieldEnemy");
                    foreach (Transform _child in _enemy.transform) {
                        _child.gameObject.SetActive(true);
                    }
                    // イベントカメラ切り替え
                    eventCamera.gameObject.transform.parent = GameObject.Find("EnemyEventView").transform;
                    eventCamera.gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                    eventCamera.gameObject.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
                    changeCamera();
                    isEventView = true;
                    Observable.TimerFrame(30) // FIXME: 60fpsの時は？
                        .Subscribe(__ => {
                            changeCamera();
                            isEventView = false;
                        });
                });

            #endregion

            #region the goal key appears.

            // キー出現
            this.UpdateAsObservable()
                .First(_ => isLevelClear == false && itemTotalCount > 0 && itemRemainCount == 0)
                .Subscribe(_ => {
                    var _key = GameObject.Find("Keys"); // フォルダオブジェクトは有効でないとNG
                    foreach (Transform _child in _key.transform) {
                        _child.gameObject.SetActive(true); // キー出現
                    }
                    // イベントカメラ切り替え
                    Observable.Timer(TimeSpan.FromSeconds(0.5d))
                        .Subscribe(__ => {
                            eventCamera.gameObject.transform.parent = GameObject.Find("KeyEventView").transform;
                            eventCamera.gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                            eventCamera.gameObject.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
                            changeCamera();
                            isEventView = true;
                            Observable.TimerFrame(30) // FIXME: 60fpsの時は？
                                .Subscribe(___ => {
                                    changeCamera();
                                    isEventView = false;
                                });
                        });
                });

            #endregion

            #region mobile phone vibration.

            // ボタンを押したらスマホ振動
            this.UpdateAsObservable()
                .Where(_ => virtualController && useVibration && (aButton.wasPressedThisFrame || bButton.wasPressedThisFrame || xButton.wasPressedThisFrame || yButton.wasPressedThisFrame ||
                    upButton.wasPressedThisFrame || downButton.wasPressedThisFrame || leftButton.wasPressedThisFrame || rightButton.wasPressedThisFrame ||
                    l1Button.wasPressedThisFrame || r1Button.wasPressedThisFrame || selectButton.wasPressedThisFrame || startButton.wasPressedThisFrame))
                .Subscribe(_ => {
                    AndroidVibrator.Vibrate(50L);
                });

            // スタート、Xボタン同時押しでスマホの振動なし
            this.UpdateAsObservable()
                .Where(_ => (xButton.isPressed && startButton.wasPressedThisFrame) || (xButton.wasPressedThisFrame && startButton.isPressed))
                .Subscribe(_ => {
                    useVibration = !useVibration;
                });

            #endregion

            #region debug mode message.

            initFpsForUpdate(); // FPS初期化
            initFpsForFixedUpdate(); // FPS初期化 (fixed)

            // FPS 更新処理 (fixed)
            this.FixedUpdateAsObservable().Where(_ => !_activeSceneName.Equals("Start"))
                .Subscribe(_ => {
                    updateFpsForFixedUpdate();
                });

            #endregion

            #region init.

            // スタート画面の場合
            if (_activeSceneName == "Start") {
                Time.timeScale = 1f; // 一時停止解除
                return;
            }
            // レベルの場合、スタートメッセージ表示、非表示
            else {
                messageUI.text = MESSAGE_LEVEL_START;
                Observable.Timer(System.TimeSpan.FromSeconds(1))
                    .First()
                    .Subscribe(_ => {
                        messageUI.text = "";
                    });
            }

            // レベル内の取得可能アイテムの総数を取得
            itemTotalCount = GameObject.FindGameObjectsWithTag("Getable").Length;
            itemRemainCount = itemTotalCount;

            // カメラ初期化
            eventCamera.enabled = false;
            mainCamera.enabled = true;

            #endregion

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// ゲーム ステイタスの確認
        /// </summary>
        void checkGameStatus() {
            if (lifeValue == 0) {
                Time.timeScale = 0f;
                isGameOver = true; // ゲームオーバーフラグON
                messageUI.text = MESSAGE_GAME_OVER;
                if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Start");
                }
            }
        }

        /// <summary>
        /// ゲーム ステイタスの表示
        /// </summary>
        void updateGameStatus() {
            updateFpsForUpdate(); // FPS 表示更新
            scoreUI.text = string.Format("Score\n{0:000000}", scoreValue);
            itemUI.text = string.Format("× {0}/{1}", itemTotalCount - itemRemainCount, itemTotalCount);
            keyUI.text = string.Format("× {0}", hasKeyValue);
        }

        /// <summary>
        /// プレイヤー ステイタス表示
        /// </summary>
        void updatePlayerStatus() {
            bombAngleUI.value = bombAngleValue;
            int _hp = (int) (lifeValue * 10);
            hpUI.text = _hp.ToString();
            speedUI.text = string.Format("Speed {0:000.0}km", Math.Round(speedValue * 5, 1, MidpointRounding.AwayFromZero)); // * 5 は調整値
            altUI.text = string.Format("ALT {0:000.0}m", Math.Round(altValue, 1, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// FPS 表示更新
        /// </summary>
        void updateFpsForUpdate() {
            ++fpsForUpdateFrameCount;
            float _time = Time.realtimeSinceStartup - fpsForUpdatePreviousTime;
            if (_time >= 0.5f) {
                fpsForUpdate = fpsForUpdateFrameCount / _time;
                fpsForUpdateFrameCount = 0;
                fpsForUpdatePreviousTime = Time.realtimeSinceStartup;
                fpsUI.text = string.Format("{0:00.0}fps", Math.Round(fpsForUpdate, 1, MidpointRounding.AwayFromZero));
            }
        }

        /// <summary>
        /// FPS 初期化
        /// </summary>
        void initFpsForUpdate() {
            fpsForUpdateFrameCount = 0;
            fpsForUpdatePreviousTime = 0.0f;
        }

        /// <summary>
        /// FPS 表示更新 (fixed)
        /// </summary>
        void updateFpsForFixedUpdate() {
            ++fpsForFixedUpdateFrameCount;
            float _time = Time.realtimeSinceStartup - fpsForFixedUpdatePreviousTime;
            if (_time >= 0.5f) {
                fpsForFixedUpdate = fpsForFixedUpdateFrameCount / _time;
                fpsForFixedUpdateFrameCount = 0;
                fpsForFixedUpdatePreviousTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// FPS 初期化 (fixed)
        /// </summary>
        void initFpsForFixedUpdate() {
            fpsForFixedUpdateFrameCount = 0;
            fpsForFixedUpdatePreviousTime = 0.0f;
        }

        /// <summary>
        /// カメラ切り替え
        /// </summary>
        void changeCamera() {
            eventCamera.enabled = !eventCamera.enabled;
            mainCamera.enabled = !mainCamera.enabled;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Debug

        public void TRACE(string value) {
            debugUI.text = value;
        }

        public void TRACE(string value1, float value2, float limit) {
            if (value2 > limit + 2f) {
                debugUI.color = Color.magenta;
            } else if (value2 > limit) {
                debugUI.color = Color.red;
            } else {
                debugUI.color = Color.black;
            }
            debugUI.text = value1;
        }
    }

}
