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

namespace StudioMeowToon {
    /// <summary>
    /// 汎用列挙体
    /// @author h.adachi
    /// </summary>
    
    #region PushedDirection

    /// <summary>
    /// 押された方向を表す列挙体。
    /// </summary>
    public enum PushedDirection {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX,
        None
    };

    #endregion

    #region Direction

    /// <summary>
    /// 方向を表す列挙体。
    /// </summary>
    public enum Direction {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX,
        None
    };

    #endregion

    #region HitsType

    /// <summary>
    /// 当たった相手を表す列挙体。
    /// </summary>
    public enum HitsType {
        Bullet,
        Player,
        Item,
        Block,
        Other
    };

    #endregion

    #region RenderingMode

    /// <summary>
    /// マテリアルのレンダリングモードを表す列挙体。
    /// </summary>
    public enum RenderingMode {
        Opaque,
        Cutout,
        Fade,
        Transparent,
    }

    #endregion

}
