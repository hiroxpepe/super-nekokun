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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace StudioMeowToon {
    /// <summary>
    /// ゲームパッドのマッピング処理
    /// @author h.adachi
    /// </summary>
    public class GamepadMaper : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        protected GameObject virtualController; // バーチャルコントロール // TODO ⇒ static ?

        protected ButtonControl aButton; // Aボタン:任天堂配置

        protected ButtonControl bButton; // Bボタン:任天堂配置

        protected ButtonControl xButton; // Xボタン:任天堂配置

        protected ButtonControl yButton; // Yボタン:任天堂配置

        protected ButtonControl upButton; // 十字キー上ボタン

        protected ButtonControl downButton; // 十字キー下ボタン

        protected ButtonControl leftButton; // 十字キー左ボタン

        protected ButtonControl rightButton; // 十字キー右ボタン

        protected ButtonControl l1Button; // L1ボタン

        protected ButtonControl r1Button; // R1ボタン

        protected ButtonControl l2Button; // L2ボタン

        protected ButtonControl r2Button; // R2ボタン

        protected ButtonControl startButton; // スタートボタン

        protected ButtonControl selectButton; // セレクトボタン

        protected ButtonControl rStickUpButton; // Rスティック上入力

        protected ButtonControl rStickDownButton; // Rスティック下入力

        bool _useVirtualController;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        public bool useVirtualController { get => _useVirtualController; } // バーチャルコントローラー使用かどうか

        ///////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        protected void Start() {
            virtualController = GameObject.Find("VirtualController"); // バーチャルコントローラー参照取得
        }

        // Update is called once per frame.
        protected void Update() {
            mapGamepad(); // キー入力マッピング
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        void mapGamepad() {
            // 物理ゲームパッド接続判定
            var controllerNames = Input.GetJoystickNames();
            if (controllerNames.Length == 0 || controllerNames[0] == "") {
                virtualController.SetActive(true);
                _useVirtualController = true;
            } else {
                virtualController.SetActive(false);
                _useVirtualController = false;
            }

            // OS判定とゲームパッドのキー参照
            upButton = Gamepad.current.dpad.up;
            downButton = Gamepad.current.dpad.down;
            leftButton = Gamepad.current.dpad.left;
            rightButton = Gamepad.current.dpad.right;
            l1Button = Gamepad.current.leftShoulder;
            r1Button = Gamepad.current.rightShoulder;
            l2Button = Gamepad.current.leftTrigger;
            r2Button = Gamepad.current.rightTrigger;
            startButton = Gamepad.current.startButton;
            selectButton = Gamepad.current.selectButton;
            rStickUpButton = Gamepad.current.rightStick.up;
            rStickDownButton = _useVirtualController ? Gamepad.current.leftStick.up : Gamepad.current.rightStick.down; // InputSystem のバグ？
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
