using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// 草など Player に触れたら透明にする処理
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

        private void OnTriggerStay(Collider other) {
            // Player と接触したら
            if (other.tag == "Player") {
                var _render = GetComponent<MeshRenderer>();
                var _materialList = _render.materials;
                foreach (var _material in _materialList) {
                    Util.SetRenderingMode(_material, RenderingMode.Fade);
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
                    Util.SetRenderingMode(_material, RenderingMode.Fade);
                    var _color = _material.color;
                    _color.a = 1f; // 透明化解除
                    _material.color = _color;
                }
            }
        }
    }

}
