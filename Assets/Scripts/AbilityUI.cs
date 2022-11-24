using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityUI : MonoBehaviour, IPointerDownHandler
{
    public Text abilityText;
    public int abilityIndex = 0;

    public bool isSelected;

    public void Init(string abilityName)
    {
        abilityText.text = abilityName;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var charData = BattleManager.Instance.currentCharacter;

        UIManager.Instance.DeselectOtherAbilityUIs(this);

        for (int i = 0; i < charData.characterData.characterAbilities.Count; i++)
        {
            if (abilityIndex.Equals(i))
            {
                if (isSelected)
                {
                    //BattleManager.Instance.currentCharacter.characterData.Attack(charData.characterAbilities[i]);
                    if (charData.characterData.CanQueueAttack)
                        charData.attackQueue = StartCoroutine(charData.characterData.QueueAttack(charData.characterData.characterAbilities[i]));
                    else
                        Debug.Log("attack queue not null, could not attack");

                    isSelected = false;
                }
                else
                {
                    UIManager.Instance.SetManaNeededUI(charData.characterData.characterAbilities[i].manaCost, charData.characterData.manaCurrentPoints);
                    isSelected = true;
                }
                
                break;
            }
        }
    }
}
