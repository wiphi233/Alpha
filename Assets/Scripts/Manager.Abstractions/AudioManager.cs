using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum AudioType
//{
//    BGM,
//    SFX,
//    UI,
//    AMBIENT,
//    SONG
//}

public class AudioManager : MonoBehaviour
{
    [Header("Audio 音效资源")]
    public AudioClip ButtonClick;
    public AudioClip flippingPages;
    public AudioClip wave;
    public AudioClip dive;
    public AudioClip wind;
    public AudioClip treeWind;
    public AudioClip rain;

    [Header("Blackground Music 背景音")]
    //public AudioClip Antler;
    //public AudioClip InfinityHeaven;
    public AudioClip Relax;
    //public AudioClip adofi;

    [Header("Global Audio Settings 全局音频设置")]
    public bool AudioListenerPause = false;
    [Range(0f, 1f)]
    public float AudioListenerVolume = 1f;

    [HideInInspector] public static AudioManager Instance;
    [HideInInspector] public static AudioSource bgmSource;      // 背景音乐
    //[HideInInspector] public static List<AudioSource> sfxSource = new List<AudioSource>();      // 音效
    //[HideInInspector] public static List<AudioSource> uiSource = new List<AudioSource>();       // UI音效
    //[HideInInspector] public static List<AudioSource> ambientSource = new List<AudioSource>();  // 环境音
    [HideInInspector] public static AudioSource songSource;     // 歌曲

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("重复的AudioManager实例");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AudioListener.volume = AudioListenerVolume;
        AudioListener.pause = AudioListenerPause;
        bgmSource = gameObject.AddComponent<AudioSource>();
        songSource = gameObject.AddComponent<AudioSource>();
        bgmSource.clip = Relax;
        bgmSource.loop = true;
        
//#if UNITY_EDITOR
//        AudioDebug();
//#endif
    }

    public AudioSource PlayAudio(AudioClip clip)
    {
        //if (audioType == AudioType.BGM)
        //{
        //    bgmSource.Stop();
        //    bgmSource.clip = clip;
        //    bgmSource.Play();
        //    return bgmSource;
        //}
        //else if (audioType == AudioType.SONG)
        //{
        //    songSource.Stop();
        //    songSource.clip = clip;
        //    songSource.Play();
        //    return songSource;
        //}
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.Play();
        //if (audioType == AudioType.SFX)
        //{
        //    sfxSource.Add(source);
        //}
        //else if (audioType == AudioType.UI)
        //{
        //    uiSource.Add(source);
        //}
        //else if (audioType == AudioType.AMBIENT)
        //{
        //    ambientSource.Add(source);
        //}
        //else
        //{
        //    return null;
        //}
        return source;
    }

    //public void AudioDebug()
    //{
    //    Debug.Log(@$"[Audio Debug]
    //        AudioListener.volume: {AudioListener.volume}
    //        AudioListener.pause: {AudioListener.pause}
    //        BGM: {bgmSource.clip?.name}
    //        SFX: {sfxSource}
    //        UI: {uiSource}
    //        Ambient: {ambientSource}
    //        Song: {songSource.clip?.name}
    //        ");
    //}
}