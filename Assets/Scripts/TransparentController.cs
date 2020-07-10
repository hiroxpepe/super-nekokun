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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// 草など Player に触れたら透明にする処理
    /// @author h.adachi
    /// </summary>
    public class TransparentController : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Start is called before the first frame update.
        void Start() {
        }

        // Update is called once per frame.
        void Update() {
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // イベントハンドラ

        void OnTriggerStay(Collider other) {
            // Player と接触したら
            if (other.tag == "Player") {
                var _render = GetComponent<MeshRenderer>();
                var _materialList = _render.materials;
                foreach (var _material in _materialList) {
                    Utils.SetRenderingMode(_material, RenderingMode.Fade);
                    var _color = _material.color;
                    _color.a = 0.6f; // 透明化実行
                    _material.color = _color;
                }
            }
        }

        void OnTriggerExit(Collider other) {
            // Player から離脱したら
            if (other.tag == "Player") {
                var _render = GetComponent<MeshRenderer>();
                var _materialList = _render.materials;
                foreach (var _material in _materialList) {
                    Utils.SetRenderingMode(_material, RenderingMode.Fade);
                    var _color = _material.color;
                    _color.a = 1f; // 透明化解除
                    _material.color = _color;
                }
            }
        }
    }

}
