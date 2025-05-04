using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyCard : MonoBehaviour
{

    public string lobbyCode;
    public TestLobby testLobby;
    public string lobbyName;
    public string lobbyMaxPlayers;
    public List<string> playerNames = new List<string>();
    public Transform lobbyPlayersListHolder;
    public GameObject prefabPlayerListText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        testLobby = GameObject.Find("TestLobby").GetComponent<TestLobby>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void PressJoin()
    {
        print("Trtying to join lobby by code!");
        testLobby.JoinLobbyByCode(lobbyCode);
    }

    public void AddPlayersToLobbyCard(List<string> playerNamesToAdd)
    {
        foreach (string playerName in playerNamesToAdd)
        {
            playerNames.Add(playerName);
        }
    }
    public void PopulatePlayerNamesList()
    {
        foreach (var playerName in playerNames)
        {
            var createdLobbyPlayerText = Instantiate(prefabPlayerListText, lobbyPlayersListHolder.transform);
            createdLobbyPlayerText.GetComponent<TextMeshProUGUI>().text = playerName;
        }
    }

}
