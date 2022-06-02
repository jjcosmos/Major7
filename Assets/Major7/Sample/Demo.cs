using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Demo : MonoBehaviour
{
    [SerializeField] private Transform orbiter;
    [SerializeField] private Text playPauseText;
    private Major7Man.PlayId loopingSoundTrack;
    private AsyncOperationHandle<AudioClip> orchestraHitHandle;
    private AsyncOperationHandle<AudioClip> surgeOSTHandle;
    private IEnumerator Start()
    {
        // Not entirely necessary - When allowed to auto init, Major7Man will be initialized before any standard component's Awake().
        yield return new WaitUntil(() => Major7Man.Initialized);
        
        // Optionally, subscribe to this to listen for audio events
        Major7Man.Singleton.OnAudioEvent += AudioEventPosted;
        
        // Grab a handle to the desired clip and ask it to load.
        // If you don't want the playing code to be async, you can preload those clips in Start().
        orchestraHitHandle = ClipDefinitions.OrchHit;
        surgeOSTHandle = ClipDefinitions.ChippySurgeFromthegameSurge;
        yield return Major7Man.LoadManyAsync(orchestraHitHandle, surgeOSTHandle);
        
        // Build an AudioInfo with the desired settings
        // At minimum, needs a name, clip, and transform (sound origin)
        var audioInfo = new Major7Man.AudioInfo(
            "Orbiter Event!",
            surgeOSTHandle.Result,
            orbiter.transform,
            true);

        loopingSoundTrack = Major7Man.Singleton.PlaySound3D(audioInfo, AudioReverbPreset.Alley);
    }

    public void OnSliderValueChanged(float percent)
    {
        if (loopingSoundTrack != Major7Man.PlayId.Invalid)
        {
            Major7Man.Singleton.SetGainByPlayId(loopingSoundTrack, percent);
        }
    }

    public void OnPlayPausePressed()
    {
        var audioInfo = new Major7Man.AudioInfo(
            "Play Pause Event!",
            orchestraHitHandle.Result,
            transform);

        if (orchestraHitHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Major7Man.Singleton.PlaySound2D(audioInfo);
        }

        if (Major7Man.Singleton.GetPauseStateByPlayId(loopingSoundTrack))
        {
            Major7Man.Singleton.ResumeByPlayId(loopingSoundTrack);
            playPauseText.text = "Pause";
        }
        else
        {
            Major7Man.Singleton.PauseByPlayId(loopingSoundTrack);
            playPauseText.text = "Play";
        }
        
    }

    private void Update()
    {
        orbiter.RotateAround(transform.position, Vector3.up, Time.deltaTime * 20f);
    }

    private void OnDestroy()
    {
        if(Major7Man.Initialized)
            Major7Man.Singleton.OnAudioEvent -= AudioEventPosted;
    }

    private void AudioEventPosted(string eventName, Vector3 position)
    {
        Debug.Log($"Playing {eventName} at {position} | {Time.time}");
    }
}
