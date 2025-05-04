// This script is for the host, client and server buttons only :)
using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace HelloWorld
{
    public class NetworkGUI : MonoBehaviour
    {
        VisualElement rootVisualElement;
        Button hostButton;
        Button clientButton;
        Button serverButton;
        Button moveButton;
        Label statusLabel;
        public TestRelay relay;
        public String joinCode;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            rootVisualElement = uiDocument.rootVisualElement;

            hostButton = CreateButton("HostButton", "Host");
            clientButton = CreateButton("ClientButton", "Client");
            // serverButton = CreateButton("ServerButton", "Server");
            moveButton = CreateButton("MoveButton", "Move");
            statusLabel = CreateLabel("StatusLabel", "Not Connected");

            rootVisualElement.Clear();
            rootVisualElement.Add(hostButton);
            rootVisualElement.Add(clientButton);
            // Not using server at the moment. Host is Server and client so :)
            // rootVisualElement.Add(serverButton);
            rootVisualElement.Add(moveButton);
            rootVisualElement.Add(statusLabel);

            hostButton.clicked += OnHostButtonClicked;
            clientButton.clicked += OnClientButtonClicked;
            // serverButton.clicked += OnServerButtonClicked;
        }

        void Update()
        {
            UpdateUI();
        }

        void OnDisable()
        {
            hostButton.clicked -= OnHostButtonClicked;
            clientButton.clicked -= OnClientButtonClicked;
            // serverButton.clicked -= OnServerButtonClicked;
        }

        async void OnHostButtonClicked()
        {
            await relay.CreateRelay();
        }


        async void OnClientButtonClicked()
        {
            await relay.JoinRelay(joinCode);
        }

        void OnServerButtonClicked() => NetworkManager.Singleton.StartServer();

        // Disclaimer: This is not the recommended way to create and stylize the UI elements, it is only utilized for the sake of simplicity.
        // The recommended way is to use UXML and USS. Please see this link for more information: https://docs.unity3d.com/Manual/UIE-USS.html
        private Button CreateButton(string name, string text)
        {
            var button = new Button();
            button.name = name;
            button.text = text;
            button.style.width = 240;
            button.style.backgroundColor = Color.white;
            button.style.color = Color.black;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        private Label CreateLabel(string name, string content)
        {
            var label = new Label();
            label.name = name;
            label.text = content;
            label.style.color = Color.black;
            label.style.fontSize = 18;
            return label;
        }

        void UpdateUI()
        {
            if (NetworkManager.Singleton == null)
            {
                SetStartButtons(false);
                SetMoveButton(false);
                SetStatusText("NetworkManager not found");
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                SetStartButtons(true);
                SetMoveButton(false);
                SetStatusText("Not connected");
            }
            else
            {
                SetStartButtons(false);
                SetMoveButton(true);
                UpdateStatusLabels();
            }
        }

        void SetStartButtons(bool state)
        {
            hostButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            clientButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            // serverButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void SetMoveButton(bool state)
        {
            moveButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            if (state)
            {
                moveButton.text = NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change";
            }
        }

        void SetStatusText(string text) => statusLabel.text = text;

        void UpdateStatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";
            string transport = "Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name;
            string modeText = "Mode: " + mode;
            SetStatusText($"{transport}\n{modeText}");
        }
    }
}