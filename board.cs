using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class board : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();
    public List<GameObject> enemy_cards = new List<GameObject>();
    public GameObject test;
    float cardSpeed = 0.2f;
    float cardOffset = 2;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void updatePos()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            float s = transform.position.x - (0.5f * cards.Count) + 0.5f;
            StartCoroutine(move(new Vector3( s + i, transform.position.y, transform.position.z - cardOffset), cardSpeed, cards[i].transform));
        }
    }

    void updateEnemyPos()
    {
        for (int i = 0; i < enemy_cards.Count; i++)
        {
            float s = transform.position.x - (0.5f * enemy_cards.Count) + 0.5f;
            StartCoroutine(move(new Vector3(s + i, transform.position.y, transform.position.z + cardOffset), cardSpeed, enemy_cards[i].transform));
        }
    }

    public void deleteCard(int i)
    {
        if (i >= 0)
        {
            Destroy(cards[i]);
            cards.RemoveAt(i);
            updatePos();
        }
    }

    public void deleteEnemyCard(int i)
    {
        if (i >= 0)
        {
            Destroy(enemy_cards[i]);
            enemy_cards.RemoveAt(i);
            updateEnemyPos();
        }
    }

    public void addCard(GameObject card, int card_type)
    {
        GameObject temp = Instantiate(card, card.transform.position, transform.rotation);
        temp.GetComponent<cardInfo>().cardType = (cardInfo.CardType)card_type;
        temp.transform.tag = "Untagged";
        
        for (int i = 0; i <= cards.Count; i++)
        {
            if (cards.Count == i)
            {
                cards.Add(temp);
                break;
            }
            if (cards[i].transform.position.x > temp.transform.position.x)
            {
                cards.Insert(i, temp);
                break;
            }
        }
        updatePos();
    }

    public void addEnemyCard(int card_type, Vector3 pos)
    {
        GameObject temp = Instantiate(test, pos, transform.rotation);
        temp.GetComponent<cardInfo>().cardType = (cardInfo.CardType)card_type;
        temp.transform.tag = "Untagged";

        for (int i = 0; i <= enemy_cards.Count; i++)
        {
            if (enemy_cards.Count == i)
            {
                enemy_cards.Add(temp);
                break;
            }
            if (enemy_cards[i].transform.position.x > temp.transform.position.x)
            {
                enemy_cards.Insert(i, temp);
                break;
            }
        }
        updateEnemyPos();
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
}
