using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Linq; // Necesario para funciones de listas como Any o Select

// Clase serializada para el jugador individual
[Serializable]
public class Player
{
    public int playerIndex;
    public Color playerColor;

    // CAMBIO: Ahora soportamos listas de dispositivos
    public List<int> deviceIds = new List<int>();
    public List<InputDevice> devices = new List<InputDevice>();

    // Personajes bloqueados para este jugador (ej. secretos)
    public List<CharacterData> lockedCharactersForThisPlayer = new List<CharacterData>();

    // Constructor actualizado
    public Player(int index, Color color, PlayerInput input, CharacterDatabase charDB)
    {
        playerIndex = index;
        playerColor = color;

        // 1. Guardar TODOS los dispositivos asociados a este PlayerInput
        foreach (var device in input.devices)
        {
            devices.Add(device);
            deviceIds.Add(device.deviceId);
        }

        // 2. Inicializar personajes bloqueados buscando los secretos en la base de datos
        if (charDB != null)
        {
            foreach (var character in charDB.characterRoster)
            {
                if (character != null && character.isSecret)
                {
                    lockedCharactersForThisPlayer.Add(character);
                }
            }
        }
    }
}

[CreateAssetMenu(fileName = "PlayersSessionData", menuName = "Game/Players Session Data")]
public class PlayersSessionData : ScriptableObject
{
    [Header("Referencias Externas")]
    // CAMBIO: Referencia necesaria para saber qué personajes son secretos
    public CharacterDatabase characterDatabase;

    [Header("Configuración Global")]
    public Color[] availableColors;

    [Header("Estado Actual (Read Only)")]
    public int playerCount = 0;
    public List<Player> activePlayers = new List<Player>();

    public void ResetSession()
    {
        playerCount = 0;
        activePlayers.Clear();
    }

    public void AddPlayer(PlayerInput input)
    {
        if (characterDatabase == null)
        {
            Debug.LogError("PlayersSessionData: Faltan asignar 'CharacterDatabase'. No se pueden calcular los personajes secretos.");
        }

        // Calcular índice
        int newIndex = playerCount;

        // Asignar color cíclico
        Color assignedColor = Color.white;
        if (availableColors != null && availableColors.Length > 0)
        {
            assignedColor = availableColors[newIndex % availableColors.Length];
        }

        // Crear el nuevo jugador pasando la DB para que filtre los secretos
        Player newPlayer = new Player(newIndex, assignedColor, input, characterDatabase);

        activePlayers.Add(newPlayer);
        playerCount++;

        Debug.Log($"Jugador {newIndex} unido. Dispositivos: {newPlayer.devices.Count}. Bloqueados: {newPlayer.lockedCharactersForThisPlayer.Count}");
    }

    // CAMBIO: Implementación completa de RemovePlayer
    public void RemovePlayer(PlayerInput input)
    {
        if (activePlayers.Count == 0) return;

        // Buscamos al jugador que tenga coincidencia con los dispositivos del input desconectado.
        // Usamos el ID del primer dispositivo del input como referencia principal, 
        // o verificamos si alguno de los dispositivos del PlayerInput existe en nuestro Player guardado.

        Player playerToRemove = null;

        foreach (var player in activePlayers)
        {
            // Verificamos si CUALQUIERA de los dispositivos del input que se va 
            // coincide con los dispositivos registrados en este jugador.
            foreach (var inputDevice in input.devices)
            {
                if (player.deviceIds.Contains(inputDevice.deviceId))
                {
                    playerToRemove = player;
                    break;
                }
            }

            if (playerToRemove != null) break;
        }

        if (playerToRemove != null)
        {
            Debug.Log($"Jugador {playerToRemove.playerIndex} desconectado. Eliminando datos.");
            activePlayers.Remove(playerToRemove);
            playerCount--;

            for (int i = 0; i < activePlayers.Count; i++)
            {
                activePlayers[i].playerIndex = i;
            }
        }
        else
        {
            Debug.LogWarning("Se intentó remover un jugador pero no se encontró coincidencia de dispositivos en la SessionData.");
        }
    }
}