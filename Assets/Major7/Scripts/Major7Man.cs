using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

[DefaultExecutionOrder(-1)]
public class Major7Man : MonoBehaviour
{
    private static M7Config config;
    public static Major7Man Singleton { get; private set; }
    public static bool Initialized { get; private set; }
    
    public Action<string, Vector3> OnAudioEvent; // This action is invoked when an audio event plays, passing the AudioInfo's name and position
    
    private AudioSource[] m_Pool;
    private AudioReverbFilter[] m_ReverbPool;
    private Transform[] m_FollowParents;
    private bool[] m_pauseStates;
    
    private void Awake()
    {
        if (null == Singleton)
        {
            Singleton = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        Init();
    }

    private void Init()
    {
        GetInitConfig();
        
        var poolSize = config.SourcePoolSize;
        m_Pool = new AudioSource[poolSize];
        m_ReverbPool = new AudioReverbFilter[poolSize];
        m_FollowParents = new Transform[poolSize];
        m_pauseStates = new bool[poolSize];

        for (var i = 0; i < poolSize; ++i)
        {
            var aSrc = new GameObject("pooled " + i, typeof(AudioSource));
            aSrc.transform.parent = transform;
            m_Pool[i] = aSrc.GetComponent<AudioSource>();
            m_Pool[i].playOnAwake = false;

            m_ReverbPool[i] = aSrc.AddComponent<AudioReverbFilter>();
            m_ReverbPool[i].reverbPreset = AudioReverbPreset.Off;

            // Not actually necessary
            m_pauseStates[i] = false;
            
            m_Pool[i].gameObject.SetActive(false);
        }
        
        StartCoroutine(CleanupRoutine());
        Initialized = true;
    }

    public static M7Config GetInitConfig()
    {
        // Addressables doesn't allow synchronous loading.
        if(config == null)
            config = Resources.Load<M7Config>("Config/M7Config");
        return config;
    }
    
    public readonly struct AudioInfo
    {
        public AudioInfo(string name, AudioClip clip, Transform transform, bool loop = false, float gain = 1, float pitch = 1)
        {
            Name = name;
            Clip = clip;
            XForm = transform;
            Loop = loop;
            Gain = gain;
            Pitch = pitch;
        }
        
        public readonly string Name; // The human-readable name for the sound. Use for subtitles/loc.
        public readonly AudioClip Clip; // The audio clip to be played.
        public readonly Transform XForm; // Can be null, if object tracking is not desired.
        public readonly float Gain; // 1 is standard, 0 is silent.
        public readonly bool Loop; // Should the audio source loop?
        public readonly float Pitch; // Pitch offset. Defaults to 1.
    }

    public readonly struct PlayId : IEquatable<PlayId>
    {
        public readonly int Value;
        public PlayId(int value)
        {
            Value = value;
        }

        public static PlayId Invalid => new PlayId(-1);

        public bool Equals(PlayId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayId other && Equals(other);
        }

        public static bool operator ==(PlayId a, PlayId b) => a.Value == b.Value;

        public static bool operator !=(PlayId a, PlayId b) => !(a == b);

        public override int GetHashCode()
        {
            return Value;
        }
    }

    private bool Validate(PlayId playId)
    {
        return playId.Value < m_Pool.Length && playId != PlayId.Invalid && null != m_Pool[playId.Value];
    }

    // Todo: This is a little hacky
    public PlayId PlaySound2D(AudioInfo audioInfo, AudioReverbPreset preset = AudioReverbPreset.Off)
    {
        var id = PlaySound3D(audioInfo, preset);
        if (id != PlayId.Invalid)
        {
            m_Pool[id.Value].spatialBlend = 0;
        }

        return id;
    }
    
    public PlayId PlaySound3D(AudioInfo audioInfo, AudioReverbPreset preset = AudioReverbPreset.Off)
    {
        if (null == audioInfo.Clip)
        {
            M7Log($"Trying to play an unloaded clip ({audioInfo.Name})!", M7LogLevel.error);
            return PlayId.Invalid;
        }
        
        if (TryGetDormantSource(out var index))
        {
            var audioSource = m_Pool[index];
            var xForm = audioSource.transform;
            
            if (null != audioInfo.XForm)
            {
                m_FollowParents[index] = audioInfo.XForm;
                xForm.position = audioInfo.XForm.position;
            }

            audioSource.spatialBlend = 1;
            audioSource.clip = audioInfo.Clip;
            audioSource.loop = audioInfo.Loop;
            audioSource.volume = audioInfo.Gain;
            audioSource.pitch = audioInfo.Pitch;

            m_ReverbPool[index].reverbPreset = preset;

            OnAudioEvent?.Invoke(audioInfo.Name, xForm.position);
            
            audioSource.gameObject.SetActive(true);
            audioSource.Play();

            return new PlayId(index);
        }
        else
        {
            return PlayId.Invalid;
        }
    }

    // Here we update the position of the audio sources based off of their follow target
    private void FixedUpdate()
    {
        for (var i = 0; i < m_Pool.Length; i++)
        {
            if (m_Pool[i].isPlaying && null != m_FollowParents[i])
            {
                m_Pool[i].transform.position = m_FollowParents[i].position;
            }
        }
    }

    public bool GetPauseStateByPlayId(PlayId playId)
    {
        if (Validate(playId))
        {
            return m_pauseStates[playId.Value];
        }

        return false;
    }

    public bool PauseByPlayId(PlayId playId)
    {
        if (Validate(playId))
        {
            m_Pool[playId.Value].Pause();
            m_pauseStates[playId.Value] = true;
            return true;
        }

        return false;
    }
    
    public bool ResumeByPlayId(PlayId playId)
    {
        if (Validate(playId))
        {
            m_Pool[playId.Value].UnPause();
            m_pauseStates[playId.Value] = false;
            return true;
        }

        return false;
    }

    public bool StopByPlayId(ref PlayId playId)
    {
        if (Validate(playId))
        {
            m_Pool[playId.Value].Stop();
            m_Pool[playId.Value].clip = null;
            m_Pool[playId.Value].gameObject.SetActive(false);
            m_pauseStates[playId.Value] = false;
            playId = PlayId.Invalid;
            return true;
        }
        
        Debug.LogError($"Play Id is invalid");
        
        playId = PlayId.Invalid;
        return false;
    }

    public bool SetGainByPlayId(PlayId playId, float volume)
    {
        if (Validate(playId))
        {
            m_Pool[playId.Value].volume = volume;
            return true;
        }
        
        Debug.LogError("Play Id is invalid");

        return false;
    }
    

    private IEnumerator CleanupRoutine()
    {
        while (true)
        {
            for (var i = 0; i < config.SourcePoolSize; ++i)
            {
                if (!m_Pool[i].isPlaying && !m_pauseStates[i])
                {
                    m_Pool[i].gameObject.SetActive(false);
                }
                yield return null;
            }
        }
    }
    
    public string GetUsageStats()
    {
        var playing = 0;
        foreach (var src in m_Pool)
        {
            playing += src.gameObject.activeSelf ? 1 : 0;
        }

        return $"{playing}/{m_Pool} sources in use";
    }

    private bool TryGetDormantSource(out int index)
    {
        for (index = 0; index < m_Pool.Length; index++)
        {
            var src = m_Pool[index];
            if (!src.gameObject.activeSelf)
            {
                return true;
            }
        }

        M7Log("No available sources in pool!", M7LogLevel.error);
        
        return false;
    }
    
    #region Loaders
    public static IEnumerator LoadManyAsync(params AsyncOperationHandle<AudioClip>[] handles)
    {
        yield return LoadManyAsync(new List<AsyncOperationHandle<AudioClip>>(handles));
    }
       
    public static IEnumerator LoadManyAsync(IEnumerable<AsyncOperationHandle<AudioClip>> handles)
    {
        while (handles.Any(x => !x.IsDone))
        {
            yield return null;
        }
   
        foreach (var handle in handles)
        {
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                throw new Exception($"$Failed to load audio file: {handle.OperationException}");
            }
        }
    }
       
    public static IEnumerator LoadAsync(AsyncOperationHandle<AudioClip> handle)
    {
        while (!handle.IsDone)
            yield return handle;
           
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            throw new Exception($"$Failed to load audio file: {handle.OperationException}");
        }
    }
    #endregion

    private static void M7Log(string msg, M7LogLevel logLevel = M7LogLevel.info)
    {
        var fullMsg = $"Major7 ({logLevel}) | {msg}";
        switch (logLevel)
        {
            case M7LogLevel.info:
                Debug.Log(fullMsg);
                break;
            case M7LogLevel.warn:
                Debug.LogWarning(fullMsg);
                break;
            case M7LogLevel.error:
                Debug.LogError(fullMsg);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    private enum M7LogLevel
    {
        info,
        warn,
        error,
    }
}
