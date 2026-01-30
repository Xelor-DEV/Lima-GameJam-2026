using UnityEngine;

namespace NexusChaser.CycloneAMS
{
    // Wrapper para el AudioClip con propiedades extra (Loop)
    [CreateAssetMenu(fileName = "NewCycloneClip", menuName = "Nexus Chaser/Cyclone AMS/Cyclone Clip")]
    public class CycloneClip : ScriptableObject
    {
        [Header("Audio Data")]
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool isLoopable;

        public AudioClip Clip => clip;
        public bool IsLoopable => isLoopable;
    }
}