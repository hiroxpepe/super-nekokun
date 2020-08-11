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

/// <summary>
/// コンパスの処理
/// @author h.adachi
/// </summary>
public class Compass : MonoBehaviour {

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

    [SerializeField]
    GameObject playerObject;

    [SerializeField]
    GameObject needleObject;

    ///////////////////////////////////////////////////////////////////////////
    // update Methods

    void LateUpdate() {
        Quaternion _q = playerObject.transform.rotation; // プレーヤーの y 軸を コンパスの z軸に設定する
        needleObject.transform.rotation = Quaternion.Euler(0f, 0f, -_q.eulerAngles.y);
    }

}
