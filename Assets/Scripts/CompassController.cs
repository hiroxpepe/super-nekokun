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

public class CompassController : MonoBehaviour {
    [SerializeField]
    Transform player;
    [SerializeField]
    Texture compBg;
    [SerializeField]
    Texture blipTex;
    void OnGUI() {
        //GUI.DrawTexture(new Rect(0, 0, 120, 120), compBg);
        //GUI.DrawTexture(new Rect(233, 110, 160, 160), compBg);
        GUI.DrawTexture(CreateBlip(), blipTex);
    }

    Rect CreateBlip() {
        float angDeg = player.eulerAngles.y - 90;
        float angRed = angDeg * Mathf.Deg2Rad;

        //float blipX = 25 * Mathf.Cos(angRed);
        //float blipY = 25 * Mathf.Sin(angRed);
        float blipX = 65 * Mathf.Cos(angRed);
        float blipY = 65 * Mathf.Sin(angRed);

        //blipX += 55;
        //blipY += 55;
        blipX += 75 + 233;
        blipY += 75 + 110;

        return new Rect(blipX, blipY, 10, 10);
    }

}
