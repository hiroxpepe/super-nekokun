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
    /// 草など Player に触れたら透明にする処理
    /// @author h.adachi
    /// </summary>
    public class Transparent : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        void Start() {

            // Player と接触したら
            this.OnTriggerEnterAsObservable().Where(x => x.IsPlayer())
                .Subscribe(_ => {
                    var _render = gameObject.GetMeshRenderer();
                    var _materialList = _render.materials;
                    foreach (var _material in _materialList) {
                        Utils.SetRenderingMode(_material, RenderingMode.Fade);
                        var _color = _material.color;
                        _color.a = 0.6f; // 透明化実行
                        _material.color = _color;
                    }
                });

            // Player から離脱したら
            this.OnTriggerExitAsObservable().Where(x => x.IsPlayer())
                .Subscribe(_ => {
                    var _render = gameObject.GetMeshRenderer();
                    var _materialList = _render.materials;
                    foreach (var _material in _materialList) {
                        Utils.SetRenderingMode(_material, RenderingMode.Fade);
                        var _color = _material.color;
                        _color.a = 1f; // 透明化解除
                        _material.color = _color;
                    }
                });
        }
    }

}
