using System.Collections.Generic;
using UnityEngine;

namespace NexusChaser.CycloneAMS
{
    // Librería que agrupa clips bajo un mismo tipo de canal
    [CreateAssetMenu(fileName = "NewCycloneLibrary", menuName = "Nexus Chaser/Cyclone AMS/Cyclone Library")]
    public class CycloneLibrary : ScriptableObject
    {
        [Header("Library Config")]
        [SerializeField] private ChannelType channelType;

        [Tooltip("If TRUE: Creates a unique AudioSource for EACH clip in this list. If FALSE: Uses the shared AudioSource for the Channel Type.")]
        [SerializeField] private bool useDedicatedSource;

        [Header("Clips")]
        [ReorderableList(ListStyle.Boxed, "Audio Clips", Foldable = true)]
        [SerializeField] private List<CycloneClip> clips = new List<CycloneClip>();

        public ChannelType ChannelType => channelType;
        public bool UseDedicatedSource => useDedicatedSource;
        public List<CycloneClip> Clips => clips;
    }
}