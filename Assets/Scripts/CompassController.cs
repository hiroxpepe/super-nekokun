﻿/*
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

/// <summary>
/// コンパスの処理
/// @author h.adachi
/// </summary>
public class CompassController : MonoBehaviour {

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

    [SerializeField]
    Transform player;

    [SerializeField]
    GameObject needle;

    ///////////////////////////////////////////////////////////////////////////
    // update Methods

    void LateUpdate() {
        Quaternion _q = player.transform.rotation; // プレーヤーの y 軸を コンパスの z軸に設定する
        needle.transform.rotation = Quaternion.Euler(0f, 0f, -_q.eulerAngles.y);
    }

}
