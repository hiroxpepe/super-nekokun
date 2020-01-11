using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace StudioMeowToon {
    /// <summary>
    /// ゲームパッドのマッピング処理
    /// </summary>
    public class GamepadMaper : MonoBehaviour {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        protected GameObject virtualController; // バーチャルコントロール

        protected ButtonControl aButton; // Aボタン:任天堂配置

        protected ButtonControl bButton; // Bボタン:任天堂配置

        protected ButtonControl xButton; // Xボタン:任天堂配置

        protected ButtonControl yButton; // Yボタン:任天堂配置

        protected ButtonControl dpadUp; // 十字キー上ボタン

        protected ButtonControl dpadDown; // 十字キー上ボタン

        protected ButtonControl dpadLeft; // 十字キー上ボタン

        protected ButtonControl dpadRight; // 十字キー上ボタン

        protected ButtonControl l1Button; // L1ボタン

        protected ButtonControl r1Button; // R1ボタン

        protected ButtonControl startButton; // スタートボタン

        protected ButtonControl selectButton; // セレクトボタン

        // Start is called before the first frame update.
        protected void Start() {
            virtualController = GameObject.Find("VirtualController"); // バーチャルコントローラー参照取得
        }

        // Update is called once per frame.
        protected void Update() {
            mapGamepad(); // キー入力マッピング
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // プライベートメソッド(キャメルケース: 動詞)

        private void mapGamepad() {
            // 物理ゲームパッド接続判定
            var controllerNames = Input.GetJoystickNames();
            if (controllerNames.Length == 0 || controllerNames[0] == "") {
                virtualController.SetActive(true);
            } else {
                virtualController.SetActive(false);
            }

            // OS判定とゲームパッドのキー参照
            dpadUp = Gamepad.current.dpad.up;
            dpadDown = Gamepad.current.dpad.down;
            dpadLeft = Gamepad.current.dpad.left;
            dpadRight = Gamepad.current.dpad.right;
            l1Button = Gamepad.current.leftShoulder;
            r1Button = Gamepad.current.rightShoulder;
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
    }

}
