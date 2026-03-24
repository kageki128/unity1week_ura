using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class AudioPlayer : MonoBehaviour
    {
        public static AudioPlayer Current { get; private set; }

        [Serializable]
        class BGMClipBinding
        {
            public BGMType bgmType;
            public AudioClip clip;
            public bool isLoop = true;
        }

        [Serializable]
        class SEClipBinding
        {
            public SEType seType;
            public AudioClip clip;
        }

        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource seSource;
        [SerializeField, Min(0f)] float bgmFadeOutDuration = 0.25f;
        [SerializeField, Min(0f)] float sePlayCooldownSeconds = 0.05f;
        [SerializeField] List<BGMClipBinding> bgmClips = new();
        [SerializeField] List<SEClipBinding> seClips = new();

        readonly Dictionary<BGMType, BGMClipBinding> bgmClipMap = new();
        readonly Dictionary<SEType, AudioClip> seClipMap = new();
        readonly Dictionary<SEType, float> seLastPlayTimeMap = new();
        Tween bgmFadeTween;
        float bgmBaseVolume = 1f;
        bool isBGMPaused;

        void Awake()
        {
            if (Current != null && !ReferenceEquals(Current, this))
            {
                Debug.LogWarning("AudioPlayer is duplicated. Latest instance is used.", this);
            }

            Current = this;
            EnsureAudioSources();
            bgmBaseVolume = bgmSource.volume;
            RebuildClipMap();
        }

        public void PlayBGM(BGMType bgmType)
        {
            if (!TryGetBGMClipBinding(bgmType, out var binding))
            {
                return;
            }

            if (bgmSource == null)
            {
                return;
            }

            if (bgmSource.isPlaying || bgmFadeTween != null)
            {
                StopBGM(true);
            }

            bgmSource.volume = bgmBaseVolume;
            bgmSource.clip = binding.clip;
            bgmSource.loop = binding.isLoop;
            isBGMPaused = false;
            bgmSource.Play();
        }

        public void PauseBGM()
        {
            if (bgmSource == null || !bgmSource.isPlaying)
            {
                return;
            }

            KillBGMFadeTween();
            bgmSource.Pause();
            isBGMPaused = true;
        }

        public void ResumeBGM()
        {
            if (bgmSource == null || !isBGMPaused)
            {
                return;
            }

            if (bgmSource.clip == null)
            {
                isBGMPaused = false;
                return;
            }

            bgmSource.UnPause();
            isBGMPaused = false;
        }

        public void StopBGM(bool isForceStop = false)
        {
            if (bgmSource == null)
            {
                return;
            }

            if (isForceStop || !bgmSource.isPlaying || bgmFadeOutDuration <= 0f)
            {
                StopBGMImmediately(killFadeTween: true);
                return;
            }

            KillBGMFadeTween();
            var startVolume = bgmSource.volume;
            if (startVolume <= 0f)
            {
                StopBGMImmediately(killFadeTween: false);
                return;
            }

            bgmFadeTween = DOVirtual.Float(startVolume, 0f, bgmFadeOutDuration, volume =>
                {
                    if (bgmSource != null)
                    {
                        bgmSource.volume = volume;
                    }
                })
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    StopBGMImmediately(killFadeTween: false);
                    bgmFadeTween = null;
                })
                .OnKill(() =>
                {
                    bgmFadeTween = null;
                });
        }

        public void PlaySE(SEType seType)
        {
            if (seSource == null)
            {
                return;
            }

            var currentTime = Time.unscaledTime;
            if (seLastPlayTimeMap.TryGetValue(seType, out var lastPlayTime) &&
                currentTime - lastPlayTime < sePlayCooldownSeconds)
            {
                return;
            }

            if (!seClipMap.TryGetValue(seType, out var clip) || clip == null)
            {
                Debug.LogWarning($"SE clip is not registered. Type: {seType}", this);
                return;
            }

            seLastPlayTimeMap[seType] = currentTime;
            seSource.PlayOneShot(clip);
        }

        public void StopSE()
        {
            if (seSource == null)
            {
                return;
            }

            seSource.Stop();
        }

        public void RefreshClipMap()
        {
            RebuildClipMap();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }

            KillBGMFadeTween();
        }

        void OnValidate()
        {
            RebuildClipMap();
        }

        bool TryGetBGMClipBinding(BGMType bgmType, out BGMClipBinding binding)
        {
            if (!bgmClipMap.TryGetValue(bgmType, out binding) || binding.clip == null)
            {
                Debug.LogWarning($"BGM clip is not registered. Type: {bgmType}", this);
                return false;
            }

            return true;
        }

        void RebuildClipMap()
        {
            bgmClipMap.Clear();
            seClipMap.Clear();

            for (var i = 0; i < bgmClips.Count; i++)
            {
                var binding = bgmClips[i];
                if (binding == null || binding.clip == null)
                {
                    continue;
                }

                if (bgmClipMap.ContainsKey(binding.bgmType))
                {
                    Debug.LogWarning($"Duplicate BGM type found. Last one is used. Type: {binding.bgmType}", this);
                }

                bgmClipMap[binding.bgmType] = binding;
            }

            for (var i = 0; i < seClips.Count; i++)
            {
                var binding = seClips[i];
                if (binding == null || binding.clip == null)
                {
                    continue;
                }

                if (seClipMap.ContainsKey(binding.seType))
                {
                    Debug.LogWarning($"Duplicate SE type found. Last one is used. Type: {binding.seType}", this);
                }

                seClipMap[binding.seType] = binding.clip;
            }
        }

        void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            if (seSource == null || ReferenceEquals(seSource, bgmSource))
            {
                seSource = gameObject.AddComponent<AudioSource>();
            }

            bgmSource.playOnAwake = false;
            seSource.playOnAwake = false;
            seSource.loop = false;
        }

        void StopBGMImmediately(bool killFadeTween)
        {
            if (bgmSource == null)
            {
                return;
            }

            if (killFadeTween)
            {
                KillBGMFadeTween();
            }

            bgmSource.Stop();
            bgmSource.clip = null;
            bgmSource.volume = bgmBaseVolume;
            isBGMPaused = false;
        }

        void KillBGMFadeTween()
        {
            if (bgmFadeTween == null)
            {
                return;
            }

            if (bgmFadeTween.IsActive())
            {
                bgmFadeTween.Kill();
            }

            bgmFadeTween = null;
        }
    }
}
