using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioClip cardPlacedOnTable;

    [SerializeField]
    private AudioClip drawCard;

    [SerializeField]
    private AudioClip cardShove;

    [SerializeField]
    private AudioClip turnNotification;

    [SerializeField]
    private AudioClip winner;

    [SerializeField]
    private AudioClip loser;

    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

    private AudioSource mainAudioSource;
    private AudioSource secondaryAudioSource;

    private void Awake()
    {
        var audioSources = GetComponents<AudioSource>();
        if(audioSources.Length == 2)
        {
            mainAudioSource = audioSources[0];
            secondaryAudioSource = audioSources[1];
        }
        else
        {
            Debug.LogError("There must be 2 audio source components attached to this game object");
        }

        SetAudioClips();
    }

    // Callback is ran only in the Unity Editor
    // Gets called whenever a serialized field changes
    private void OnValidate()
    {
        SetAudioClips();
    }

    private void SetAudioClips()
    {
        audioClips["cardPlacedOnTable"] = cardPlacedOnTable;
        audioClips["drawCard"] = drawCard;
        audioClips["cardShove"] = cardShove;
        audioClips["turnNotification"] = turnNotification;
        audioClips["winner"] = winner;
        audioClips["loser"] = loser;
    }

    public void PlayClip(string name)
    {
        if(audioClips.ContainsKey(name))
        {
            // Card sfx shouldn't interfere with other sounds such as turn notification or announcer sound

            var secondaryLayerClips = new AudioClip[]
            {
                turnNotification,
                winner,
                loser
            };

            if(secondaryLayerClips.Contains(audioClips[name]))
            {
                secondaryAudioSource.clip = audioClips[name];
                secondaryAudioSource.Play();
            }
            else
            {
                mainAudioSource.clip = audioClips[name];
                mainAudioSource.Play();
            }
        }
        else
        {
            Debug.LogWarningFormat("Could not play the sound: {0}", name);
        }
    }

    public float GetCurrentClipDuration()
    {
        return mainAudioSource.clip.length;
    }
}
