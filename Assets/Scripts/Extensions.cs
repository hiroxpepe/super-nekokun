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

using System.Collections.Generic;
using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// 汎用拡張メソッドクラス
    /// @author h.adachi
    /// </summary>
    public static class Extensions {

        #region type of object.

        /// <summary>
        /// whether the GameObject's name contains "Player".
        /// </summary>
        public static bool LikePlayer(this GameObject self) {
            return self.name.Contains("Player");
        }

        /// <summary>
        /// whether the GameObject's tag is "Player".
        /// </summary>
        public static bool IsPlayer(this GameObject self) {
            return self.tag.Equals("Player");
        }

        /// <summary>
        /// whether the Transform's tag is "Player".
        /// </summary>
        public static bool IsPlayer(this Transform self) {
            return self.tag.Equals("Player");
        }

        /// <summary>
        /// whether the GameObject's name contains "Item".
        /// </summary>
        public static bool LikeItem(this GameObject self) {
            return self.name.Contains("Item");
        }

        /// <summary>
        /// whether the GameObject's tag is "Getable".
        /// </summary>
        public static bool Getable(this GameObject self) {
            return self.tag.Equals("Getable"); // TODO: "Getable"
        }

        /// <summary>
        /// whether the GameObject's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this GameObject self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Collider's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Collider self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the GameObject's name contains "Ground".
        /// </summary>
        public static bool LikeGround(this GameObject self) {
            return self.name.Contains("Ground");
        }

        /// <summary>
        /// whether the GameObject's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this GameObject self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collider's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Collider self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the GameObject's name contains "Slope".
        /// </summary>
        public static bool LikeSlope(this GameObject self) {
            return self.name.Contains("Slope");
        }

        /// <summary>
        /// whether the GameObject's name contains "Water".
        /// </summary>
        public static bool LikeWater(this GameObject self) {
            return self.name.Contains("Water");
        }

        /// <summary>
        /// whether the GameObject's name contains "Bullet".
        /// </summary>
        public static bool LikeBullet(this GameObject self) {
            return self.name.Contains("Bullet");
        }

        /// <summary>
        /// whether the Transform's name contains "Bullet".
        /// </summary>
        public static bool LikeBullet(this Transform self) {
            return self.name.Contains("Bullet");
        }

        /// <summary>
        /// whether the GameObject's name contains "_Piece".
        /// </summary>
        public static bool LikePiece(this GameObject self) {
            return self.name.Contains("_Piece");
        }

        /// <summary>
        /// whether the Transform's name contains "Ladder_Body".
        /// </summary>
        public static bool LikeLadderBody(this Transform self) {
            return self.name.Contains("Ladder_Body");
        }

        /// <summary>
        /// whether the GameObject's tag is "Holdable".
        /// </summary>
        public static bool Holdable(this GameObject self) {
            return self.tag.Equals("Holdable");
        }

        #endregion

        #region get the object.

        /// <summary>
        /// get the BoxCollider object.
        /// </summary>
        public static BoxCollider GetBoxCollider(this GameObject self) {
            return self.GetComponent<BoxCollider>();
        }

        /// <summary>
        /// get the Collider object.
        /// </summary>
        public static Collider GetCollider(this GameObject self) {
            return self.GetComponent<Collider>();
        }

        /// <summary>
        /// get the Rigidbody object.
        /// </summary>
        public static Rigidbody GetRigidbody(this GameObject self) {
            return self.GetComponent<Rigidbody>();
        }

        /// <summary>
        /// get the Rigidbody object.
        /// </summary>
        public static Rigidbody GetRigidbody(this Transform self) {
            return self.GetComponent<Rigidbody>();
        }

        /// <summary>
        /// add a Rigidbody object.
        /// </summary>
        public static Rigidbody AddRigidbody(this GameObject self) {
            return self.AddComponent<Rigidbody>();
        }

        /// <summary>
        /// add a Rigidbody object.
        /// </summary>
        public static Rigidbody AddRigidbody(this Transform self) {
            return self.gameObject.AddComponent<Rigidbody>();
        }

        /// <summary>
        /// get the Renderer object.
        /// </summary>
        public static Renderer GetRenderer(this GameObject self) {
            return self.GetComponent<Renderer>();
        }

        /// <summary>
        /// get the MeshRenderer object.
        /// </summary>
        public static MeshRenderer GetMeshRenderer(this GameObject self) {
            return self.GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// get the CommonController object.
        /// </summary>
        public static CommonController GetCommonController(this GameObject self) {
            return self.GetComponent<CommonController>();
        }

        /// <summary>
        /// get the CommonController object.
        /// </summary>
        public static CommonController GetCommonController(this Transform self) {
            return self.GetComponent<CommonController>();
        }

        /// <summary>
        /// get the ItemController object. 
        /// </summary>
        public static ItemController GetItemController(this GameObject self) {
            return self.GetComponent<ItemController>();
        }

        /// <summary>
        /// get the BlockController object. 
        /// </summary>
        public static BlockController GetBlockController(this GameObject self) {
            return self.GetComponent<BlockController>();
        }

        /// <summary>
        /// get the PlayerController object.
        /// </summary>
        public static PlayerController GetPlayerController(this Transform self) {
            return self.GetComponent<PlayerController>();
        }

        /// <summary>
        /// get Transform objects.
        /// </summary>
        public static IEnumerable<Transform> GetTransformsInChildren(this GameObject self) {
            return self.GetComponentsInChildren<Transform>();
        }

        #endregion

    }

}
