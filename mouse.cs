using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems; // Required for IPointer interfaces
public class mouse : NetworkBehaviour
{
    public GameObject card;
    public GameObject board_go;

    board boardScript;

    public List<GameObject> cards = new List<GameObject>();

    public GameObject highlightedCard = null;

    bool isSelectedCard = false;

    float startPosY;

    float cardSpeed = 0.1f;

    public Transform card_place;

    float cardSpaceing = 0.1f;

    NetworkObject thisNetworkObject;
    public CardManager cardManager;
    private Vector3 cardBasePosition;

    void Start()
    {
        thisNetworkObject = gameObject.GetComponent<NetworkObject>();
    }

    // Update is called once per framee
    void Update()
    {
        // Drag the highlighted card
        if (Input.GetMouseButton(0) && highlightedCard)
        {
            Vector3 mousePos = Input.mousePosition;
            float cardDist = (10 * (Input.mousePosition.y + 1) / Screen.height);
            if (cardDist < 3.5f)
                cardDist = 3.5f;

            mousePos.z = Camera.main.nearClipPlane + cardDist;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
            highlightedCard.transform.position = worldPosition;
            if (!isSelectedCard)
                StartCoroutine(rotateTo(new Vector3(90, 0, 0), 0.2f, highlightedCard.transform));
            isSelectedCard = true;
        }
        // User stops interracting with card
        else
        {
            if (isSelectedCard)
            {
                if (highlightedCard)
                    StartCoroutine(rotateTo(card_place.rotation.eulerAngles, 0.2f, highlightedCard.transform));

                if (Input.mousePosition.y > Screen.height * 0.8f || Input.mousePosition.y < Screen.height * 0.35f || Input.mousePosition.x > Screen.width * 0.8f || Input.mousePosition.x < Screen.width * 0.2f)
                    updatePos();
                else
                {
                    if (cards.Count > 0)
                    {
                        for (int i = 0; i < cards.Count; i++)
                        {
                            if (ReferenceEquals(cards[i], highlightedCard))
                            {
                                // Card is played
                                boardScript.addCard(cards[i], (int)cards[i].GetComponent<cardInfo>().cardType);
                                Destroy(cards[i]);
                                cards.RemoveAt(i);
                                updatePos();
                                break;
                            }
                        }
                    }
                }
            }

            isSelectedCard = false;
        }

        // Hover over card and highlight it
        if (!isSelectedCard)// Needs work
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            bool isHitGameObjectOwnedByMe = true;
            if (Physics.Raycast(ray, out hit, 100))
            {
                GameObject hitCard = hit.transform.gameObject;

                if (hitCard.tag == "card")
                {
                    // Get base card position so we know where to return the card to
                    cardBasePosition = hitCard.GetComponent<CardData>().cardBasePosition.Value;
                }
                if (hit.transform.tag == "card" && !hitCard.GetComponent<NetworkObject>().IsOwner)
                {
                    print("We are NOT the owner of this card: " + hitCard.name + " , returning");
                    isHitGameObjectOwnedByMe = false;
                }
                // Return of we do NOT own the gameobject so we cant pick up opponents cards
                if (!isHitGameObjectOwnedByMe) { return; }

                // If the cursor leaves the currently highlighted card, move card back to base position
                if (highlightedCard != null && highlightedCard != hit.transform.gameObject)
                {
                    print("Cursor left card: " + highlightedCard.name);

                    // Move the previous card back to its original position
                    Vector3 goToPosition = new Vector3(cardBasePosition.x, cardBasePosition.y, cardBasePosition.z);
                    var prevCardNetworkObject = highlightedCard.GetComponent<NetworkObject>();
                    cardManager.RequestServerToMoveCardServerRpc(goToPosition, cardSpeed, prevCardNetworkObject);

                    highlightedCard = null;
                }

                // If hovering over a new card, move it up
                if (hit.transform.tag == "card" && highlightedCard == null)
                {
                    print("Card is hovered: " + hitCard.name);
                    Vector3 goToPosition = new Vector3(cardBasePosition.x, cardBasePosition.y + 1, cardBasePosition.z);

                    var cardNetworkObject = hitCard.GetComponent<NetworkObject>();
                    cardManager.RequestServerToMoveCardServerRpc(goToPosition, cardSpeed, cardNetworkObject);
                    highlightedCard = hit.transform.gameObject;
                }
            }
            else // If Raycast doesn't hit anything, reset the highlighted card
            {
                if (highlightedCard != null)
                {
                    print("Cursor left all cards, resetting position");

                    // Move the previous card back to its original position
                    Vector3 goToPosition = new Vector3(highlightedCard.transform.position.x, highlightedCard.transform.position.y - 1, highlightedCard.transform.position.z);
                    var prevCardNetworkObject = highlightedCard.GetComponent<NetworkObject>();
                    cardManager.RequestServerToMoveCardServerRpc(goToPosition, cardSpeed, prevCardNetworkObject);

                    highlightedCard = null; // Reset highlighted card
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            addCard(Random.Range(0, 3));
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (cards.Count > 0)
            {
                Destroy(cards[cards.Count - 1]);
                cards.RemoveAt(cards.Count - 1);
                updatePos();
            }
        }
    }

    void updatePos()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            float s = card_place.position.x - (0.5f * cards.Count) + 0.5f;
            StartCoroutine(move(new Vector3(s + i, startPosY, card_place.position.z + (i * -0.005f)), cardSpeed, cards[i].transform));
        }
    }

    void addCard(int card_type)
    {
        GameObject temp = Instantiate(card, new Vector3(transform.localPosition.x, startPosY, transform.localPosition.z + 4), card_place.rotation);
        temp.GetComponent<NetworkObject>().Spawn();
        // temp.GetComponent<cardInfo>().cardType = (cardInfo.CardType)card_type;
        cards.Add(temp);
        updatePos();
    }

    IEnumerator move(Vector3 Gotoposition, float duration, Transform cardTransform)
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

    IEnumerator rotateTo(Vector3 Gotoposition, float duration, Transform cardTransform)
    {
        float elapsedTime = 0;
        Vector3 currentPos = cardTransform.position;

        while (elapsedTime < duration)
        {
            if (cardTransform)
            {
                cardTransform.rotation = Quaternion.Euler(Vector3.Lerp(currentPos, Gotoposition, (elapsedTime / duration)));
            }
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        if (cardTransform)
            cardTransform.rotation = Quaternion.Euler(Gotoposition);
        yield return null;
    }
}
