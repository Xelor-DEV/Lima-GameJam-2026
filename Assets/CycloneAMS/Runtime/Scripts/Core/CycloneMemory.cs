using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace NexusChaser.CycloneAMS
{
    public static class AudioConstants
    {
        public const float MIN_VOLUME_THRESHOLD = 0.0001f;
        public const float MAX_VOLUME_THRESHOLD = 1;
        public const string AUDIO_CHANNEL_SOURCE_PREFIX = "SharedSource_";
        public const string DEDICATED_SOURCE_PREFIX = "DedicatedSource_";
    }

    public enum ChannelType
    {
        Master,
        Music,
        SFX,
        Voices,
        UI
    }

    [Serializable]
    public class VolumeParam
    {
        [Header("Channel Configuration")]
        public string name;
        public ChannelType type;

        [Header("Mixer Settings")]
        [Tooltip("The AudioMixerGroup to which the AudioSource created for this channel will belong.")]
        public AudioMixerGroup mixerGroup;

        [Tooltip("The exact name of the exposed parameter in the AudioMixer")]
        public string exposedParamName;

        [Range(AudioConstants.MIN_VOLUME_THRESHOLD, AudioConstants.MAX_VOLUME_THRESHOLD)]
        public float volume = 0.5f;
        
        // Propiedad calculada para convertir lineal a decibelios
        public float DecibelValue => Mathf.Log10(Mathf.Max(volume, AudioConstants.MIN_VOLUME_THRESHOLD)) * 20f;
    }

    [CreateAssetMenu(fileName = "NewCycloneMemory", menuName = "Nexus Chaser/Cyclone AMS/Cyclone Memory", order = 1)]
    public class CycloneMemory : ScriptableObject
    {
        [Header("Audio Mixer Reference")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Channel Configuration")]
        [ReorderableList(ListStyle.Boxed, "Channel", Foldable = true)]
        [LabelByChild("name")]
        [SerializeField] private List<VolumeParam> volumeChannels = new List<VolumeParam>();

        // Diccionario para acceso rápido O(1)
        private Dictionary<ChannelType, VolumeParam> channelLookup;

        private void OnEnable()
        {
            try
            {
                InitializeLookup();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CycloneAMS] Fatal error initializing CycloneMemory: {e.Message}");
            }
        }

        public void InitializeLookup()
        {
            channelLookup = new Dictionary<ChannelType, VolumeParam>();
            foreach (VolumeParam channel in volumeChannels)
            {
                if (!channelLookup.ContainsKey(channel.type))
                {
                    channelLookup.Add(channel.type, channel);
                }
            }
        }

        public void SetVolume(ChannelType type, float newVolume)
        {
            try
            {
                if (audioMixer == null)
                {
                    throw new NullReferenceException("[CycloneAMS] AudioMixer reference is NULL in CycloneMemory.");
                }

                if (channelLookup == null || channelLookup.Count == 0)
                {
                    InitializeLookup();
                }

                if (channelLookup.TryGetValue(type, out VolumeParam channel))
                {
                    // Actualizamos datos
                    channel.volume = Mathf.Clamp(newVolume, AudioConstants.MIN_VOLUME_THRESHOLD, AudioConstants.MAX_VOLUME_THRESHOLD);

                    // Intentamos aplicar al mixer
                    bool success = audioMixer.SetFloat(channel.exposedParamName, channel.DecibelValue);

                    if (!success)
                    {
                        throw new ArgumentException($"[CycloneAMS] Parameter '{channel.exposedParamName}' is not exposed in the AudioMixer.");
                    }
                }
                else
                {
                    throw new KeyNotFoundException($"[CycloneAMS] Channel '{type}' was not found in CycloneMemory configuration.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Error setting volume for {type}: {ex.Message}");
                throw;
            }
        }

        public float GetVolume(ChannelType type)
        {
            try
            {
                if (channelLookup == null)
                {
                    InitializeLookup();
                }

                if (channelLookup.TryGetValue(type, out VolumeParam channel))
                {
                    return channel.volume;
                }
                else
                {
                    throw new KeyNotFoundException($"[CycloneAMS] Attempted to access volume for channel '{type}', but it is not configured.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Critical error in GET volume ({type}): {ex.Message}");
                throw;
            }
        }

        public void ApplyAllToMixer()
        {
            try
            {
                if (audioMixer == null)
                {
                    throw new NullReferenceException("AudioMixer is NULL.");
                }

                foreach (VolumeParam channel in volumeChannels)
                {
                    if (!audioMixer.SetFloat(channel.exposedParamName, channel.DecibelValue))
                    {
                        Debug.LogWarning($"[CycloneAMS] Warning: Could not apply initial value to '{channel.exposedParamName}'. Is it exposed?");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CycloneAMS] Failed to apply initial configuration: {ex.Message}");
            }
        }

        public VolumeParam GetParamByChannel(ChannelType type)
        {
            try
            {
                if (channelLookup == null)
                {
                    InitializeLookup();
                }             

                if (channelLookup.TryGetValue(type, out VolumeParam param))
                {
                    return param;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Error getting VolumeParam for {type}: {ex.Message}");
                return null;
            }
        }

        public void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float time)
        {
            try
            {
                if (audioMixer == null)
                {
                    throw new NullReferenceException("[CycloneAMS] AudioMixer is NULL in CycloneMemory.");
                }

                if (snapshots == null || weights == null)
                {
                    throw new ArgumentNullException("[CycloneAMS] Snapshots or Weights array is null.");
                }

                if (snapshots.Length != weights.Length)
                {
                    throw new ArgumentException("[CycloneAMS] Snapshots and Weights arrays must have the same length.");
                }

                audioMixer.TransitionToSnapshots(snapshots, weights, time);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CycloneAMS] Failed to transition snapshots: {ex.Message}");
            }
        }

        public List<VolumeParam> VolumeChannels => volumeChannels;
    }
}