using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CardManager : NetworkBehaviour
{
    public GameObject baseCardPrefab;
    public Transform cardSpawnPosition;
    public mouse mouseScript;
    public GameObject board_go;

    [Header("Host stuff")]
    public GameObject hostCardHolder;
    public GameObject hostSelectedCardHolder;
    public GameObject hostCardSpawnPoint;

    [Header("Client stuff")]
    public GameObject clientCardHolder;
    public GameObject clientSelectedCardHolder;
    public GameObject clientCardSpawnPoint;


    // Assign values to these before spawning to change what we spawn over the network
    public GameObject networkPrefabToSpawn;
    public Transform networkPrefabToSpawnPosition;
    public GameObject recentlySpawnedNetworkPrefab;

    public CardData playersSelectedCardsData;
    public PlayerData playerData;

    // These two network variables are synced over the network. The server can change these at will and they will sync for everyone else automatically but if client wants to change these they have to send a server rpc like normally
    public NetworkVariable<CardData.CardType> hostChosenCard = new NetworkVariable<CardData.CardType>(CardData.CardType.StenCard, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<CardData.CardType> clientChosenCard = new NetworkVariable<CardData.CardType>(CardData.CardType.StenCard, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> hostPoints = new NetworkVariable<int>(0, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> clientPoints = new NetworkVariable<int>(0, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    public TextMeshProUGUI hostAndClientPointsText;

    public TextMeshProUGUI gameResultText;
    // This value is only for the server to keep track of
    private int amountOfPlayersReady = 0;

    float cardSpeed = 0.1f;

    // Define outcomes in a dictionary
    Dictionary<(CardData.CardType, CardData.CardType), string> outcomes = new Dictionary<(CardData.CardType, CardData.CardType), string>
    {
        {(CardData.CardType.StenCard, CardData.CardType.StenCard), "DRAW"},
        {(CardData.CardType.StenCard, CardData.CardType.SaxCard), "WIN"},
        {(CardData.CardType.StenCard, CardData.CardType.PåseCard), "LOOSE"},
        {(CardData.CardType.SaxCard, CardData.CardType.StenCard), "LOOSE"},
        {(CardData.CardType.SaxCard, CardData.CardType.SaxCard), "DRAW"},
        {(CardData.CardType.SaxCard, CardData.CardType.PåseCard), "WIN"},
        {(CardData.CardType.PåseCard, CardData.CardType.StenCard), "WIN"},
        {(CardData.CardType.PåseCard, CardData.CardType.SaxCard), "LOOSE"},
        {(CardData.CardType.PåseCard, CardData.CardType.PåseCard), "DRAW"}
    };


    void Start()
    {
        // These two are attached functions that will trigger whenever the network
        // variable data is changed. Easy to keep track of the values and update gui
        hostPoints.OnValueChanged += HostPointsOnValueChanged;
        clientPoints.OnValueChanged += ClientPointsOnValueChanged;
    }
    // This 
    private void HostPointsOnValueChanged(int previousValue, int newValue)
    {
        print($"TESTIIIIIIIING host new value is: {newValue}");
        UpdateHostClientPointsText();
    }
    private void ClientPointsOnValueChanged(int previousValue, int newValue)
    {
        print($"TESTIIIIIIIING client new value is: {newValue}");
        UpdateHostClientPointsText();
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            // This will run the spawning on the server only :)
            ulong myClientId = NetworkManager.Singleton.LocalClientId; // Get the client's own ID to enable ownershup of card
            SpawnPlayerCardsServerRpc(playerData.clientOrHost, myClientId);
        }
    }
    // Updates the score points to whatever the network variable values currently are
    // These values are synced over the network :)
    public void UpdateHostClientPointsText()
    {
        print($"Updating score for client and host - Host score is: {hostPoints.Value} and client score is: {clientPoints.Value}");
        hostAndClientPointsText.text = $"Scoring\nHost: {hostPoints.Value}\nClient: {clientPoints.Value}";
    }
    // Update with WINNER, DRAW or LOSER text
    public void UpdateGameResultText(String resultTest)
    {
        gameResultText.gameObject.SetActive(true);
        gameResultText.text = resultTest;
    }

    // This should only run locally on server to calculate the win condition
    // And send the results to the client
    private void TryCalculateWinner()
    {
        if (IsServer)
        {
            // Once we reach 2 ready players (both has selected their card) 
            // then we can calucate winner
            if (amountOfPlayersReady >= 2)
            {
                print("PLAYERS READY ARE 2, LETS MAKE SOMEONE WIN");
                print("Host chosen card is: " + hostChosenCard.Value);
                print("client chosen card is: " + clientChosenCard.Value);

                // Fetch and print the outcome based on selected cards for host and client
                string hostResult = outcomes[(hostChosenCard.Value, clientChosenCard.Value)];
                string clientResult = outcomes[(clientChosenCard.Value, hostChosenCard.Value)];

                print($"hostResult: {hostResult}");
                // Show result for Host - Host does it locally, no need for networking
                UpdateGameResultText(hostResult);
                // Send result to client
                SendResultToClientServerRpc(clientResult);
                if (hostResult == "WIN")
                {
                    hostPoints.Value++;
                }
                else if (hostResult == "LOOSE")
                {
                    clientPoints.Value++;
                }
            }
        }

    }

    // Send trigger to update GUI score from server to client
    // Triggers updating the scoring based on the network variable points
    [Rpc(SendTo.NotServer)]
    public void SendScoreGuiUpdateToClientServerRpc()
    {
        print("Sending to client to update score, in SendScoreGuiUpdateToClientServerRpc");
        UpdateHostClientPointsText();
    }
    // Send game results text (WIN, DRAW, LOOSE) from server on win/loose/draw to client
    [Rpc(SendTo.NotServer)]
    public void SendResultToClientServerRpc(String result)
    {
        UpdateGameResultText(result);
    }
    // TODO: Test this, send my update to network variable
    [Rpc(SendTo.Server)]
    public void SendUpdateNetworkVariableServerRpc(CardData.CardType cardType)
    {
        clientChosenCard.Value = cardType;
    }
    // Send to server that we are ready to calculate win condition!
    // WIll only play on server
    [Rpc(SendTo.Server)]
    public void SendPlayerIsReadyToServerRpc()
    {
        print("TEST");
        amountOfPlayersReady++;
        TryCalculateWinner();
    }

    // Host spawns cards on start of game, spawns amount of cards equal to the template cards in scene (currently 4 cards)
    // and assings spawned cards to that position
    [Rpc(SendTo.Server)]
    public void SpawnPlayerCardsServerRpc(string hostOrClient, ulong clientId)
    {
        // Based on string we spawn cards for either host or client
        Transform cardHolder = (hostOrClient == "host") ? hostCardHolder.transform : clientCardHolder.transform;
        Transform cardSpawnPoint = (hostOrClient == "host") ? hostCardSpawnPoint.transform : clientCardSpawnPoint.transform;

        print("Spawning cards for: " + hostOrClient + " for client: " + clientId);

        foreach (Transform card in cardHolder)
        {
            GameObject spawnedCard = Instantiate(baseCardPrefab, cardSpawnPoint.position, cardSpawnPoint.rotation);
            // Set tag to host or client
            var randomCardType = (CardData.CardType)UnityEngine.Random.Range(0, 3); // Upper bound is exclusive

            // Assign ownership to the requesting client or host so we know who owns what card
            NetworkObject netObj = spawnedCard.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(clientId);

            // print("random card type is: " + randomCardType);
            spawnedCard.GetComponent<CardData>().cardType.Value = randomCardType;

            spawnedCard.GetComponent<CardData>().shouldWeUpdateCardGFX.Value = true;
            Vector3 cardGoToPosition = new Vector3(card.transform.position.x, card.transform.position.y, card.transform.position.z);
            // Set base position
            spawnedCard.GetComponent<CardData>().cardBasePosition.Value = cardGoToPosition;

            // Set rotation and then move the cards from spawn point to hand
            spawnedCard.transform.rotation = card.transform.rotation;
            StartCoroutine(MoveCardToPosition(cardGoToPosition, cardSpeed, spawnedCard.transform));
        }
    }
    // Server sends order that every client should run move locally
    // this needs to be ordered by server since its a coroutine... fett störande :)))
    [Rpc(SendTo.Everyone)]
    public void SendMoveCardToClientsServerRpc(Vector3 Gotoposition, float duration, NetworkObjectReference cardToMoveNetworkObject)
    {
        if (cardToMoveNetworkObject.TryGet(out NetworkObject cardToMoveObject))
        {
            StartCoroutine(MoveCardToPosition(Gotoposition, duration, cardToMoveObject.transform));
        }
        else
        {
            print("COULD NOT FIND NETWORK OBJECT ON CARD");
        }

    }
    // Clients can call this to request a move of card to be performed on the server
    [Rpc(SendTo.Server)]
    public void RequestServerToMoveCardServerRpc(Vector3 Gotoposition, float duration, NetworkObjectReference cardToMoveNetworkObject)
    {
        SendMoveCardToClientsServerRpc(Gotoposition, duration, cardToMoveNetworkObject);
    }
    // TODO: SERVER Need to run this when repositioning the card(s???) to their original position
    void updatePos(Transform card_place, float startPosY, List<GameObject> cardsList)
    {
        for (int i = 0; i < cardsList.Count; i++)
        {
            float s = card_place.position.x - (0.5f * cardsList.Count) + 0.5f;
            StartCoroutine(MoveCardToPosition(new Vector3(s + i, startPosY, card_place.position.z + (i * -0.005f)), cardSpeed, cardsList[i].transform));
        }
    }

    IEnumerator MoveCardToPosition(Vector3 Gotoposition, float duration, Transform cardTransform)
    {
        float elapsedTime = 0;
        Vector3 currentPos = cardTransform.position;

        while (elapsedTime < duration)
        {
            if (cardTransform)
            {
                cardTransform.position = Vector3.Lerp(currentPos, Gotoposition, (elapsedTime / duration));
            }
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        if (cardTransform)
            cardTransform.position = Gotoposition;
        yield return null;
    }

    // Select clicked card from pre defined prefabs
    public void SelectCard(CardData selectedCardData)
    {
        string hostOrClient = "";
        // Quick dirty way to differentiate between if we are server or not
        if (IsServer)
        {
            hostOrClient = "host";
            print("WE ARE SERVER");
            hostChosenCard.Value = selectedCardData.cardType.Value;
        }
        else
        {
            hostOrClient = "client";
            SendUpdateNetworkVariableServerRpc(selectedCardData.cardType.Value);
            print("WE ARE CLIENT, sending update to server! we are not allowed");
        }
        // Differentiate between host and client where to spawn the selected card
        GameObject selectedCardHolder = (hostOrClient == "host") ? hostSelectedCardHolder : clientSelectedCardHolder;

        // Set the card to spawn and spawn it
        networkPrefabToSpawn = baseCardPrefab;
        SpawnSelectedCardRpc(selectedCardHolder.transform.position, selectedCardHolder.transform.rotation, selectedCardData.cardType.Value);

        // Send message to server that you are ready!
        // SendPlayerIsReadyToServerRpc();
    }

    // TODO: THIS MIGHT NEED SOME CHANGE 
    // Spawn the card which the player selected
    [Rpc(SendTo.Server)]
    private void SpawnSelectedCardRpc(Vector3 position, Quaternion rotation, CardData.CardType cardType)
    {
        GameObject spawnedCard = Instantiate(networkPrefabToSpawn, position, rotation);
        spawnedCard.transform.position = position;
        spawnedCard.transform.rotation = rotation;
        // Assign the data to the card based on its type
        spawnedCard.GetComponent<NetworkObject>().Spawn();
        spawnedCard.GetComponent<CardData>().cardType.Value = cardType;
        spawnedCard.GetComponent<CardData>().shouldWeUpdateCardGFX.Value = true;
    }
}
