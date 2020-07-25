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
        /// whether the Collider's tag is "Player".
        /// </summary>
        public static bool IsPlayer(this Collider self) {
            return self.gameObject.tag.Equals("Player");
        }

        /// <summary>
        /// whether the Collision's tag is "Player".
        /// </summary>
        public static bool IsPlayer(this Collision self) {
            return self.gameObject.tag.Equals("Player");
        }

        /// <summary>
        /// whether the GameObject's tag is "Holdable".
        /// </summary>
        public static bool Holdable(this GameObject self) {
            return self.tag.Equals("Holdable");
        }

        /// <summary>
        /// whether the GameObject's tag is "Getable".
        /// </summary>
        public static bool Getable(this GameObject self) {
            return self.tag.Equals("Getable");
        }

        /// <summary>
        /// whether the GameObject's name contains "Item".
        /// </summary>
        public static bool LikeItem(this GameObject self) {
            return self.name.Contains("Item");
        }

        /// <summary>
        /// whether the Collision's name contains "Item".
        /// </summary>
        public static bool LikeItem(this Collision self) {
            return self.gameObject.name.Contains("Item");
        }

        /// <summary>
        /// whether the Transform's name contains "Item".
        /// </summary>
        public static bool LikeItem(this Transform self) {
            return self.name.Contains("Item");
        }

        /// <summary>
        /// whether the GameObject's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this GameObject self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Transform's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Transform self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Collider's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Collider self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Collision's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Collision self) {
            return self.gameObject.name.Contains("Block");
        }

        /// <summary>
        /// whether the GameObject's name contains "Ground".
        /// </summary>
        public static bool LikeGround(this GameObject self) {
            return self.name.Contains("Ground");
        }

        /// <summary>
        /// whether the Transform's name contains "Ground".
        /// </summary>
        public static bool LikeGround(this Transform self) {
            return self.name.Contains("Ground");
        }

        /// <summary>
        /// whether the GameObject's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this GameObject self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Transform's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Transform self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collider's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Collider self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collision's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Collision self) {
            return self.gameObject.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collision's name contains "EnemyWall".
        /// </summary>
        public static bool LikeEnemyWall(this Collision self) {
            return self.gameObject.name.Contains("EnemyWall");
        }

        /// <summary>
        /// whether the GameObject's name contains "Slope".
        /// </summary>
        public static bool LikeSlope(this GameObject self) {
            return self.name.Contains("Slope");
        }

        /// <summary>
        /// whether the Collision's name contains "Plate".
        /// </summary>
        public static bool LikePlate(this Collision self) {
            return self.gameObject.name.Contains("Plate");
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
        /// whether the GameObject's name contains "Key".
        /// </summary>
        public static bool LikeKey(this GameObject self) {
            return self.name.Contains("Key");
        }

        /// <summary>
        /// whether the GameObject's name contains "_Piece".
        /// </summary>
        public static bool LikePiece(this GameObject self) {
            return self.name.Contains("_Piece");
        }

        /// <summary>
        /// whether the Transform's name contains "Ladder".
        /// </summary>
        public static bool LikeLadder(this Transform self) {
            return self.name.Contains("Ladder");
        }

        /// <summary>
        /// whether the Transform's name contains "Ladder_Body".
        /// </summary>
        public static bool LikeLadderBody(this Transform self) {
            return self.name.Contains("Ladder_Body");
        }

        /// <summary>
        /// whether the Transform's name contains "Stair".
        /// </summary>
        public static bool LikeStair(this Transform self) {
            return self.name.Contains("Stair");
        }

        /// <summary>
        /// whether the Transform's name contains "Down_Point".
        /// </summary>
        public static bool LikeDownPoint(this Transform self) {
            return self.name.Contains("Down_Point");
        }

        /// <summary>
        /// whether the GameObject's name contains "Clone".
        /// </summary>
        public static bool LikeClone(this GameObject self) {
            return self.name.Contains("Clone");
        }

        /// <summary>
        /// whether the GameObject's name contains "Balloon".
        /// </summary>
        public static bool LikeBalloon(this GameObject self) {
            return self.name.Contains("Balloon");
        }

        /// <summary>
        /// whether the GameObject's name contains "Bomb".
        /// </summary>
        public static bool LikeBomb(this GameObject self) {
            return self.name.Contains("Bomb");
        }

        /// <summary>
        /// whether the Collision's name contains "debris".
        /// </summary>
        public static bool LikeDebris(this Collision self) {
            return self.gameObject.name.Contains("debris");
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
        /// get the RectTransform object.
        /// </summary>
        public static RectTransform GetRectTransform(this GameObject self) {
            return self.GetComponent<RectTransform>();
        }

        /// <summary>
        /// get Transform objects.
        /// </summary>
        public static IEnumerable<Transform> GetTransformsInChildren(this GameObject self) {
            return self.GetComponentsInChildren<Transform>();
        }

        /// <summary>
        /// get the Common object.
        /// </summary>
        public static Common GetCommon(this GameObject self) {
            return self.GetComponent<Common>();
        }

        /// <summary>
        /// get the Common object.
        /// </summary>
        public static Common GetCommon(this Transform self) {
            return self.GetComponent<Common>();
        }

        /// <summary>
        /// get the Item object. 
        /// </summary>
        public static Item GetItem(this GameObject self) {
            return self.GetComponent<Item>();
        }

        /// <summary>
        /// get the Block object. 
        /// </summary>
        public static Block GetBlock(this GameObject self) {
            return self.GetComponent<Block>();
        }

        /// <summary>
        /// get the Player object.
        /// </summary>
        public static Player GetPlayer(this GameObject self) {
            return self.GetComponent<Player>();
        }

        /// <summary>
        /// get the Player object.
        /// </summary>
        public static Player GetPlayer(this Transform self) {
            return self.GetComponent<Player>();
        }

        /// <summary>
        /// get GameSystem objects.
        /// </summary>
        public static GameSystem GetGameSystem(this GameObject self) {
            return GameObject.Find("GameSystem").GetComponent<GameSystem>();
        }

        /// <summary>
        /// get SoundSystem objects.
        /// </summary>
        public static SoundSystem GetSoundSystem(this GameObject self) {
            return GameObject.Find("SoundSystem").GetComponent<SoundSystem>();
        }

        /// <summary>
        /// get CameraSystem objects.
        /// </summary>
        public static CameraSystem GetCameraSystem(this GameObject self) {
            return GameObject.Find("CameraSystem").GetComponent<CameraSystem>();
        }

        #endregion

        #region get the GameObject.

        /// <summary>
        /// get the "Player" GameObject.
        /// </summary>
        public static GameObject GetPlayerGameObject(this GameObject self) {
            return GameObject.FindGameObjectWithTag("Player");
        }

        #endregion

    }

}
