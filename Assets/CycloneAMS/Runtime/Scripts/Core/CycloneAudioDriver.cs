using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace NexusChaser.CycloneAMS
{
    public class CycloneAudioDriver : NonPersistentSingleton<CycloneAudioDriver>
    {
        [Header("Settings")]
        [SerializeField] private CycloneMemory memory;
        [SerializeField] private GameObject audioSourcePrefab;

        [SerializeField] private bool playMusicOnAwake = false;
        [SerializeField] private CycloneClip awakeMusic;
        [SerializeField] private bool applyMemoryToMixer = true;

        [Header("Libraries")]
        [Tooltip("Drag all the audio libraries the game will use here")]
        [ReorderableList(ListStyle.Boxed, "Audio Libraries", Foldable = true)]
        [SerializeField] private List<CycloneLibrary> audioLibraries;

        [Header("Snapshots")]
        [SerializeField] private CycloneSnapshot defaultSnapshot;

        // 1. Shared Sources: One AudioSource per ChannelType (Music, SFX, etc.)
        private Dictionary<ChannelType, AudioSource> sharedChannelSources = new Dictionary<ChannelType, AudioSource>();

        // 2. Clip Lookup for Shared Sources: Maps a Clip to a ChannelType
        private Dictionary<CycloneClip, ChannelType> sharedClipMap = new Dictionary<CycloneClip, ChannelType>();

        // 3. Dedicated Sources: Maps a specific Clip to its own exclusive AudioSource
        private Dictionary<CycloneClip, AudioSource> dedicatedClipSources = new Dictionary<CycloneClip, AudioSource>();

        // --- NUEVO: Sistema de Instancias Rastreadas (Tracked Instances) ---
        private Dictionary<int, AudioSource> trackedSources = new Dictionary<int, AudioSource>();
        private int nextTrackedId = 0;
        // -------------------------------------------------------------------

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSystem();
        }

        private void Start()
        {
            if (defaultSnapshot != null)
            {
                ApplySnapshot(defaultSnapshot);
            }

            if (applyMemoryToMixer == true && memory != null)
            {
                memory.ApplyAllToMixer();
            }

            if (awakeMusic != null && playMusicOnAwake == true)
            {
                Play(awakeMusic);
            }
        }

        private void InitializeAudioSystem()
        {
            try
            {
                if (memory == null || audioSourcePrefab == null)
                {
                    throw new MissingReferenceException("[CycloneAMS] CycloneMemory or AudioSource Prefab is not assigned.");
                }

                // Step 1: Create the base shared sources (one per channel type)
                InitializeSharedSources();

                // Step 2: Process libraries (Shared vs Dedicated logic)
                InitializeLibraries();

                Debug.Log($"[CycloneAMS] System initialized. Shared Sources: {sharedChannelSources.Count}, Dedicated Sources: {dedicatedClipSources.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CycloneAMS] CRITICAL FAILURE during initialization: {ex.Message}");
            }
        }

        private void InitializeSharedSources()
        {
            foreach (VolumeParam param in memory.VolumeChannels)
            {
                try
                {
                    GameObject newSourceObj = Instantiate(audioSourcePrefab, transform);
                    newSourceObj.name = AudioConstants.AUDIO_CHANNEL_SOURCE_PREFIX + param.name;

                    AudioSource source = newSourceObj.GetComponent<AudioSource>();

                    if (source == null)
                    {
                        throw new MissingComponentException("[CycloneAMS] Prefab does not have an AudioSource component.");
                    }

                    source.outputAudioMixerGroup = param.mixerGroup;
                    source.playOnAwake = false;
                    source.loop = false;

                    if (!sharedChannelSources.ContainsKey(param.type))
                    {
                        sharedChannelSources.Add(param.type, source);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CycloneAMS] Failed to create shared source for {param.name}: {e.Message}");
                }
            }
        }

        private void InitializeLibraries()
        {
            foreach (CycloneLibrary lib in audioLibraries)
            {
                if (lib == null) continue;

                // We need the volume param to assign the correct Mixer Group to dedicated sources
                VolumeParam channelParam = memory.GetParamByChannel(lib.ChannelType);

                foreach (CycloneClip clip in lib.Clips)
                {
                    if (clip == null) continue;

                    try
                    {
                        if (lib.UseDedicatedSource)
                        {
                            // === LOGIC A: DEDICATED SOURCE ===
                            // Create a specific AudioSource just for this clip
                            CreateDedicatedSource(clip, channelParam);
                        }
                        else
                        {
                            // === LOGIC B: SHARED SOURCE ===
                            // Just register the clip to the map so we know which shared channel to use later
                            if (!sharedClipMap.ContainsKey(clip))
                            {
                                sharedClipMap.Add(clip, lib.ChannelType);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[CycloneAMS] Error processing clip '{clip.name}' in library: {e.Message}");
                    }
                }
            }
        }

        private void CreateDedicatedSource(CycloneClip clip, VolumeParam channelParam)
        {
            GameObject dedicatedObj = Instantiate(audioSourcePrefab, transform);
            dedicatedObj.name = $"{AudioConstants.DEDICATED_SOURCE_PREFIX}{clip.name}";

            AudioSource source = dedicatedObj.GetComponent<AudioSource>();

            if (channelParam != null)
            {
                source.outputAudioMixerGroup = channelParam.mixerGroup;
            }
            else
            {
                Debug.LogWarning($"[CycloneAMS] No MixerGroup found for dedicated clip '{clip.name}'. It will play on Default AudioListener.");
            }

            // Pre-configure the source since it's exclusive to this clip
            source.clip = clip.Clip;
            source.loop = clip.IsLoopable;
            source.playOnAwake = false;

            if (!dedicatedClipSources.ContainsKey(clip))
            {
                dedicatedClipSources.Add(clip, source);
            }
        }

        public void Play(CycloneClip cycloneClip)
        {
            try
            {
                if (cycloneClip == null)
                {
                    throw new ArgumentNullException("[CycloneAMS] CycloneClip is null.");
                }

                // 1. Check if this clip has a Dedicated Source
                if (dedicatedClipSources.TryGetValue(cycloneClip, out AudioSource dedicatedSource))
                {
                    // Logic: Dedicated sources already have clip/loop set during init.
                    // If it's already playing, just let it be (or restart if needed).
                    if (!dedicatedSource.isPlaying)
                    {
                        dedicatedSource.Play();
                    }
                    return;
                }

                // 2. If not dedicated, check if it's registered for a Shared Source
                if (sharedClipMap.TryGetValue(cycloneClip, out ChannelType type))
                {
                    if (sharedChannelSources.TryGetValue(type, out AudioSource sharedSource))
                    {
                        // Logic: Shared sources must be reconfigured every time
                        sharedSource.Stop(); // Optional: Stop previous sound on this channel
                        sharedSource.clip = cycloneClip.Clip;
                        sharedSource.loop = cycloneClip.IsLoopable;
                        sharedSource.Play();
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Shared source for channel type '{type}' not found.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CycloneAMS] Clip '{cycloneClip.name}' is not registered in any loaded Library.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Failed to Play '{cycloneClip?.name}': {ex.Message}");
            }
        }

        public void PlayOneShot(CycloneClip cycloneClip)
        {
            try
            {
                if (cycloneClip == null)
                {
                    throw new ArgumentNullException("[CycloneAMS] CycloneClip is null.");
                }

                // 1. Check Dedicated
                if (dedicatedClipSources.TryGetValue(cycloneClip, out AudioSource dedicatedSource))
                {
                    // Even for dedicated sources, OneShot is useful to overlap the same sound
                    dedicatedSource.PlayOneShot(cycloneClip.Clip);
                    return;
                }

                // 2. Check Shared
                if (sharedClipMap.TryGetValue(cycloneClip, out ChannelType type))
                {
                    if (sharedChannelSources.TryGetValue(type, out AudioSource sharedSource))
                    {
                        sharedSource.PlayOneShot(cycloneClip.Clip);
                    }
                }
                else
                {
                    Debug.LogWarning($"[CycloneAMS] Clip '{cycloneClip.name}' is not registered in any loaded Library.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Failed to PlayOneShot '{cycloneClip?.name}': {ex.Message}");
            }
        }

        public void Stop(CycloneClip cycloneClip)
        {
            try
            {
                if (dedicatedClipSources.TryGetValue(cycloneClip, out AudioSource source))
                {
                    source.Stop();
                }
                else if (sharedClipMap.TryGetValue(cycloneClip, out ChannelType type))
                {
                    // If it's a shared source, we only stop it if IT IS currently playing THIS clip
                    if (sharedChannelSources.TryGetValue(type, out AudioSource sharedSource))
                    {
                        if (sharedSource.clip == cycloneClip.Clip)
                        {
                            sharedSource.Stop();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Error stopping clip: {ex.Message}");
            }
        }

        public void StopAll()
        {
            foreach (var source in sharedChannelSources.Values) source.Stop();
            foreach (var source in dedicatedClipSources.Values) source.Stop();

            // Limpiar también las instanciadas
            List<int> ids = new List<int>(trackedSources.Keys);
            foreach (int id in ids) StopTracked(id);
        }

        public void ApplySnapshot(CycloneSnapshot snapshotData)
        {
            try
            {
                if (snapshotData == null)
                {
                    throw new ArgumentNullException("[CycloneAMS] Snapshot data is null.");
                }

                if (snapshotData.Snapshot == null)
                {
                    throw new NullReferenceException($"[CycloneAMS] The AudioMixerSnapshot inside '{snapshotData.name}' is missing.");
                }

                AudioMixerSnapshot[] snapshots = new AudioMixerSnapshot[] { snapshotData.Snapshot };
                float[] weights = new float[] { snapshotData.Weight };

                memory.TransitionToSnapshots(snapshots, weights, snapshotData.TransitionTime);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Error applying snapshot: {ex.Message}");
            }
        }

        // Sobrecarga con tiempo personalizado
        public void ApplySnapshot(CycloneSnapshot snapshotData, float customTime)
        {
            try
            {
                if (snapshotData == null)
                { 
                    throw new ArgumentNullException("[CycloneAMS] Snapshot data is null."); 
                }

                AudioMixerSnapshot[] snapshots = new AudioMixerSnapshot[] { snapshotData.Snapshot };
                float[] weights = new float[] { snapshotData.Weight };

                memory.TransitionToSnapshots(snapshots, weights, customTime);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Error applying snapshot (custom time): {ex.Message}");
            }
        }

        public void SetPitch(CycloneClip cycloneClip, float pitch)
        {
            try
            {
                if (cycloneClip == null) return;

                // 1. Buscar en Fuentes Dedicadas (Prioridad)
                if (dedicatedClipSources.TryGetValue(cycloneClip, out AudioSource dedicatedSource))
                {
                    dedicatedSource.pitch = pitch;
                    return;
                }

                // 2. Buscar en Fuentes Compartidas
                if (sharedClipMap.TryGetValue(cycloneClip, out ChannelType type))
                {
                    if (sharedChannelSources.TryGetValue(type, out AudioSource sharedSource))
                    {
                        // IMPORTANTE: Solo cambiamos el pitch si este canal está reproduciendo NUESTRO clip.
                        // Esto evita cambiarle el tono a otro sonido que haya robado el canal.
                        if (sharedSource.clip == cycloneClip.Clip)
                        {
                            sharedSource.pitch = pitch;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CycloneAMS] Error setting pitch for '{cycloneClip?.name}': {ex.Message}");
            }
        }



        /// <summary>
        /// Crea una NUEVA instancia dedicada para este clip y devuelve un ID.
        /// Úsalo cuando varios objetos necesitan reproducir el mismo loop simultáneamente.
        /// Debes llamar a StopTracked(id) para detenerlo.
        /// </summary>
        public int PlayTracked(CycloneClip cycloneClip)
        {
            if (cycloneClip == null) return -1;

            // 1. Instanciar nueva fuente
            GameObject newObj = Instantiate(audioSourcePrefab, transform);
            newObj.name = $"TrackedSource_{cycloneClip.name}_{nextTrackedId}";
            AudioSource source = newObj.GetComponent<AudioSource>();

            // 2. Configurar Mixer Group
            // Buscamos a qué canal pertenece este clip (ya sea dedicado o compartido)
            VolumeParam targetParam = null;

            // Intento A: ¿Está en el mapa compartido?
            if (sharedClipMap.TryGetValue(cycloneClip, out ChannelType type))
            {
                targetParam = memory.GetParamByChannel(type);
            }
            // Intento B: ¿Es dedicado? (Buscamos la librería a fuerza bruta, un poco costoso pero seguro)
            else
            {
                foreach (var lib in audioLibraries)
                {
                    if (lib.Clips.Contains(cycloneClip))
                    {
                        targetParam = memory.GetParamByChannel(lib.ChannelType);
                        break;
                    }
                }
            }

            if (targetParam != null) source.outputAudioMixerGroup = targetParam.mixerGroup;

            // 3. Configurar clip
            source.clip = cycloneClip.Clip;
            source.loop = cycloneClip.IsLoopable;
            source.Play();

            // 4. Registrar
            int id = nextTrackedId++;
            trackedSources.Add(id, source);

            return id;
        }

        /// <summary>
        /// Detiene y destruye una instancia de audio creada con PlayTracked.
        /// </summary>
        public void StopTracked(int instanceId)
        {
            if (instanceId == -1) return;

            if (trackedSources.TryGetValue(instanceId, out AudioSource source))
            {
                if (source != null)
                {
                    source.Stop(); // 1. Parar sonido YA.
                    source.volume = 0f; // 2. Silenciar por si acaso.
                    source.clip = null; // 3. Desvincular clip.
                    Destroy(source.gameObject); // 4. Destruir GameObject.
                }
                trackedSources.Remove(instanceId);
            }
        }
        public void SetPitchTracked(int instanceId, float pitch)
        {
            if (trackedSources.TryGetValue(instanceId, out AudioSource source))
            {
                if (source != null) source.pitch = pitch;
            }
        }
    }
}