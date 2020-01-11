using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    public class BillBoardController : MonoBehaviour {

        // Awake is called when the script instance is being loaded.
        protected void Awake() {
        }

        // Start is called before the first frame update.
        void Start() {

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(_ => {
                });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable()
                .Subscribe(_ => {
                });
        }

        // Update is called once per frame.
        void Update() {
            Vector3 _cameraPosition = Camera.main.transform.position;
            _cameraPosition.y = transform.position.y;
            transform.LookAt(_cameraPosition);
        }

        // FixedUpdate is called just before each physics update.
        void FixedUpdate() {
        }

        // LateUpdate is called after all Update functions have been called.
        void LateUpdate() {
        }
    }

}
