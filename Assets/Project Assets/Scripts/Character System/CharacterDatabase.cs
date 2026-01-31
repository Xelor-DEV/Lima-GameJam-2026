using UnityEngine;
using System;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character System/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [Header("Database Config")]
    public CharacterData[] characterRoster;

    // Retrieves a character based on its index within the roster.
    public CharacterData GetCharacter(int index)
    {
        try
        {
            return characterRoster[index];
        }
        catch (IndexOutOfRangeException)
        {
            Debug.LogWarning($"[CharacterDatabase] Index {index} is out of range. Returning null.");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterDatabase] An unexpected error occurred: {e.Message}");
            return null;
        }
    }
}