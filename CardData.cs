using UnityEngine;
using System;
using TMPro;
using System.Linq;
using Unity.Netcode;

public class CardData : NetworkBehaviour
{
    public enum CardType
    {
        StenCard,
        SaxCard,
        PåseCard,
        BacksideCard
    }
    // public CardType cardType;
    // Making cardType a NetworkVariable to sync it across the network
    public NetworkVariable<CardType> cardType = new NetworkVariable<CardType>(CardType.BacksideCard);

    public Material[] materialList;
    public TextMeshPro cardTypeText;
    public MeshRenderer cardTypeIcon;

    public NetworkVariable<bool> shouldWeUpdateCardGFX = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> randomizeMaterialOnStart = new NetworkVariable<bool>(false);
    public NetworkVariable<Vector3> cardBasePosition = new NetworkVariable<Vector3>();


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Start listening when shouldWeUpdateCardGFX is changed 
        shouldWeUpdateCardGFX.OnValueChanged += UpdateCardGFX;
    }

    private void UpdateCardGFX(bool oldValue, bool newValue)
    {
        if (newValue == true) { UpdateCardDisplay(cardType.Value); }
    }

    private void UpdateCardDisplay(CardType cardTypeInput)
    {
        // var cardMaterial = GetComponent<Renderer>().material;
        switch (cardTypeInput)
        {

            // Currently with henkecard we only use material, not any text on card FYI so i comment this out
            case CardType.StenCard:
                // cardTypeText.text = "Sten";
                GetComponent<Renderer>().material = materialList.FirstOrDefault(m => m.name == "rock");
                break;
            case CardType.SaxCard:
                // cardTypeText.text = "Sax";
                GetComponent<Renderer>().material = materialList.FirstOrDefault(m => m.name == "scissors");
                break;
            case CardType.PåseCard:
                // cardTypeText.text = "Påse";
                GetComponent<Renderer>().material = materialList.FirstOrDefault(m => m.name == "paper");
                break;
            case CardType.BacksideCard:
                // cardTypeText.text = "";
                GetComponent<Renderer>().material = materialList.FirstOrDefault(m => m.name == "Backside_Cardmat");
                break;
        }
    }
}
