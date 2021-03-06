﻿/*
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
    /// BGM・効果音関連の処理
    /// @author h.adachi
    /// </summary>
    public class SoundSystem : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // BGM・効果音への参照

        [SerializeField]
        AudioClip itemClip; // アイテム取得時効果音

        [SerializeField]
        AudioClip jumpClip; // ジャンプ効果音

        [SerializeField]
        AudioClip climbClip; // 登る効果音

        [SerializeField]
        AudioClip walkClip; // 歩く効果音

        [SerializeField]
        AudioClip runClip; // 走る効果音

        [SerializeField]
        AudioClip groundedClip; // 着地効果音

        [SerializeField]
        AudioClip hitClip; // 弾に当たる効果音

        [SerializeField]
        AudioClip shootClip; // 弾の発射効果音

        [SerializeField]
        AudioClip explosionClip; // 破壊された効果音

        [SerializeField]
        AudioClip damageClip; // ダメージを与えた効果音

        [SerializeField]
        AudioClip knockedupClip; // ブロックを下から叩いた効果音

        [SerializeField]
        AudioClip waterInClip; // 水中に入る効果音

        [SerializeField]
        AudioClip waterSinkClip; // 水中で沈む効果音

        [SerializeField]
        AudioClip waterForwardClip; // 水中で進む効果音

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        string nowPlayingClipSe1; // 今再生中の効果音1

        string nowPlayingClipSe2; // 今再生中の効果音2

        AudioSource se1AudioSource; // 効果音用オーディオソース1

        AudioSource bgmAudioSource; // BGM用オーディオソース

        AudioSource se2AudioSource; // 効果音用オーディオソース2

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

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
        // update Methods

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
