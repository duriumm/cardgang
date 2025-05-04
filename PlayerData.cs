using UnityEngine;
using Unity.Netcode;


public class PlayerData : NetworkBehaviour
{
    public string clientOrHost;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Setting the string value on joining / hosting server
    public void SetClientOrHostConnection()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("I am the Host (Client + Server).");
            clientOrHost = "host";
        }
        else if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("I am the Server.");
            clientOrHost = "host";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("I am a Client.");
            clientOrHost = "client";
        }
        else
        {
            Debug.Log("I am NOT connected to the network.");
        }
    }
}
