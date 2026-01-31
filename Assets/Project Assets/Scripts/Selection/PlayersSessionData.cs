using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public class Player
{
    public int index;
    public Color color;
    public List<int> devIds = new List<int>();
    public List<InputDevice> devices = new List<InputDevice>();
    public CharacterData selectedCharacter;
    public List<CharacterData> lockedChars = new List<CharacterData>();

    public Player(int idx, Color col, PlayerInput input, CharacterDatabase db)
    {
        index = idx;
        color = col;

        foreach (var device in input.devices)
        {
            devices.Add(device);
            devIds.Add(device.deviceId);
        }

        if (db != null)
        {
            foreach (var character in db.characterRoster)
            {
                if (character != null && character.isSecret)
                {
                    lockedChars.Add(character);
                }
            }
        }
    }
}

[CreateAssetMenu(fileName = "PlayersSessionData", menuName = "Game/Players Session Data")]
public class PlayersSessionData : ScriptableObject
{
    [Header("External References")]
    public CharacterDatabase charDB;

    [Header("Global Config")]
    public Color[] colors;

    [Header("Current State")]
    public int count = 0;
    public List<Player> players = new List<Player>();

    public void ResetSession()
    {
        count = 0;
        players.Clear();
    }

    public void AddPlayer(PlayerInput input)
    {
        int newIndex = count;
        Color assignedColor = Color.white;

        if (colors != null && colors.Length > 0)
        {
            assignedColor = colors[newIndex % colors.Length];
        }

        Player newPlayer = new Player(newIndex, assignedColor, input, charDB);
        players.Add(newPlayer);
        count++;
    }

    public void RemovePlayer(PlayerInput input)
    {
        if (players.Count == 0) return;

        Player playerToRemove = players.FirstOrDefault(p => input.devices.Any(d => p.devIds.Contains(d.deviceId)));

        if (playerToRemove != null)
        {
            players.Remove(playerToRemove);
            count--;

            for (int i = 0; i < players.Count; i++)
            {
                players[i].index = i;
            }
        }
    }
}