using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainWindController : MonoBehaviour
{
    public GameObject playerObject;

    [Header("Particle System")]
    private ParticleSystem rainParticleSystem;

    [Header("Rain Settings")]
    private const float maxEmissionRate = 1024f;
    private const float transitionDuration = 10f;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;

    private ParticleSystem.EmissionModule emissionModule;
    private Coroutine currentTransitionCoroutine;
    private System.Random rng;
    private AudioSource audioSource;

    private void Awake()
    {
        rng = new System.Random();
        if (rainParticleSystem == null)
        {
            rainParticleSystem = GetComponent<ParticleSystem>();
        }
        emissionModule = rainParticleSystem.emission;
        emissionModule.rateOverTime = 0f;
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        signalCenter.Subscribe(SignalType.ChangeGameSettings, OnChangeGameSettings);
        signalCenter.Subscribe(SignalType.WorldLoaded, OnWorldLoaded);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = AudioManager.Instance.rain;
    }

    public void OnChangeGameSettings(GameObject sender, object data)
    {
        if (GameStateManager.Get() != GameState.LoadingCompleted)
        {
            return;
        }
        if ((bool)Alpha.gameSettings.Categories["World"]["Weather"].CurrentValue == false ||
            (bool)Alpha.gameSettings.Categories["World"]["Rain"].CurrentValue == false)
        {
            StartCoroutine(TransitionEmissionRate(0, 0f));
        }
        if (currentTransitionCoroutine != null)
        {
            StopCoroutine(currentTransitionCoroutine);
        }
        if ((bool)Alpha.gameSettings.Categories["World"]["Weather"].CurrentValue == true &&
            (bool)Alpha.gameSettings.Categories["World"]["Rain"].CurrentValue == true)
        {
            OnWorldLoaded();
        }
    }

    public void OnWorldLoaded(GameObject sender = null, object data = null)
    {
        DOVirtual.DelayedCall(rng.Next(1, 30), () => StartRain());
    }

    public void StartRain()
    {
        if ((bool)Alpha.gameSettings.Categories["World"]["Weather"].CurrentValue == false ||
            (bool)Alpha.gameSettings.Categories["World"]["Rain"].CurrentValue == false)
        {
            return;
        }
        if (currentTransitionCoroutine != null)
        {
            StopCoroutine(currentTransitionCoroutine);
        }
        audioSource.volume = 0f;
        audioSource.Play();
        currentTransitionCoroutine = StartCoroutine(TransitionEmissionRate(maxEmissionRate, transitionDuration));
        DOVirtual.DelayedCall(rng.Next(30 + (int)transitionDuration, 120), () => StopRain());
    }

    public void StopRain()
    {
        if ((bool)Alpha.gameSettings.Categories["World"]["Weather"].CurrentValue == false ||
            (bool)Alpha.gameSettings.Categories["World"]["Rain"].CurrentValue == false)
        {
            return;
        }
        if (currentTransitionCoroutine != null)
        {
            StopCoroutine(currentTransitionCoroutine);
        }
        currentTransitionCoroutine = StartCoroutine(TransitionEmissionRate(0f, transitionDuration));
        DOVirtual.DelayedCall(rng.Next(30 + (int)transitionDuration, 120), () => StopRain());
    }

    private IEnumerator TransitionEmissionRate(float targetRate, float duration)
    {
        float startRate = emissionModule.rateOverTime.constant;
        if (duration <= 0.01f)
        {
            emissionModule.rateOverTime = targetRate;
            yield break;
        }
        float elapsed = 0f;
        float initialVolume = audioSource.volume;
        float targetVolume = maxEmissionRate / targetRate;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float current = Mathf.Lerp(startRate, targetRate, t);
            emissionModule.rateOverTime = current;
            audioSource.volume = Mathf.Lerp(initialVolume, targetVolume, t);
            yield return null;
        }
        emissionModule.rateOverTime = targetRate;
    }

    private void LateUpdate()
    {
        transform.position = playerObject.transform.position + new Vector3(0, 200, 0);
    }

    //private ParticleSystem ps;
    //// 使用 List 替代数组，不会产生 GC 垃圾
    //private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    //void Start()
    //{
    //    ps = GetComponent<ParticleSystem>();
    //}

    //void OnParticleCollision(GameObject other)
    //{
    //    // 使用接受 List 的重载版本
    //    int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

    //    // 获取粒子系统中所有粒子的数据
    //    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
    //    int numParticles = ps.GetParticles(particles);

    //    // 遍历碰撞事件，将对应粒子销毁
    //    for (int i = 0; i < numCollisionEvents; i++)
    //    {
    //        int particleIndex = collisionEvents[i].particleIndex;
    //        if (particleIndex < numParticles)
    //        {
    //            particles[particleIndex].remainingLifetime = 0f;
    //        }
    //    }

    //    // 将修改后的粒子数据写回粒子系统
    //    ps.SetParticles(particles, numParticles);
    //}
}