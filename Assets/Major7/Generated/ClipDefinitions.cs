using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ClipDefinitions {
	public static AsyncOperationHandle<AudioClip> ChippySurgeFromthegameSurge => Addressables.LoadAssetAsync<AudioClip>("Assets/Major7/Sample/DemoAudio/ChippySurge (From the game Surge).wav");
	public static AsyncOperationHandle<AudioClip> OrchHit => Addressables.LoadAssetAsync<AudioClip>("Assets/Major7/Sample/DemoAudio/OrchHit.wav");
}