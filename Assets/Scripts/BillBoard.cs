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
