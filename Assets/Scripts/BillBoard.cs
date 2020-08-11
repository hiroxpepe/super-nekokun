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
    /// @author h.adachi
    /// </summary>
    public class BillBoard : MonoBehaviour {

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
