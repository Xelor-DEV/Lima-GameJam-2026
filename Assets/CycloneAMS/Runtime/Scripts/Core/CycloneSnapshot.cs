using UnityEngine;
using UnityEngine.Audio;

namespace NexusChaser.CycloneAMS
{
    [CreateAssetMenu(fileName = "NewCycloneSnapshot", menuName = "Nexus Chaser/Cyclone AMS/Cyclone Snapshot")]
    public class CycloneSnapshot : ScriptableObject
    {
        [Header("Snapshot Config")]
        [Tooltip("The AudioMixerSnapshot to transition to.")]
        [SerializeField] private AudioMixerSnapshot snapshot;

        [Tooltip("Time in seconds to reach this snapshot.")]
        [Min(0f)]
        [SerializeField] private float transitionTime = 1.0f;
        [Tooltip("Target weight for this snapshot (0.0 to 1.0).\n\n" +
                 "• Set to 1.0 to fully replace current state (Standard 'TransitionTo' behavior).\n" +
                 "• Set to lower values (e.g., 0.5) to blend this snapshot with others.")]
        [Range(0f, 1f)]
        [SerializeField] private float weight = 1.0f;

        public AudioMixerSnapshot Snapshot => snapshot;
        public float TransitionTime => transitionTime;
        public float Weight => weight;
    }
}