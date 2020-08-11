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
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// キーボックスの処理
    /// @author h.adachi
    /// </summary>
    public class KeyBox : MonoBehaviour {
        // Start is called before the first frame update
        void Start() {
            // プレイヤーがキーを持って接触したら
            this.OnCollisionEnterAsObservable()
                .Where(x => x.IsPlayer())
                .Subscribe(x => {
                    foreach (Transform _child in x.gameObject.transform) {
                        if (_child.LikeKey()) {
                            gameObject.GetGameSystem().ClearLevel(); // レベルクリア
                        }
                    }
                });
        }
    }

}