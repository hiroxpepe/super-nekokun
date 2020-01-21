using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// キーボックスの処理
    /// </summary>
    public class KeyBoxController : MonoBehaviour {
        // Start is called before the first frame update
        void Start() {
            // プレイヤーがキーを持って接触したら
            this.OnCollisionEnterAsObservable()
                .Where(t => t.gameObject.name.Equals("Player"))
                .Subscribe(t => {
                    foreach (Transform _child in t.gameObject.transform) {
                        if (_child.name.Contains("Key")) {
                            var _gameSystem = GameObject.Find("GameSystem"); // レベルクリア
                            _gameSystem.GetComponent<GameSystem>().ClearLevel();
                        }
                    }
                });
        }
    }

}