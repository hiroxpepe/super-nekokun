using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// 汎用列挙体
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
