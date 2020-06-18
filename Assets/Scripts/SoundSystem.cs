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
    /// BGM・効果音関連の処理
    /// @author h.adachi
    /// </summary>
    public class SoundSystem : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // BGM・効果音への参照

        [SerializeField]
        private AudioClip itemClip; // アイテム取得時効果音

        [SerializeField]
        private AudioClip jumpClip; // ジャンプ効果音

        [SerializeField]
        private AudioClip climbClip; // 登る効果音

        [SerializeField]
        private AudioClip walkClip; // 歩く効果音

        [SerializeField]
        private AudioClip runClip; // 走る効果音

        [SerializeField]
        private AudioClip groundedClip; // 着地効果音

        [SerializeField]
        private AudioClip hitClip; // 弾に当たる効果音

        [SerializeField]
        private AudioClip shootClip; // 弾の発射効果音

        [SerializeField]
        private AudioClip explosionClip; // 破壊された効果音

        [SerializeField]
        private AudioClip damageClip; // ダメージを与えた効果音

        [SerializeField]
        private AudioClip knockedupClip; // ブロックを下から叩いた効果音

        [SerializeField]
        private AudioClip waterInClip; // 水中に入る効果音

        [SerializeField]
        private AudioClip waterSinkClip; // 水中で沈む効果音

        [SerializeField]
        private AudioClip waterForwardClip; // 水中で進む効果音

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // フィールド

        private string nowPlayingClipSe1; // 今再生中の効果音1

        private string nowPlayingClipSe2; // 今再生中の効果音2

        private AudioSource se1AudioSource; // 効果音用オーディオソース1

        private AudioSource bgmAudioSource; // BGM用オーディオソース

        private AudioSource se2AudioSource; // 効果音用オーディオソース2

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // パブリックメソッド

        public void PlayItemClip() { // アイテム取得時の効果音を鳴らす
            if (se1AudioSource.isPlaying) {
                se1AudioSource.Stop();
            }
            se1AudioSource.PlayOneShot(itemClip, 2.5f);
            nowPlayingClipSe1 = "itemClip";
        }

        public void PlayJumpClip() {
            if (se1AudioSource.isPlaying) {
                se1AudioSource.Stop();
            }
            se1AudioSource.PlayOneShot(jumpClip);
            nowPlayingClipSe1 = "jumpClip";
        }

        public void PlayGroundedClip() {
            if (se1AudioSource.isPlaying) {
                se1AudioSource.Stop();
            }
            se1AudioSource.PlayOneShot(groundedClip, 0.35f);
            nowPlayingClipSe1 = "groundedClip";
        }

        public void PlayWalkClip() {
            if (nowPlayingClipSe1 != "walkClip" && nowPlayingClipSe1 != "groundedClip" && nowPlayingClipSe1 != "itemClip") {
                se1AudioSource.Stop();
            }
            if (!se1AudioSource.isPlaying) {
                se1AudioSource.clip = walkClip;
                se1AudioSource.Play();
                nowPlayingClipSe1 = "walkClip";
            }
        }

        public void PlayRunClip() {
            if (nowPlayingClipSe1 != "runClip" && nowPlayingClipSe1 != "groundedClip" && nowPlayingClipSe1 != "itemClip") {
                se1AudioSource.Stop();
            }
            if (!se1AudioSource.isPlaying) {
                se1AudioSource.clip = runClip;
                se1AudioSource.Play();
                nowPlayingClipSe1 = "runClip";
            }
        }

        public void PlayClimbClip() {
            if (nowPlayingClipSe1 != "climbClip") {
                se1AudioSource.Stop();
            }
            if (!se1AudioSource.isPlaying) {
                se1AudioSource.clip = climbClip;
                se1AudioSource.Play();
                nowPlayingClipSe1 = "climbClip";
            }
        }

        public void PlayWaterInClip() {
            se2AudioSource.PlayOneShot(waterInClip, 1.5f);
        }

        public void PlayWaterSinkClip() {
            se2AudioSource.PlayOneShot(waterSinkClip,  0.5f);
        }

        public void PlayWaterForwardClip() {
            if (nowPlayingClipSe1 != "waterForwardClip") {
                se1AudioSource.Stop();
            }
            if (!se1AudioSource.isPlaying) {
                se1AudioSource.clip = waterForwardClip;
                se1AudioSource.Play();
                nowPlayingClipSe1 = "waterForwardClip";
            }
        }

        public void PlayShootClip() {
            se2AudioSource.PlayOneShot(shootClip, 2.0f);
        }

        public void PlayHitClip() {
            se2AudioSource.PlayOneShot(hitClip, 3.5f);
        }

        public void PlayExplosionClip() {
            se2AudioSource.PlayOneShot(explosionClip, 3.0f);
        }

        public void PlayDamageClip() {
            se2AudioSource.PlayOneShot(damageClip, 3.0f);
        }

        public void PlayKnockedupClip() {
            se2AudioSource.PlayOneShot(knockedupClip, 3.5f);
        }

        public void StopClip() {
            if (nowPlayingClipSe1 != "groundedClip" && nowPlayingClipSe1 != "itemClip") {
                se1AudioSource.Stop();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Start is called before the first frame update
        void Start() {
            // TODO: 順番に注意
            se1AudioSource = GetComponents<AudioSource>()[0]; // SEオーディオソース設定
            bgmAudioSource = GetComponents<AudioSource>()[1]; // BGMオーディオソース設定
            se2AudioSource = GetComponents<AudioSource>()[2]; // SEオーディオソース設定
        }

        // Update is called once per frame
        void Update() {
        }
    }

}
