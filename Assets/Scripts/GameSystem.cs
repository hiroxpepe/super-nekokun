using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// ゲームシステムの処理
    /// </summary>
    public class GameSystem : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 設定・参照 (bool => is+形容詞、has+過去分詞、can+動詞原型、三単現動詞)

        [SerializeField]
        private Text information; // アイテム数表示用テキストUI

        [SerializeField]
        private Text message; // メッセージ表示用テキストUI

        [SerializeField]
        private Text debug; // デバッグ表示用テキストUI

        [SerializeField]
        private Button home; // 戻るボタンUI

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

        private GameObject virtualController; // バーチャルコントロール

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
        // ゲームパッド ボタン

        private ButtonControl aButton;

        private ButtonControl bButton;

        private ButtonControl xButton;

        private ButtonControl yButton;

        private ButtonControl dpadUp;

        private ButtonControl dpadDown;

        private ButtonControl dpadLeft;

        private ButtonControl dpadRight;

        private ButtonControl startButton;

        private ButtonControl selectButton;

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
        void Start() {
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
            home.transform.gameObject.SetActive(false);

            virtualController = GameObject.Find("VirtualController"); // バーチャルコントローラー参照取得

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    checkGameOver(); // GAMEオーバー確認
                    checkLevelClear(); // レベルクリア確認
                    updateGameInfo();
                    updatePlayerStatus();
                });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    updateFpsForFixedUpdate();
                });
        }

        // Update is called once per frame.
        void Update() {
            getGamepadInput(); // キー入力

            if (SceneManager.GetActiveScene().name == "Start") { // スタート画面の場合
                if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Level1");
                }
                return;
            } else if (SceneManager.GetActiveScene().name == "Level1") { // Level1の場合のAボタン遷移
                if (selectButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Start");
                }
            }

            //checkGameOver(); // GAMEオーバー確認
            //checkLevelClear(); // レベルクリア確認

            //updateGameInfo();
            //updatePlayerStatus();
        }

        //// FixedUpdate is called just before each physics update.
        //void FixedUpdate() {
        //    updateFpsForFixedUpdate();
        //}

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プライベートメソッド(キャメルケース: 動詞)

        private void checkGameOver() {
            if (playerLifeValue == 0) {
                Time.timeScale = 0f;
                isGameOver = true; // GAMEオーバーフラグON
                message.text = "Game Over!"; // GAMEオーバーメッセージ表示
                home.transform.gameObject.SetActive(true);
                if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Start");
                }
            }
        }

        private void checkLevelClear() { // 全てのアイテムを集めたら一時停止
            if (isLevelClear == false && itemTotalCount > 0 && itemRemainCount == 0) {
                Time.timeScale = 0f;
                isLevelClear = true; // ステージクリアフラグON
                message.text = "Level Clear!"; // クリアメッセージ表示
                home.transform.gameObject.SetActive(true);
                if (startButton.wasPressedThisFrame || aButton.wasPressedThisFrame) {
                    SceneManager.LoadScene("Start");
                }
            }
        }

        private void updatePlayerStatus() { // Player のステイタス表示
            playerLifeUI.value = playerLifeValue;
            playerBombAngleUI.value = playerBombAngleValue;
        }

        private void updateGameInfo() { // GAME の情報を表示
            updateFpForUpdate(); // FPS更新
            information.text = "Item (" + itemRemainCount + "/" + itemTotalCount + ")\r\nFPS1 " + fpsForUpdate + "\r\nFPS2 " + fpsForFixedUpdate; // 残りアイテム数表示
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

        private void getGamepadInput() {
            // 物理ゲームパッド接続判定
            var controllerNames = Input.GetJoystickNames();
            if (controllerNames[0] == "") {
                virtualController.gameObject.SetActive(true);
            } else {
                virtualController.gameObject.SetActive(false);
            }

            // OS判定とゲームパッドのキー参照
            dpadUp = Gamepad.current.dpad.up;
            dpadDown = Gamepad.current.dpad.down;
            dpadLeft = Gamepad.current.dpad.left;
            dpadRight = Gamepad.current.dpad.right;
            startButton = Gamepad.current.startButton;
            selectButton = Gamepad.current.selectButton;
            if (Application.platform == RuntimePlatform.Android) {
                // Android
                aButton = Gamepad.current.aButton;
                bButton = Gamepad.current.bButton;
                xButton = Gamepad.current.xButton;
                yButton = Gamepad.current.yButton;
            } else if (Application.platform == RuntimePlatform.WindowsPlayer) {
                // Windows
                aButton = Gamepad.current.bButton;
                bButton = Gamepad.current.aButton;
                xButton = Gamepad.current.yButton;
                yButton = Gamepad.current.xButton;
            } else {
                // Unityで開発中は取れない？
                aButton = Gamepad.current.bButton;
                bButton = Gamepad.current.aButton;
                xButton = Gamepad.current.yButton;
                yButton = Gamepad.current.xButton;
            }
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
