/*
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UniRx;
using UniRx.Triggers;

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

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
                mapGamepad(); // キー入力マッピング
            });
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
