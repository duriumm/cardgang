using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using Newtonsoft.Json;
using System;  // You need to install Newtonsoft.Json package for this
using System.Linq; // Required for .Last()

public class TestLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float lobbyUpdateTimer;
    private float heartBeatTimer;
    // ENTER THE JOIN CODE HERE IN THE INSPECTOR AND THEN PRESS J INGAME TO JOIN
    public string joinLobbyCode;
    public string playerName;
    public string testUpdatePlayerName;
    public Transform lobbyBackground;
    public GameObject prefabLobbyCard;
    public TestRelay testRelay;
    public GameObject hostCamera;
    public GameObject clientCamera;
    public GameObject lobbyCamera;
    public GameObject lobbyGUI;
    public PlayerData playerData;
    public GameObject gameHolder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        // Listener for when a user signs in
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerName = "Player-" + UnityEngine.Random.Range(10, 99);
        Debug.Log(playerName);
    }
    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>{
                {
                    "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                }
            }
        };
    }
    // Makes sure the lobby does not DC after x amount of seconds of inactivity
    // By sending a heartbeat keeping it alive
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0f)
            {
                float heartBeatTimerMax = 15;
                heartBeatTimer = heartBeatTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    // Triggers when a player joins your lobby. Will only be executed for the host
    private async void OnLobbyChanged(ILobbyChanges changes)
    {
        print("SOMETHING CHANGED ON LOBBY! ACTING WITH APPLY TO LOBBY");
        changes.ApplyToLobby(hostLobby); // This seems to re-render the lobby gui?? unsure how it can affect client and host
        if (changes.PlayerJoined.Changed)
        {
            print("--------- PLAYER JOINED LOBBY");

            Player joinedPlayer = changes.PlayerJoined.Value.Last().Player;
            Debug.Log("Joined player name: " + joinedPlayer.Data["PlayerName"].Value);

            // Convert the List of Player Names to a JSON string
            List<string> namesList = new List<string> { };
            List<Player> players = joinedLobby.Players; // Since im lobby host, this is MY lobby here

            // Add all existing player in the lobby to our list of players
            foreach (Player player in players)
            {
                namesList.Add(player.Data["PlayerName"].Value);
            }
            // Add the joining player in to our list of players
            namesList.Add(joinedPlayer.Data["PlayerName"].Value);

            // Save serialized player names as json string to easier deserialize as separate 
            // objects, since we can only use string here
            string playerNamesJson = JsonConvert.SerializeObject(namesList);
            var lobbyPlayerNamesDataUpdate = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "PlayerNames", new DataObject(DataObject.VisibilityOptions.Public, playerNamesJson) }
                }
            };

            // Update the lobby data with player names
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, lobbyPlayerNamesDataUpdate);

            RefreshLobbyList();
        }
    }

    // Creating a lobby puts you inside that lobby as a single joiner
    public async void CreateLobby()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 2;

            // We can use these lobby options to create private lobbies for example like here below
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                // Creating a Player object with chosen visibility and name inside GetPlayer()
                IsPrivate = false, // Lobby is NOT private
                Player = GetPlayer(), // Create a player when creating the lobby
                Data = new Dictionary<string, DataObject>{ // Set lobby data as game mode to be "Duel"
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Duel")},
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public, "WoodenTable")},
                    {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, "PLACEHOLDER")} // We update the lobby code after this
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            // Assign the created lobby to our Lobby variable as host lobby
            hostLobby = lobby;
            joinedLobby = hostLobby; // Just to keep a reference of the lobby that we just joined
            Debug.Log("Created lobby! " + joinedLobby.Name + " " + joinedLobby.MaxPlayers + " " + joinedLobby.Id + " " + joinedLobby.LobbyCode);

            // Get the player that created the lobby
            Player player = joinedLobby.Players[0];

            // Update the created lobbys metadata with lobby code so client can get it afterwards
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>{
                    { "LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, joinedLobby.LobbyCode)}
                }
            });


            // Convert the List of Player Names to a JSON string
            string[] namesArray = { playerName };
            string playerNamesJson = JsonConvert.SerializeObject(namesArray);

            // UPDATING LOBBY DATA WITH JSON LIST OF NAMES
            var lobbyPlayerNamesDataUpdate = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "PlayerNames", new DataObject(DataObject.VisibilityOptions.Public, playerNamesJson) }
                }
            };

            // Update the lobby data with player names
            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, lobbyPlayerNamesDataUpdate);

            // // Refresh lobby list after creation so we see our newly created lobby in list
            RefreshLobbyList();

            // Create relay and start session as host
            string gameSessionJoinCode = await testRelay.CreateRelay();


            // UPDATING LOBBY DATA WITH game sessions join code
            var gameSessionJoinCodeLobbyData = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameSessionJoinCode", new DataObject(DataObject.VisibilityOptions.Public, gameSessionJoinCode) }
                }
            };
            // Update the lobby data with game session join code
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, gameSessionJoinCodeLobbyData);

            // This should subscribe us
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            try
            {
                var m_LobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(joinedLobby.Id, callbacks);
            }
            catch (LobbyServiceException ex)
            {
                print("Woppsie somerthing broke");
            }

            lobbyCamera.SetActive(false);
            hostCamera.SetActive(true);
            lobbyGUI.SetActive(false);
            gameHolder.SetActive(true);
            playerData.SetClientOrHostConnection();

        }
        catch (LobbyServiceException e)
        {

            Debug.Log(e);
        }
    }
    public List<string> GetPlayerNamesFromLobbyData(Lobby lobby)
    {
        if (lobby.Data.ContainsKey("PlayerNames"))
        {
            // Retrieve the JSON string from the lobby data
            string playerNamesJson = lobby.Data["PlayerNames"].Value;

            // Log the raw JSON to inspect its format
            Debug.Log($"Player Names JSON: {playerNamesJson}");
            try
            {
                // Deserialize the JSON string back to a List<string>
                List<string> playerNames = JsonConvert.DeserializeObject<List<string>>(playerNamesJson);
                // Return the list of player names
                return playerNames;
            }
            catch (JsonException e)
            {
                Debug.LogError($"Failed to deserialize player names: {e.Message}");
                return new List<string>(); // Return an empty list if deserialization fails
            }
        }
        else
        {
            Debug.LogWarning("Player names not found in lobby data!");
            return new List<string>(); // Return an empty list if no data is found
        }
    }
    public async void RefreshLobbyList()
    {

        try
        {
            // Delete current list of lobby gui before populating it with new set
            foreach (Transform child in lobbyBackground)
            {
                GameObject lobbyCard = child.gameObject;
                // Debug.Log($"Found lobby card: {lobbyCard.name}");
                Destroy(lobbyCard);
            }
            // QUery to find ALL lobbies
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            // Debug.Log("Lobbies found! " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                // Create GUI elements for every found lobby
                var createdLobbyCard = Instantiate(prefabLobbyCard, lobbyBackground.transform);
                LobbyCard lobbyCard = createdLobbyCard.GetComponent<LobbyCard>();
                lobbyCard.lobbyName = lobby.Name;
                lobbyCard.lobbyMaxPlayers = lobby.MaxPlayers.ToString();
                lobbyCard.lobbyCode = lobby.Data["LobbyCode"].Value;

                // Playernames from the lobby object deserialized
                List<string> playerNames = GetPlayerNamesFromLobbyData(lobby);
                // Add players to lobby card
                lobbyCard.AddPlayersToLobbyCard(playerNames);
                // Populate players in lobby card object
                lobbyCard.PopulatePlayerNamesList();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    // Function to update game mode at runtime if needed
    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>{
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode)}
                }
            });
            joinedLobby = hostLobby; // To keep reference of current joined lobby
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    public async Task ListLobbies()
    {
        try
        {
            // // Query to get the lobbies of choice!
            // QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            // {
            //     Count = 25,
            //     Filters = new List<QueryFilter>{
            //         new QueryFilter(
            //             QueryFilter.FieldOptions.AvailableSlots,
            //             "0",
            //             QueryFilter.OpOptions.GT // GT = Greater Than - SFilter shows all lobbies with greater than 0 available slots
            //         )
            //     },
            //     Order = new List<QueryOrder>{
            //         // Not ascending, Oldest to newest created query
            //         new QueryOrder(false, QueryOrder.FieldOptions.Created)
            //     }
            // };

            // QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found! " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value + " Lobbycode: " + lobby.LobbyCode);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            print("wopsuie");
        }
    }
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby; // To keep reference to lobby you joined
            Debug.Log("Joined lobby with code " + lobbyCode);

            // Here we could populate our local list of player only
            RefreshLobbyList();

            PrintPlayers(lobby);

            // Join the hosts session as a client
            await testRelay.JoinRelay(joinedLobby.Data["GameSessionJoinCode"].Value);

            lobbyCamera.SetActive(false);
            clientCamera.SetActive(true);
            lobbyGUI.SetActive(false);
            gameHolder.SetActive(true);
            playerData.SetClientOrHostConnection();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    // Just joins first available lobby quickly 
    public async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();

            Debug.Log("Quickjoined lobby!");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    // Just joins first available lobby quickly 
    public void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in lobby " + lobby.Name);
        foreach (Player player in lobby.Players)
        {
            Debug.Log("Player name: " + player.Data["PlayerName"].Value);
        }
    }
    // This NEEDS to be called to update the lobby data values over the network
    // Otherwise it only updates on the user that did the update
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }
    }
    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            playerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>{
                    {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
                }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    private void Update()
    {
        // Keep lobby alive
        HandleLobbyHeartbeat();
        // if someone updates the lobby data, this will run every 1.1 sec to update this value over the network
        HandleLobbyPollForUpdates();
        if (Input.GetKeyDown(KeyCode.L))
        {
            ListLobbies();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            CreateLobby();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            JoinLobbyByCode(joinLobbyCode);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            QuickJoinLobby();
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            UpdatePlayerName(testUpdatePlayerName);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            LeaveLobby();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            CleanupLobbies();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            RefreshLobbyList();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintPlayers(joinedLobby);
        }
    }
    public async Task CleanupLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();

            foreach (Lobby lobby in response.Results)
            {
                Debug.Log($"Deleting lobby: {lobby.Name} (ID: {lobby.Id})");
                await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
            }

            Debug.Log("All stale lobbies deleted.");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to delete lobbies: {e.Message}");
        }
    }
    public void StartGame()
    {

    }
}

