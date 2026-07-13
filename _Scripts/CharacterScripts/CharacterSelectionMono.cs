using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectionMono : NetworkBehaviour
{
    [SerializeField]
    private GameObject CharacterSelectDisplay;

    [SerializeField]
    private Transform CharacterPreviewParent;

    [SerializeField]
    private TMP_Text CharacterNameText;

    [SerializeField]
    private TMP_Text CharacterGunText;

    [SerializeField]
    private TMP_Text CharacterAbilityText;

    // [SerializeField]
    // private float turnSpeed = 90f;

    [SerializeField]
    private Character[] characters;
    private int currentCharacterIndex = 0;
    private List<GameObject> characterInstances = new List<GameObject>();
    private Animator characterAnimator; // Add this line to hold the Animator reference.

    [SerializeField]
    private GameObject[] characterInfoPanels; // Each character's UI panel

    void Awake()
    {
        CharacterSelectDisplay.SetActive(true);
    }

    void Start()
    {
        CharacterSelectDisplay.SetActive(true);

        if (characters.Length == 0)
        {
            return;
        }

        InitializeCharacters();
        UpdateCharacterPanel();
    }

    void Update()
    {
        // RotateCharacterPreview();
    }

    private void InitializeCharacters()
    {
        foreach (Transform child in CharacterPreviewParent)
        {
            Debug.Log("🗑 Destroying old character preview: " + child.gameObject.name);
            Destroy(child.gameObject);
        }
        characterInstances.Clear();

        foreach (var character in characters)
        {
            GameObject characterInstance = Instantiate(
                character.CharacterPreviewPrefab,
                CharacterPreviewParent
            );
            characterInstance.SetActive(false);
            characterInstances.Add(characterInstance);
        }

        if (characterInstances.Count > 0)
        {
            characterInstances[currentCharacterIndex].SetActive(true);
            CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
            CharacterAbilityText.text = characters[currentCharacterIndex].CharacterAbility;
            CharacterGunText.text = characters[currentCharacterIndex].CharacterGun;
            // Play the animation for the current character preview.
            characterAnimator = characterInstances[currentCharacterIndex].GetComponent<Animator>();
            if (
                characterAnimator != null
                && characters[currentCharacterIndex].CharacterSelectionAnimation != null
            )
            {
                characterAnimator.Play(
                    characters[currentCharacterIndex].CharacterSelectionAnimation.name
                );
            }
        }
    }

    public void Select()
    {
        int selectedCharacter = currentCharacterIndex;

        // if (NetworkClient.active && NetworkClient.localPlayer != null)
        // {
        //     NetworkRoomPlayerLobby roomPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerLobby>();

        //     if (roomPlayer != null)
        //     {
        //         roomPlayer.SetSelectedCharacter(selectedCharacter);
        //     }
        //     else
        //     {
        //         Debug.LogError("NetworkGamePlayerLobby component not found!");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("Not in a multiplayer session, storing selection locally");
        //     PlayerPrefs.SetInt("SelectedCharacter", selectedCharacter);
        //     PlayerPrefs.Save();
        // }

        Debug.LogWarning("Not in a multiplayer session, storing selection locally");
        PlayerPrefs.SetInt("SelectedCharacter", selectedCharacter);
        PlayerPrefs.Save();

        if (NetworkServer.active)
        {
            Debug.Log("Changing Scene to Lobby (Server)");
            NetworkManager.singleton.ServerChangeScene("Lobby");
        }
        else
        {
            Debug.Log("Changing Scene to Lobby (Client)");
            SceneManager.LoadScene("Lobby");
        }
    }

    public void Right()
    {
        Debug.Log("Button Pressed: Right");

        if (characterInstances.Count == 0)
        {
            Debug.LogWarning("No characters available to switch");
            return;
        }

        characterInstances[currentCharacterIndex].SetActive(false);
        currentCharacterIndex = (currentCharacterIndex + 1) % characterInstances.Count;
        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
        CharacterAbilityText.text = characters[currentCharacterIndex].CharacterAbility;
        CharacterGunText.text = characters[currentCharacterIndex].CharacterGun;
    }

    public void Left()
    {
        if (characterInstances.Count == 0)
        {
            Debug.LogWarning("No characters available to switch");
            return;
        }

        characterInstances[currentCharacterIndex].SetActive(false);
        currentCharacterIndex =
            (currentCharacterIndex - 1 + characterInstances.Count) % characterInstances.Count;
        characterInstances[currentCharacterIndex].SetActive(true);
        CharacterNameText.text = characters[currentCharacterIndex].CharacterName;
        CharacterAbilityText.text = characters[currentCharacterIndex].CharacterAbility;
        CharacterGunText.text = characters[currentCharacterIndex].CharacterGun;
    }

    private void UpdateCharacterPanel()
    {
        for (int i = 0; i < characterInfoPanels.Length; i++)
        {
            characterInfoPanels[i].SetActive(i == currentCharacterIndex);
        }
    }
}
