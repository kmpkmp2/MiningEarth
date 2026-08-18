using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using DeepEarth.Core;

namespace DeepEarth.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Mixer")]
        [SerializeField] private AudioMixer masterMixer;

        [Header("Database")]
        [SerializeField] private AudioDatabase database;

        private const string ParamMaster = "MasterVolume";
        private const string ParamBGM    = "BGMVolume";
        private const string ParamSFX    = "SFXVolume";
        private const string ParamUI     = "UIVolume";

        private string    _currentBGMId;
        private Coroutine _fadeCoroutine;

        // ── Singleton ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("[Audio]\nDuplicate AudioManager Destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            var s = SaveManager.CurrentData.AudioSettings;
            ApplyMixerVolume(ParamMaster, s.MasterVolume);
            ApplyMixerVolume(ParamBGM,    s.BGMVolume);
            ApplyMixerVolume(ParamSFX,    s.SFXVolume);
            ApplyMixerVolume(ParamUI,     s.SFXVolume);
            Debug.Log("[Audio]\nAudioManager Initialized");
        }

        // ── BGM ──────────────────────────────────────────────────────

        public void PlayBGM(string id)
        {
            if (_currentBGMId == id && bgmSource.isPlaying) return;
            PlayBGMAsync(id).Forget();
        }

        public void StopBGM()
        {
            _currentBGMId = null;
            bgmSource.Stop();
        }

        public void FadeOutBGM(float duration = 1f)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutRoutine(duration));
        }

        public void FadeInBGM(string id, float duration = 1f)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            FadeInBGMAsync(id, duration).Forget();
        }

        public string CurrentBGMId => _currentBGMId;

        private async UniTaskVoid PlayBGMAsync(string id)
        {
            var data = database?.GetClip(id);
            if (data == null) { Debug.LogWarning($"[BGM]\nClip not found: {id}"); return; }

            var clip = await LoadClipAsync(data.AddressableKey);
            if (clip == null) return;

            _currentBGMId    = id;
            bgmSource.clip   = clip;
            bgmSource.loop   = data.Loop;
            bgmSource.volume = data.Volume;
            bgmSource.Play();
            Debug.Log($"[BGM]\nPlay\n{id}");
        }

        private async UniTaskVoid FadeInBGMAsync(string id, float duration)
        {
            var data = database?.GetClip(id);
            if (data == null) return;

            var clip = await LoadClipAsync(data.AddressableKey);
            if (clip == null) return;

            _currentBGMId    = id;
            bgmSource.clip   = clip;
            bgmSource.loop   = data.Loop;
            bgmSource.volume = 0f;
            bgmSource.Play();
            Debug.Log($"[BGM]\nPlay\n{id}");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed          += Time.unscaledDeltaTime;
                bgmSource.volume  = Mathf.Lerp(0f, data.Volume, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            bgmSource.volume = data.Volume;
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            float start   = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed          += Time.unscaledDeltaTime;
                bgmSource.volume  = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }
            bgmSource.volume = 0f;
            StopBGM();
        }

        // ── SFX / UI ─────────────────────────────────────────────────

        public void PlaySFX(string id)
        {
            var data = database?.GetClip(id);
            if (data == null) return;
            PlaySourceAsync(data, sfxSource).Forget();
        }

        public void PlayUISound(string id)
        {
            var data = database?.GetClip(id);
            if (data == null) return;
            PlaySourceAsync(data, uiSource).Forget();
        }

        public void StopAllSFX()
        {
            sfxSource.Stop();
            uiSource.Stop();
        }

        private async UniTaskVoid PlaySourceAsync(AudioClipData data, AudioSource source)
        {
            var clip = await LoadClipAsync(data.AddressableKey);
            if (clip == null) return;
            source.PlayOneShot(clip, data.Volume);
        }

        // ── Volume ───────────────────────────────────────────────────

        public void SetMasterVolume(float linear)
        {
            ApplyMixerVolume(ParamMaster, linear);
            SaveManager.CurrentData.AudioSettings.MasterVolume = linear;
            SaveManager.Save();
            Debug.Log($"[Audio]\nMaster Volume\n{linear:F2}");
        }

        public void SetBGMVolume(float linear)
        {
            ApplyMixerVolume(ParamBGM, linear);
            SaveManager.CurrentData.AudioSettings.BGMVolume = linear;
            SaveManager.Save();
            Debug.Log($"[Audio]\nBGM Volume\n{linear:F2}");
        }

        public void SetSFXVolume(float linear)
        {
            ApplyMixerVolume(ParamSFX, linear);
            ApplyMixerVolume(ParamUI,  linear);
            SaveManager.CurrentData.AudioSettings.SFXVolume = linear;
            SaveManager.Save();
            Debug.Log($"[Audio]\nSFX Volume\n{linear:F2}");
        }

        public void Mute(bool mute)
        {
            if (masterMixer == null) return;
            var s  = SaveManager.CurrentData.AudioSettings;
            float db = mute ? -80f : ToDecibels(s.MasterVolume);
            masterMixer.SetFloat(ParamMaster, db);
            s.IsMuted = mute;
            SaveManager.Save();
        }

        public void UnMute() => Mute(false);

        // ── Helpers ──────────────────────────────────────────────────

        private static async UniTask<AudioClip> LoadClipAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            try { return await ResourceManager.Instance.LoadAssetAsync<AudioClip>(key); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Audio]\nClip load failed: {key}\n{e.Message}");
                return null;
            }
        }

        private void ApplyMixerVolume(string param, float linear)
        {
            // Unity Object에는 "?."(C# 순수 null 체크)가 아니라 "== null"(엔진의 fake-null 판정)을 써야 한다.
            // masterMixer가 Inspector에서 미할당 상태면 "?."는 이를 감지하지 못해 SetFloat 호출이 그대로
            // UnassignedReferenceException을 던진다 — Mute()의 기존 명시적 null 체크와 동일하게 맞춘다.
            if (masterMixer == null) return;
            masterMixer.SetFloat(param, ToDecibels(linear));
        }

        private static float ToDecibels(float linear)
            => Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
    }
}
