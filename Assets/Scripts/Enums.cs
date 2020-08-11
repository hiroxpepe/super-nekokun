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
