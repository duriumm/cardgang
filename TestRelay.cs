using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    public string joinCodeThisUser;
    public TextMeshProUGUI joinCodeThisUserText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        // This will attach listener to signed in, so triggers everytime someone signs in 
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        // TODO: This happens now in TestLobby.cs instead. Might not need?
        // await AuthenticationService.Instance.SignInAnonymouslyAsync();

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {

        }
    }

    // This is for creating the Relay (server creation???) for the host??
    public async Task<string> CreateRelay()
    {
        try
        {
            // Two players allowed only
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeThisUser = joinCode;
            joinCodeThisUserText.text = joinCodeThisUser;
            Debug.Log("Join code: " + joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start game as host
            NetworkManager.Singleton.StartHost();

            // return joincode to use for clients
            return joinCode;

        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return "";
        }
    }
    // Joining the host with client??
    public async Task JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
