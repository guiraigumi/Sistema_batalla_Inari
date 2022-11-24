using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Transform rowHolder;
    public Transform nameHolder;
    private SFXManager sfxManager;

    [Header("UI Prefabs")]
    public GameObject rowPrefab;
    public GameObject namePrefab;

    [Header("First PlayerUI")]
    public RowUI defaultRowUI;
    public OnClickGenericEvent firstOnClick;

    public GameObject actionWindow;
    public GameObject abilityWindow;

    [Header("Ability Window")]
    public Transform abilityUIHolder;
    public GameObject abilityUIPrefab;
    public Text manaNeededUI;

    [Header("Ability Texts")]
    public Text abilityText;
    public Text enemyAbText;

    public static int currentUICount = 1;

    private void Awake()
    {
        sfxManager = GameObject.Find("SFXManager").GetComponent<SFXManager>();
        Instance = this;
    }

    public void SpawnRow(out RowUI processedUI, CharacterData passedData)
    {
        //Instantiate a row inside the row holder.
        GameObject tmpRow = Instantiate(rowPrefab);
        tmpRow.transform.SetParent(rowHolder, false);
        RowUI rowTmpInfo = tmpRow.GetComponent<RowUI>();

        //Instantiate name inside the name holder.
        GameObject tmpName = Instantiate(namePrefab);
        tmpName.transform.SetParent(nameHolder, false);
        Text txtName = tmpName.GetComponent<Text>();
        OnClickGenericEvent onClickEvent = tmpName.GetComponent<OnClickGenericEvent>();

        tmpRow.name = "Character " + tmpRow.transform.childCount;

        rowTmpInfo.characterUI = txtName;
        onClickEvent.charHolder = passedData;

        processedUI = rowTmpInfo;
    }

    public void FillAbilityWindow()
    {
        CleanAbilityWindow();

        var data = BattleManager.Instance.currentCharacter.characterData.characterAbilities;

        for (int i = 0; i < data.Count; i++)
        {
            GameObject tmpAbilityPrefab = Instantiate(abilityUIPrefab);
            tmpAbilityPrefab.transform.SetParent(abilityUIHolder);

            AbilityUI tmpAbUI = tmpAbilityPrefab.GetComponent<AbilityUI>();
            tmpAbUI.abilityIndex = i;
            tmpAbUI.Init(data[i].abilityName);

        }
    }

    public void DeselectOtherAbilityUIs(AbilityUI thisUI)
    {
        foreach (var item in FindObjectsOfType<AbilityUI>())
        {
            if (item != thisUI)
            {
                item.isSelected = false;
            }
        }
    }

    public void SetManaNeededUI(int abilityMana, int charCurrMana)
    {
        manaNeededUI.text = abilityMana + " / " + charCurrMana;
    }

    public void SetAbilityText(string abName, CharacterTeam team)
    {
        switch (team)
        {
            case CharacterTeam.Friendly:
                abilityText.text = abName;
                break;
            case CharacterTeam.Enemy:
                enemyAbText.text = abName;
                break;
        }
    }

    void CleanAbilityWindow()
    {
        foreach (Transform item in abilityUIHolder)
        {
            Destroy(item.gameObject);
        }
    }
}
