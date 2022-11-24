using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class CharacterData
{
    public int id;
    public string characterName = "CharacterName";
    private SFXManager sfxManager;

    [Space(10)]

    public AbilityData basicAttack;
    public AbilityData limitBurst;
    public List<AbilityData> characterAbilities;
    public AbilityData attackSelected;

    [Space(10)]

    public CharacterUIData charUI;

    [Space(10)]

    public int maxHealth = 100;
    public int curHealth = 100;

    [Space(10)]

    public int maxManaPoints = 100;
    public int manaCurrentPoints = 100;

    [Space(10)]

    public float maxLBPoints = 100;
    public float currentLBPoints = 100;

    [Space(10)]

    public float speedLimit = 10;
    public float curSpeed = 10;

    [Space(10)]

    public CharacterState characterState;
    public CharacterTeam characterTeam;

    CharacterState savedState;

    [Space(10)]

    public UnityEvent onAttack;
    public UnityEvent onAttackQueue;
    public UnityEvent onWasAttacked;
    public UnityEvent onJustReady;

    UnityEvent onCharacterDied = new UnityEvent();

    [Space(16)]

    public CharacterData _target;

    [HideInInspector]
    public CharacterControl _charCont;

    public void Init()
    {
        sfxManager = GameObject.Find("SFXManager").GetComponent<SFXManager>();
        if (characterTeam == CharacterTeam.Friendly)
        {
            charUI.charData = this;
            charUI.Init(maxHealth, curHealth, maxManaPoints, manaCurrentPoints, characterName, speedLimit, maxLBPoints);
            onJustReady.AddListener(OnReadyDefault);
        }
            
        onAttack.AddListener(CharacterAttackDefault);
        onWasAttacked.AddListener(CharacterAttackedDefault);
        onAttackQueue.AddListener(OnAttackQueueDefault);

        onCharacterDied.AddListener(OnDeathDefault);

        characterState = CharacterState.Idle;
    }

    public IEnumerator QueueAttack(AbilityData ability)
    {
        if (!IsAlive || characterState == CharacterState.Finish || _target == null)
        {
            Debug.Log("stopped");
            _charCont.ClearAttackQueue();

            yield break;
        }  
        
        if(ability.manaCost <= manaCurrentPoints)
        {
            manaCurrentPoints -= ability.manaCost;
            if (characterTeam == CharacterTeam.Friendly)
            {
                charUI.UpdateManaBar(manaCurrentPoints);
            }
            
        }
        else
        {
            _charCont.ClearAttackQueue();
            yield break;
        }

        characterState = CharacterState.TryingAttack;
        onAttackQueue.Invoke();

        yield return new WaitUntil(() => _target.IsAttackable);

        if (!_target.IsAlive)
            yield break;


        if (!IsAlive || characterState == CharacterState.Finish || _target == null)
        {
            _charCont.ClearAttackQueue();
            yield break;
        }

        characterState = CharacterState.Attacking;

        _charCont.cachedTarget = _target._charCont;
        _charCont.lastAbilityUsed = ability;

        if (ability != limitBurst)
            _charCont.AttackAnimation();

        else
            _charCont.LimitBurstAnimation();

        yield return new WaitUntil(() => characterState == CharacterState.Attacking);


        UIManager.Instance.SetAbilityText(ability.abilityName, characterTeam);

        //_target.SaveCharacterState();
        //_target.characterState = CharacterState.Attacked;

        Debug.Log("Attacked with " + ability.abilityName + " to " + _target.characterName);
       
        
        //switch (ability.output)
        //{
        //    case AbilityOutput.Damage:
        //        _target.Damage(ability.abValue);
        //        break;
        //    case AbilityOutput.Heal:
        //        _target.Heal(ability.abValue);
        //        break;
        //}

        if (characterTeam == CharacterTeam.Friendly)
        {
            IncreaseLB(5);
        }
        
        BattleManager.Instance.readyToAttackQueue.Dequeue();
        //_target = null;
        _charCont.ClearAttackQueue();
        
        if (characterTeam == CharacterTeam.Friendly)
        {            
            BattleManager.Instance.currentCharacter = null;
            BattleManager.Instance.TurnQueue = new Queue<string>(BattleManager.Instance.TurnQueue.Where(friend => friend != characterName));
            
            if (BattleManager.Instance.TurnQueue.Count > 0)
            {
                BattleManager.Instance.friendlyCharacters.Find(friend =>
                    friend.characterData.characterName == BattleManager.Instance.TurnQueue.First()).characterData.SelectCharacter();
            }
        }
    }

    
    public void Heal(int healAmount)
    {
        curHealth = Mathf.Clamp(curHealth + healAmount, 0, _target.maxHealth);
        charUI.UpdateHealthBar(curHealth, maxHealth);
    }

    public void Damage(int damageAmount)
    {
        //var finalHealth = curHealth - damageAmount;
        curHealth -= damageAmount;
        
        if (characterTeam == CharacterTeam.Friendly)
        {
            charUI.UpdateHealthBar(curHealth, maxHealth);
        }

        if (curHealth <= 0)
        {
            curHealth = 0;
            characterState = CharacterState.Died;
            onCharacterDied.Invoke();
        }

        //Handling Limit Burst.
        if(characterTeam == CharacterTeam.Friendly)
        {
            IncreaseLB(5);
        }
        

        onWasAttacked.Invoke();
    }

    public bool CanQueueAttack
    {
        get
        {
            return characterState == CharacterState.TryingAttack || _charCont.attackQueue == null;
        }
    }

    public bool IsAttackable
    {
        get
        {
            return characterState == CharacterState.Idle || characterState == CharacterState.Ready || characterState == CharacterState.GoingAttack;
        }
    }

    public bool IsReadyForAction
    {
        get
        {
            return curSpeed >= speedLimit;
        }  
    }

    public bool IsReadyForLB
    {
        get
        {
        return (currentLBPoints >= maxLBPoints);
        }
    }

    public bool IsAlive
    {
        get { return characterState != CharacterState.Died; }
    }

    void IncreaseLB(int amount)
    {
        currentLBPoints += amount;
        currentLBPoints = Mathf.Clamp(currentLBPoints, 0, maxLBPoints);

        charUI.UpdateLimitBar(currentLBPoints);
    }

    void CharacterAttackDefault()
    {
        
    }

    void CharacterAttackedDefault()
    {
        if (characterState != CharacterState.Died)
        {
            characterState = savedState;
        }
            
        
    }

    void OnReadyDefault()
    {
        if (BattleManager.Instance.currentCharacter is null)
        {
            SelectCharacter();
        }
    }

    void OnAttackQueueDefault()
    {
        curSpeed = 0;

        if(characterTeam == CharacterTeam.Friendly)
            charUI.UpdateTimeBar(curSpeed);
    }

    void OnDeathDefault()
    {
        BattleManager.Instance.CheckMatchStatus();
        characterState = CharacterState.Died;
    }

    public void AddToAttackQueue()
    {
        if (characterTeam == CharacterTeam.Enemy)
        {
            BattleManager.Instance.readyToAttackQueue.Enqueue(id.ToString());
        }
        else
        {
            BattleManager.Instance.readyToAttackQueue.Enqueue(characterTeam.ToString());
            BattleManager.Instance.TurnQueue.Enqueue(characterName);
        }
        var newattacker = BattleManager.Instance.readyToAttackQueue.Last();
        Debug.Log("New Character "+newattacker+" added to the queue");
    }
    public void SelectCharacter()
    {
        if (!IsReadyForAction)
            return;

        UIManager.Instance.actionWindow.SetActive(true);
        EventSystem.current.SetSelectedGameObject(GameObject.FindGameObjectWithTag("Attack"));
        //var attackButton = GameObject.FindGameObjectWithTag("Attack");
        //EventSystem.current.GetComponent<EventSystem>().firstSelectedGameObject = attackButton;
        sfxManager.ActionSound();
        UIManager.Instance.abilityWindow.SetActive(false);

        foreach (var item in GameObject.FindObjectsOfType<CharacterControl>())
        {
            if (item.characterData.characterTeam != CharacterTeam.Enemy)
                item.characterData.ResetUINameText();
        }

        charUI.physicUI.characterUI.color = Color.cyan;
        BattleManager.Instance.currentCharacter = _charCont;
    }

    public void SaveCharacterState()
    {
        savedState = characterState;
    }

    public void ResetUINameText()
    {
        charUI.physicUI.characterUI.color = Color.white;
    }

    public IEnumerator CharacterLoop()
    {
        while (characterState != CharacterState.Died)
        {
            while (curSpeed < speedLimit)
            {

                yield return new WaitUntil(() => characterState != CharacterState.TryingAttack && characterState != CharacterState.Attacking);

                curSpeed += Time.deltaTime;

                if (characterTeam == CharacterTeam.Friendly)
                {
                    charUI.UpdateTimeBar(curSpeed);
                }

                if (characterState != CharacterState.Attacked || characterState != CharacterState.Died)
                {
                    characterState = CharacterState.Idle;
                }

                yield return null;
            }

            //We are ready
            curSpeed = speedLimit;
            
            characterState = CharacterState.Ready;
            AddToAttackQueue();
            
            onJustReady.Invoke();

            yield return new WaitUntil(() => characterState == CharacterState.Attacking);

            yield return new WaitUntil(() => characterState == CharacterState.Idle);
         


        }
    }

    
}

[System.Serializable]
public class CharacterUIData
{
    public RowUI physicUI;
    public int placeInUI = 1;

    [HideInInspector]
    public CharacterData charData;

public void Init(int maxHealth, int curHealth, int maxMana, int curMana, string charName, float speedLimit, float limitMax)
    {
        placeInUI = UIManager.currentUICount;

        if (placeInUI == 1)
        {
            //Do not want to spawn another row and use the first one.
            physicUI = UIManager.Instance.defaultRowUI;
            UIManager.Instance.firstOnClick.charHolder = charData;
        }
        else
        {
            //General Spawning  of the row
            UIManager.Instance.SpawnRow(out physicUI, charData);
        }
        

        //Health Slider Setup
        physicUI.healthSlider.maxValue = maxHealth;
        UpdateHealthBar(curHealth, maxHealth);

        //Mana Slider Setup
        physicUI.manaSlider.maxValue = maxMana;
        UpdateManaBar(curMana);

        //Character Info Setup
        physicUI.characterUI.text = charName;

        //Limit and Time bar Setup
        physicUI.limitSlider.maxValue = limitMax;
        physicUI.limitSlider.value = 0;
        physicUI.timeSlider.maxValue = speedLimit;
        physicUI.timeSlider.value = 0;

        UIManager.currentUICount++;
    }

    public void UpdateTimeBar(float currentProg)
    {
        physicUI.timeSlider.value = currentProg;
    }

    public void UpdateLimitBar(float currentProg)
    {
        physicUI.limitSlider.value = currentProg;
    }

    public void UpdateHealthBar(int currentAmount, int maxAmount)
    {
        physicUI.healthSlider.value = currentAmount;
        physicUI.healthUI.text = currentAmount.ToString() + "/" + maxAmount.ToString();
    }

    public void UpdateManaBar(int currentAmount)
    {
        physicUI.manaSlider.value = currentAmount;
        physicUI.manaUI.text = currentAmount.ToString();
    }
}


public enum CharacterTeam
{
    Friendly,
    Enemy
}


public enum CharacterState
{
    Loading,
    Idle,
    Ready,
    Attacked,
    SelectingTarget,
    ReadyToAttack,
    Attacking,
    Died,
    TryingAttack,
    GoingAttack,
    Finish
}
