using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioMeowToon {
    public class CameraFilter : MonoBehaviour {

        [SerializeField] Material material;

        // Start is called before the first frame update
        void Start() {
        }

        // Update is called once per frame
        void Update() {
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest) {
            Graphics.Blit(src, dest, material);
        }
    }

}
