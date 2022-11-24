using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnClickGenericEvent : MonoBehaviour, IPointerDownHandler {

    public CharacterData charHolder;

    public void OnPointerDown(PointerEventData eventData)
    {
        BattleManager.Instance.SelectCharacter(charHolder);
    }
}
