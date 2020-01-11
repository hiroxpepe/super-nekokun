using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioMeowToon {
    /// <summary>
    /// BGM・効果音関連の処理
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

        private string nowPlayingClip; // 今再生中の効果音

        private AudioSource seAudioSource; // 効果音用オーディオソース

        private AudioSource bgmAudioSource; // BGM用オーディオソース

        private AudioSource se2AudioSource; // 効果音用オーディオソース2

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // パブリックメソッド

        public void PlayItemClip() { // アイテム取得時の効果音を鳴らす
            if (seAudioSource.isPlaying) {
                seAudioSource.Stop();
            }
            seAudioSource.PlayOneShot(itemClip, 2.5f);
            nowPlayingClip = "itemClip";
        }

        public void PlayJumpClip() {
            if (seAudioSource.isPlaying) {
                seAudioSource.Stop();
            }
            seAudioSource.PlayOneShot(jumpClip);
            nowPlayingClip = "jumpClip";
        }

        public void PlayGroundedClip() {
            if (seAudioSource.isPlaying) {
                seAudioSource.Stop();
            }
            seAudioSource.PlayOneShot(groundedClip, 0.35f);
            nowPlayingClip = "groundedClip";
        }

        public void PlayWalkClip() {
            if (nowPlayingClip != "walkClip" && nowPlayingClip != "groundedClip" && nowPlayingClip != "itemClip") {
                seAudioSource.Stop();
            }
            if (!seAudioSource.isPlaying) {
                seAudioSource.clip = walkClip;
                seAudioSource.Play();
                nowPlayingClip = "walkClip";
            }
        }

        public void PlayRunClip() {
            if (nowPlayingClip != "runClip" && nowPlayingClip != "groundedClip" && nowPlayingClip != "itemClip") {
                seAudioSource.Stop();
            }
            if (!seAudioSource.isPlaying) {
                seAudioSource.clip = runClip;
                seAudioSource.Play();
                nowPlayingClip = "runClip";
            }
        }

        public void PlayClimbClip() {
            if (nowPlayingClip != "climbClip") {
                seAudioSource.Stop();
            }
            if (!seAudioSource.isPlaying) {
                seAudioSource.clip = climbClip;
                seAudioSource.Play();
                nowPlayingClip = "climbClip";
            }
        }

        public void PlayWaterInClip() {
            se2AudioSource.PlayOneShot(waterInClip, 1.5f);
        }

        public void PlayWaterSinkClip() {
            se2AudioSource.PlayOneShot(waterSinkClip,  0.5f);
        }

        public void PlayWaterForwardClip() {
            if (nowPlayingClip != "waterForwardClip") {
                seAudioSource.Stop();
            }
            if (!seAudioSource.isPlaying) {
                seAudioSource.clip = waterForwardClip;
                seAudioSource.Play();
                nowPlayingClip = "waterForwardClip";
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
            if (nowPlayingClip != "groundedClip" && nowPlayingClip != "itemClip") {
                seAudioSource.Stop();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 更新メソッド

        // Start is called before the first frame update
        void Start() {
            // TODO: 順番に注意
            seAudioSource = GetComponents<AudioSource>()[0]; // SEオーディオソース設定
            bgmAudioSource = GetComponents<AudioSource>()[1]; // BGMオーディオソース設定
            se2AudioSource = GetComponents<AudioSource>()[2]; // SEオーディオソース設定
        }

        // Update is called once per frame
        void Update() {
        }
    }

}
