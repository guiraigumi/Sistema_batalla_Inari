using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEngine.EventSystems;

public class BattleManager : MonoBehaviour
{
    public CharacterControl currentCharacter;

    public List<CharacterControl> friendlyCharacters = new List<CharacterControl>();

    public List<CharacterControl> enemyCharacters = new List<CharacterControl>();

    public Queue<string> readyToAttackQueue = new Queue<string>();
    public Queue<string> TurnQueue = new Queue<string>();
    public Queue<CharacterControl> goingToAttackQueue = new Queue<CharacterControl>();
    
    public Animator cursorAnimator;
    public GameObject graphicCube;
    private SFXManager sfxManager;

    public static BattleManager Instance;

    private void Awake()
    {
        sfxManager = GameObject.Find("SFXManager").GetComponent<SFXManager>();
        cursorAnimator = GetComponent<Animator>();
        Instance = this;
    }

    private void Start()
    {
        friendlyCharacters = FindObjectsOfType<CharacterControl>().ToList().FindAll(x => x.characterData.characterTeam == CharacterTeam.Friendly);
        enemyCharacters = FindObjectsOfType<CharacterControl>().ToList().FindAll(x => x.characterData.characterTeam == CharacterTeam.Enemy);
        int randomId = 0;
        foreach (CharacterControl enemy in enemyCharacters)
        {
            enemy.characterData.id = randomId;
            randomId++;
        }
    }

    private void Update()
    {
        if (goingToAttackQueue.Count() > 0 
            && !friendlyCharacters.Any(friend => friend.characterData.characterState == CharacterState.Attacking)
            && !enemyCharacters.Any(enemy => enemy.characterData.characterState == CharacterState.Attacking))
        {         
            Debug.Log(goingToAttackQueue.First().characterData.characterName+" is atacking");
            CharacterControl atacker = goingToAttackQueue.Dequeue();
            if (atacker.characterData.CanQueueAttack)
            {
                atacker.attackQueue = StartCoroutine(atacker.characterData.QueueAttack(atacker.characterData.attackSelected));
                if (atacker.characterData.characterTeam == CharacterTeam.Friendly)
                {                    
                    GraphicSelectStatus(false);
                }
            }
        }
        if(currentCharacter is not null && currentCharacter.characterData.characterState == CharacterState.ReadyToAttack)
        {	
            if (enemyCharacters.Count > 1)
            {
                if (Input.GetButtonDown("Horizontal"))
                {
                    CharacterData enemytargeted = currentCharacter.characterData._target;
                    sfxManager.MoveSound();
                    int idx = enemyCharacters.FindIndex(enemy => enemy.characterData.id == enemytargeted.id);
                    if (idx == enemyCharacters.Count - 1)
                    {
                        SelectCharacterTarget(enemyCharacters[0].characterData);
                    }
                    else
                    {
                        SelectCharacterTarget(enemyCharacters[idx + 1].characterData);
                    }
                }else if (Input.GetButtonDown("Submit"))
                {
                    Debug.Log(currentCharacter.characterData.characterName +" going to the attack queue.");
                    goingToAttackQueue.Enqueue(currentCharacter);
                    sfxManager.SelectSound();
                    UIManager.Instance.actionWindow.SetActive(false);
                    UIManager.Instance.abilityWindow.SetActive(false);
                }
            }
        }
        if(currentCharacter is not null && currentCharacter.characterData.characterState == CharacterState.SelectingTarget)
        {
            currentCharacter.characterData.characterState = CharacterState.ReadyToAttack;
        }

        if (readyToAttackQueue.Count > 0)
        {
            if(readyToAttackQueue.First() != CharacterTeam.Friendly.ToString())
            {                              
                CharacterControl enemyControl =
                    enemyCharacters.Find(enemy => enemy.characterData.id.ToString() == readyToAttackQueue.First()
                                                  && enemy.characterData.characterState != CharacterState.GoingAttack
                                                  && enemy.characterData.characterState != CharacterState.Attacking);
                if (enemyControl is not null)
                {
                    enemyControl.characterData.characterState = CharacterState.GoingAttack;
                    enemyControl.characterData._target = BattleManager.Instance.RandomFriendlyCharacter.characterData;
                
                    //ALWAYS BASIC ATTACK
                    enemyControl.characterData.attackSelected = enemyControl.characterData.basicAttack;
                    
                    Debug.Log(enemyControl.characterData.characterName +" going to the attack queue.");
                    goingToAttackQueue.Enqueue(enemyControl);
                }
            }
        }
    }

    public void SelectCharacter(CharacterData newChar)
    {
        newChar.SelectCharacter();       
        SetTargetGraphicPosition(currentCharacter);
    }

    public void SelectCharacterTarget(CharacterData target)
    {
        if (currentCharacter != null)
        {
            if (currentCharacter.characterData.IsReadyForAction)
            {               
                currentCharacter.characterData._target = target;
                SetTargetGraphicPosition(currentCharacter);
            }   
        }
    }

    public void DoBasicAttackOnTarget()
    {
        if (currentCharacter.characterData.IsReadyForAction)
        {
            if (currentCharacter.characterData.characterTeam == CharacterTeam.Friendly)
            {
                Debug.Log("IsReady to attack");
                currentCharacter.characterData.characterState = CharacterState.SelectingTarget;
                EventSystem.current.SetSelectedGameObject(null);
                SetTargetGraphicPosition(enemyCharacters.FirstOrDefault());
                BattleManager.Instance.SelectCharacterTarget(enemyCharacters.FirstOrDefault().characterData);
                currentCharacter.characterData.attackSelected = currentCharacter.characterData.basicAttack;
            }
        }
    }

    public void SetTargetGraphicPosition(CharacterControl charControl)
    {
        if (charControl == null)
            return;
        
        if (charControl.characterData._target == null)
        { 
            //Deactivate graphic cube
            GraphicSelectStatus(false);
        }
        else
        {
            GraphicSelectStatus(true);
            var charTarget = charControl.characterData._target._charCont;
            graphicCube.transform.position = new Vector3(charTarget.transform.position.x, charTarget.transform.position.y + 2, charTarget.transform.position.z);
            cursorAnimator.Play("Cursor");
        }


    }
        
    void GraphicSelectStatus(bool status)
    {
        graphicCube.SetActive(status);
    }

    public void DoLBAttackOnTarget()
    {
        if (currentCharacter.characterData.IsReadyForLB)
        {
            Debug.Log("IsReady to attack");
            if (currentCharacter.characterData.characterTeam == CharacterTeam.Friendly)
            {
                if (currentCharacter.characterData._target != null)
                {
                    Debug.Log("Player did an action");
                    if (currentCharacter.characterData._target.IsAttackable)
                    {
                        if (currentCharacter.characterData.CanQueueAttack)
                        { 
                            currentCharacter.attackQueue = StartCoroutine(currentCharacter.characterData.QueueAttack(currentCharacter.characterData.limitBurst));
                            currentCharacter.characterData.currentLBPoints = 0;
                            currentCharacter.characterData.charUI.UpdateLimitBar(currentCharacter.characterData.currentLBPoints);
                        }

                    }
                }
            }
        }

    }

    public CharacterControl RandomFriendlyCharacter
    {
        get
        {
            return friendlyCharacters[Random.Range(0, friendlyCharacters.Count)];
    }

  }

    public void CheckMatchStatus()
    {
        if (FriendlyCharacterAlive && !EnemyCharacterAlive)
        {
            Debug.Log("Match Won!");

            StopAllCharacters();
        }

        if (!FriendlyCharacterAlive && EnemyCharacterAlive)
        {
            Debug.Log("Match lost!");

            StopAllCharacters();
        }
    }

    void StopAllCharacters()
    {
        for (int i = 0; i < friendlyCharacters.Count; i++)
        {
            friendlyCharacters[i].StopAll();
        }

        for (int i = 0; i < enemyCharacters.Count; i++)
        {
            enemyCharacters[i].StopAll();
        }
    }

    public bool FriendlyCharacterAlive
    {
        get
        {
            bool friendlyAlive = false;

            for (int i = 0; i < friendlyCharacters.Count; i++)
            {
                if (friendlyCharacters[i].characterData.IsAlive)
                {
                    friendlyAlive = true;
                }
            }

            return friendlyAlive;
        }
    }

    bool EnemyCharacterAlive
    {
        get
        {
            bool enemyAlive = false;

            for (int i = 0; i < enemyCharacters.Count; i++)
            {
                if (enemyCharacters[i].characterData.IsAlive)
                {
                    enemyAlive = true;
                }
            }

            return enemyAlive;
        }
    }

}

