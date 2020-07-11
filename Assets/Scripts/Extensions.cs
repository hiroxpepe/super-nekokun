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
    /// 汎用拡張メソッドクラス
    /// @author h.adachi
    /// </summary>
    public static class Extensions {

        public static bool LikePlayer(this GameObject self) {
            return self.name.Contains("Player");
        }

        public static bool IsPlayer(this GameObject self) {
            return self.tag.Equals("Player");
        }

        public static bool LikeItem(this GameObject self) {
            return self.name.Contains("Item");
        }

        public static bool IsItem(this GameObject self) {
            return self.tag.Equals("Item"); // TODO: "Getable"
        }

        public static bool LikeBlock(this GameObject self) {
            return self.name.Contains("Block");
        }

        public static bool LikeGround(this GameObject self) {
            return self.name.Contains("Ground");
        }

        public static bool LikeWall(this GameObject self) {
            return self.name.Contains("Wall");
        }

        public static bool LikeSlope(this GameObject self) {
            return self.name.Contains("Slope");
        }

        public static bool LikeWater(this GameObject self) {
            return self.name.Contains("Water");
        }

        public static bool LikeBullet(this GameObject self) {
            return self.name.Contains("Bullet");
        }

        public static bool Holdable(this GameObject self) {
            return self.tag.Equals("Holdable");
        }

    }

}
