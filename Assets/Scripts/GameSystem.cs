using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// ゲームシステムの処理
    /// </summary>
    public class GameSystem : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 設定・参照 (bool => is+形容詞、has+過去分詞、can+動詞原型、三単現動詞)

        [SerializeField]
        private Text information; // アイテム数表示用テキストUI

        [SerializeField]
        private Text message; // メッセージ表示用テキストUI

        [SerializeField]
        private Text debug; // デバッグ表示用テキストUI

        [SerializeField]
        private Slider playerLifeUI; // Player HPのUI

        [SerializeField]
        private Slider playerBombAngleUI; // Player 弾道角度のUI

        [SerializeField]
        private Material lockonFocusMaterial; // ロックオン用マテリアル

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        private int itemTotalCount = 0; // アイテム総数

        private int itemRemainCount = 0; // アイテム残数

        private bool isGameOver = false; // GAMEオーバーフラグ

        private bool isLevelClear = false; // ステージクリアフラグ

        private float playerLifeValue = 10f; // Player HP;

        private float playerBombAngleValue = 0f; // Player 弾道角度;

        private GameObject lockonTarget = null; // ロックオン対象

        private Material lockonOriginalMaterial; // ロックオン対象の元のマテリアル

        private bool useVibration = true; // スマホ時に振動を使うかどうか

        private bool isPausing = false; // ポーズ(一時停止)

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // FPS計測

        int fpsForUpdateFrameCount; // FPSフレームカウント

        float fpsForUpdatePreviousTime; // FPS前フレーム秒

        float fpsForUpdate = 0f; // FPS

        // FPS計測 Fixed

        int fpsForFixedUpdateFrameCount; // FixedUpdate() FPSフレームカウント

        float fpsForFixedUpdatePreviousTime; // FixedUpdate() FPS前フレーム秒

        float fpsForFixedUpdate = 0f; // FixedUpdate() FPS

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プロパティ(キャメルケース: 名詞、形容詞)

        public bool gameOver { // GAMEオーバーかどうかを返す
            get => isGameOver;
        }

        public bool levelClear { // ステージをクリアしたかどうかを返す
            get => isLevelClear;
        }

        public int playerLife {
            set => playerLifeValue = (float) value / 10; // UIの仕様が 0～1 なので
        }

        public float bombAngle {
            // min: -0.125
            // max: 1.25
            // ⇒ スライダーにこの値を設定する方が早い
            set => playerBombAngleValue = value; // UIの仕様が 0～1 なので
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // パブリックメソッド

        public void GameStart() {
            SceneManager.LoadScene("Level1"); // Level1に遷移
        }

        public void Return() {
            SceneManager.LoadScene("Start"); // スタート画面に戻る
        }

        public void DecrementItem() {
            itemRemainCount--; // アイテム数デクリメント
            updateGameInfo(); // TODO: OnGUI ?
        }

        public void ClearLevel() { // Levelクリア
            Time.timeScale = 0f;
            isLevelClear = true; // ステージクリアフラグON
            message.text = "Level Clear!"; // クリアメッセージ表示
        }

        /// <summary>
        /// 指定されたタグの中で最も近いオブジェクトを返す。(ロックオンシステム)
        /// </summary>
        public GameObject SerchNearTargetByTag(GameObject player, string tag) {
            float _tmp = 0;
            float _near = 0;
            var _previousTarget = lockonTarget;
            foreach (GameObject _obj in GameObject.FindGameObjectsWithTag(tag)) { // タグで管理する方がシンプル
                if (_obj.GetComponent<BlockController>().IsRenderedInMainCamera()) { // カメラに写っている場合
                    _tmp = Vector3.Distance(_obj.transform.position, player.transform.position); // 自分とオブジェクトの距離を計測
                    if (_near == 0 || _near > _tmp) { // 距離の近いオブジェクトを保存
                        _near = _tmp;
                        lockonTarget = _obj;
                    }
                }
            }
            if (lockonTarget) {
                if (lockonOriginalMaterial != null) {
                    _previousTarget.GetComponent<Renderer>().material = lockonOriginalMaterial; // 前回のターゲットを元のマテリアルに戻す
                }
                lockonOriginalMaterial = lockonTarget.GetComponent<Renderer>().material; // 新しいターゲットのマテリアル保存
                lockonTarget.GetComponent<Renderer>().material = lockonFocusMaterial; // 新しいターゲットにフォーカスマテリアルを適用
            }
            return lockonTarget;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Awake is called when the script instance is being loaded.
        void Awake() {
            // フレームレートを設定
            Application.targetFrameRate = 30; // 60fps と 30fps のみ仕上げる
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            // ボタンを押したらスマホ振動
            this.UpdateAsObservable()
                .Where(_ => virtualController && useVibration &&(aButton.wasPressedThisFrame || bButton.wasPressedThisFrame || xButton.wasPressedThisFrame || yButton.wasPressedThisFrame ||
                    dpadUp.wasPressedThisFrame || dpadDown.wasPressedThisFrame || dpadLeft.wasPressedThisFrame || dpadRight.wasPressedThisFrame ||
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

            // ポーズ(一時停止)実行・解除
            this.UpdateAsObservable()
                .Where(_ => SceneManager.GetActiveScene().name.Contains("Level") && startButton.wasPressedThisFrame && !isLevelClear)
                .Subscribe(_ => {
                    if (isPausing) {
                        Time.timeScale = 1f;
                        message.text = "";
                    } else {
                        Time.timeScale = 0f;
                        message.text = "Pause";
                    }
                    isPausing = !isPausing;
                });

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Where(_ => !SceneManager.GetActiveScene().name.Equals("Start"))
                .Subscribe(_ => {
                    checkGameOver(); // GAMEオーバー確認
                    checkKey(); // レベルクリア確認
                    updateGameInfo();
                    updatePlayerStatus();
                });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    updateFpsForFixedUpdate();
                });

            // 砲台全破壊
            var _fieldEnemy = false;
            this.UpdateAsObservable()
                .Where(_ => SceneManager.GetActiveScene().name.Contains("Level") && GameObject.Find("Cannon").transform.childCount == 0)
                .Subscribe(_ => {
                    _fieldEnemy = true;
                });

            // フィールド敵出現
            this.UpdateAsObservable()
                .Where(_ => SceneManager.GetActiveScene().name.Contains("Level") && _fieldEnemy)
                .Subscribe(_ => {
                    var _enemy = GameObject.Find("FieldEnemy");
                    foreach (Transform _child in _enemy.transform) {
                        _child.gameObject.SetActive(true);
                    }
                });

            // TODO: スタート画面の場合、おそらく以下でエラーが出てる

            initFpsForUpdate(); // FPS初期化
            initFpsForFixedUpdate(); // fixed FPS初期化

            if (SceneManager.GetActiveScene().name == "Start") { // スタート画面の場合
                Time.timeScale = 1f; // 一時停止解除
                return;
            }

            // アイテムの総数を取得 // TODO: ⇒ OnGUI に移動？
            itemTotalCount = GameObject.FindGameObjectsWithTag("Item").Length;
            itemRemainCount = itemTotalCount;
            updateGameInfo();

            message.text = ""; // メッセージ初期化、非表示
        }

        // Update is called once per frame.
        new void Update() {
            base.Update();

            if (SceneManager.GetActiveScene().name == "Start") { // スタート画面の場合
                if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Level1"); // TODO: メソッド
                }
                return;
            } else if (SceneManager.GetActiveScene().name == "Level1") { // Level1の場合のAボタン遷移
                if (selectButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Start"); // TODO: メソッド
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プライベートメソッド(キャメルケース: 動詞)

        private void checkGameOver() {
            if (playerLifeValue == 0) {
                Time.timeScale = 0f;
                isGameOver = true; // GAMEオーバーフラグON
                message.text = "Game Over!"; // GAMEオーバーメッセージ表示
                if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Start");
                }
            }
        }

        private void checkKey() { // 全てのアイテムを集めたらキー出現
            if (isLevelClear == false && itemTotalCount > 0 && itemRemainCount == 0) {
                var _key = GameObject.Find("Keys"); // フォルダオブジェクトは有効でないとNG
                foreach (Transform _child in _key.transform) {
                    _child.gameObject.SetActive(true); // キー出現
                }
            }
        }

        private void updatePlayerStatus() { // Player のステイタス表示
            playerLifeUI.value = playerLifeValue;
            playerBombAngleUI.value = playerBombAngleValue;
        }

        private void updateGameInfo() { // GAME の情報を表示
            updateFpForUpdate(); // FPS更新
            information.text = "Item (" + itemRemainCount + "/" + itemTotalCount + ")" + 
                               "\r\nfps1 " + string.Format("{0:F3}", Math.Round(fpsForUpdate, 3, MidpointRounding.AwayFromZero)) + 
                               "\r\nfps2 " + string.Format("{0:F3}", Math.Round(fpsForFixedUpdate, 3, MidpointRounding.AwayFromZero)); // 残りアイテム数表示
        }

        private void updateFpForUpdate() { // FPS更新
            ++fpsForUpdateFrameCount; // FPS取得
            float _time = Time.realtimeSinceStartup - fpsForUpdatePreviousTime;
            if (_time >= 0.5f) {
                fpsForUpdate = fpsForUpdateFrameCount / _time;
                fpsForUpdateFrameCount = 0;
                fpsForUpdatePreviousTime = Time.realtimeSinceStartup;
            }
        }

        private void initFpsForUpdate() { // FPS初期化
            fpsForUpdateFrameCount = 0;
            fpsForUpdatePreviousTime = 0.0f;
        }

        private void updateFpsForFixedUpdate() { // fixed FPS更新
            ++fpsForFixedUpdateFrameCount; // fixed FPS取得
            float _time = Time.realtimeSinceStartup - fpsForFixedUpdatePreviousTime;
            if (_time >= 0.5f) {
                fpsForFixedUpdate = fpsForFixedUpdateFrameCount / _time;
                fpsForFixedUpdateFrameCount = 0;
                fpsForFixedUpdatePreviousTime = Time.realtimeSinceStartup;
            }
        }

        private void initFpsForFixedUpdate() { // fixed FPS初期化
            fpsForFixedUpdateFrameCount = 0;
            fpsForFixedUpdatePreviousTime = 0.0f;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // デバッグ

        public void TRACE(string value) {
            debug.text = value;
        }

        public void TRACE(string value1, float value2, float limit) {
            if (value2 > limit + 2f) {
                debug.color = Color.magenta;
            } else if (value2 > limit) {
                debug.color = Color.red;
            } else {
                debug.color = Color.black;
            }
            debug.text = value1;
        }
    }

}
