using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class CardManager : MonoBehaviour
{
    public static CardManager Inst { get; private set;}
    void Awake() => Inst = this;

    [SerializeField] ItemSO itemSO;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] List<Card> myCards;
    [SerializeField] List<Card> otherCards;
    [SerializeField] Transform cardSpawnPoint;
    [SerializeField] Transform myCardLeft;
    [SerializeField] Transform myCardRight;
    [SerializeField] Transform otherCardLeft;
    [SerializeField] Transform otherCardRight;
    [SerializeField] ECardState eCardState;

    List<Item> itemBuffer;
    Card selectCard;
    bool isMyCardDrag;
    bool onMyCardArea;
    enum ECardState { Nothing, CanMouseOver, CanMouseDrag }

    public Item PopItem()
    {
        if (itemBuffer.Count == 0)
        {
            SetupItemBuffer();
        }
        Item item = itemBuffer[0];
        itemBuffer.RemoveAt(0);
        return item;
    }
    void SetupItemBuffer()
    {
        itemBuffer = new List<Item>(100);
        for (int i = 0; i < itemSO.items.Length; i++)
        {
            Item item = itemSO.items[i];
            for (int j = 0; j < item.percent;j++)
            {
                itemBuffer.Add(item);
            }
        }

        for (int i = 0; i < itemBuffer.Count; i++)
        {
            int rand = Random.Range(i, itemBuffer.Count);
            Item temp = itemBuffer[i];
            itemBuffer[i] = itemBuffer[rand];
            itemBuffer[rand] = temp;
        }
    }
    void Start()
    {
        SetupItemBuffer();
        TurnManager.OnAddCard += AddCard;
    }

    void OnDestroy()
    {
        TurnManager.OnAddCard -= AddCard;
    }

    void Update()
    {
        if (isMyCardDrag)
        {
            CardDrag();
        }

        DetectCardArea();
        SetECardState();
    }

    void AddCard(bool isMine)
    {
        var cardObject = Instantiate(cardPrefab, cardSpawnPoint.position, Utils.QI);
        var card = cardObject.GetComponent<Card>();
        card.Setup(PopItem(), isMine);
        (isMine ? myCards : otherCards).Add(card);

        SetOriginOrder(isMine);
        CardAlignment(isMine);
    }
    void SetOriginOrder(bool isMine)
    {
        int count = isMine ? myCards.Count : otherCards.Count;
        for (int i = 0; i < count; i++)
        {
            var targetCard = isMine ? myCards[i] : otherCards[i];
            targetCard?.GetComponent<Order>().SetOriginOrder(i);
        }
    }
    
    void CardAlignment(bool isMine)
    {
        List<PRS> originCardPRSs = new List<PRS>();
        if (isMine)
        {
            originCardPRSs = RoundAlignment(myCardLeft, myCardRight, myCards.Count, 0.5f, Vector3.one * 1.9f);
        }
        else
        {
            originCardPRSs = RoundAlignment(otherCardLeft, otherCardRight, otherCards.Count, -0.5f, Vector3.one * 1.9f);
        }
        var targetCards = isMine ? myCards : otherCards;
        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];

            targetCard.originPRS = originCardPRSs[i];
            targetCard.MoveTransform(targetCard.originPRS, true, 0.7f);
        }
    }

    List<PRS> RoundAlignment(Transform leftTr, Transform rightTr, int objCount, float height, Vector3 scale)
    {
        float[] objLerps = new float[objCount];
        List<PRS> results = new List<PRS>(objCount);

        switch (objCount)
        {
            case 1: objLerps = new float[] { 0.5f }; break;
            case 2: objLerps = new float[] { 0.27f, 0.73f }; break;
            case 3: objLerps = new float[] { 0.1f, 0.5f, 0.9f }; break;
            default:
                float interval = 1f / (objCount - 1);
                for (int i = 0; i < objCount; i++)
                {
                    objLerps[i] = interval * i;
                }
                break;
        }

        for (int i = 0; i < objCount; i++)
        {
            var targetPos = Vector3.Lerp(leftTr.position, rightTr.position, objLerps[i]);
            var targetRot = Utils.QI;
            if (objCount >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(height, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));
                curve = height >= 0 ? curve : -curve;
                targetPos.y += curve;
                targetRot = Quaternion.Slerp(leftTr.rotation, rightTr.rotation, objLerps[i]);

            }
            results.Add(new PRS(targetPos, targetRot, scale));
        }
        return results;
    }

    #region MyCard
    public void CardMouseOver(Card card)
    {
        if (eCardState == ECardState.Nothing)
        {
            return;
        }
        selectCard = card;
        EnlargeCard(true, card);
    }

    public void CardMouseExit(Card card)
    {
        EnlargeCard(false, card);
    }

    public void CardMouseDown()
    {
        if (eCardState != ECardState.CanMouseDrag)
        {
            return;
        }
        isMyCardDrag = true;
    }

    public void CardMouseUp()
    {
        isMyCardDrag = false;

        if (eCardState != ECardState.CanMouseDrag)
        {
            return;
        }
    }

    void CardDrag()
    {
        if (!onMyCardArea)
        {
            selectCard.MoveTransform(new PRS(Utils.MousePos, Utils.QI, selectCard.originPRS.scale), false);
        }
    }

    void DetectCardArea()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(Utils.MousePos, Vector3.forward);
        int layer = LayerMask.NameToLayer("MyCardArea");
        onMyCardArea = Array.Exists(hits, x => x.collider.gameObject.layer == layer);
    }

    void EnlargeCard(bool isEnlarge, Card card)
    {
        if (isEnlarge)
        {
            Vector3 enlargePos = new Vector3(card.originPRS.pos.x, -7.8f, -100f);
            card.MoveTransform(new PRS(enlargePos, Utils.QI, Vector3.one * 2.5f), false);
        }
        else
        {
            card.MoveTransform(card.originPRS, false);
        }
        card.GetComponent<Order>().SetMostFrontOrder(isEnlarge);
    }

    void SetECardState()
    {
        if (TurnManager.Inst.isLoading)
        {
            eCardState = ECardState.Nothing;
            Debug.Log("Nothing");
        }
        else if (!TurnManager.Inst.myTurn)
        {
            eCardState = ECardState.CanMouseOver;
            Debug.Log("CanMouseOver");
        }
        else if (TurnManager.Inst.myTurn)
        {
            eCardState = ECardState.CanMouseDrag;
            Debug.Log("CanMouseDrag");
        }
    }

    #endregion
}
