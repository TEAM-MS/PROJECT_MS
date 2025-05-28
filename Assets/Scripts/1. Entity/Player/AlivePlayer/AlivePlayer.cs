using System;
using System.Collections;
using System.Runtime.CompilerServices;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Random = UnityEngine.Random;

[RequireComponent(typeof(InteractionHandler))]
public class AlivePlayer : NetworkBehaviour, IDamageable
{
    #region Stats
    [field: SerializeField] public HealthResource Health {get; private set;}
    [field: SerializeField] public StaminaResource Stamina {get; private set;}

   // private readonly SyncVar<Inventory> Inventory = new SyncVar<Inventory>(new Inventory());
    public Inventory Inventory {get; private set;} = new Inventory();
    #endregion

    public InteractionHandler InteractionHandler { get; private set; }
    public CharacterController CharacterController { get; private set; }
    public Animator Animator { get; private set; }
    [field: SerializeField] public AnimatorOverrideController overrideController { get; private set; }
    [field: SerializeField] public CinemachineCamera CinemachineCamera { get; private set; }
    [field: SerializeField] public AlivePlayerSO AlivePlayerSO { get; private set; }
    [field: SerializeField] public AlivePlayerAnimationData AnimationData { get; private set; }
    [field: SerializeField] public WeaponController WeaponHandler { get; private set; }
    private AlivePlayerStateMachine stateMachine;

    public QuickSlotHandler QuickSlotHandler {get; private set;}
    public ItemHandler ItemHandler {get; private set;}
    public PlacementHandler PlacementHandler {get; private set;}
    public EquipHandler EquipHandler {get; private set;}

    public event Action onDead;
    public event Action onDamaged;

    private bool isDead = false;
    public bool IsDead => isDead;

    private static readonly string ouchSound = "Sound/Player/Ouch_01.mp3";
    private static readonly string eatSound = "Sound/Player/Eat_01.mp3";

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        InteractionHandler = GetComponent<InteractionHandler>();
        CharacterController = GetComponent<CharacterController>();
        Animator = GetComponentInChildren<Animator>();
        AnimationData = new AlivePlayerAnimationData();

        overrideController = new AnimatorOverrideController(Animator.runtimeAnimatorController);
        Animator.runtimeAnimatorController = overrideController;

        EquipHandler = gameObject.GetOrAddComponent<EquipHandler>();
        EquipHandler.Setup(Inventory);

        QuickSlotHandler = gameObject.GetOrAddComponent<QuickSlotHandler>();

        ItemHandler = gameObject.GetOrAddComponent<ItemHandler>();
        PlacementHandler = gameObject.GetOrAddComponent<PlacementHandler>(); 

        GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_MainColor", NetworkRoomSystem.Instance.GetPlayerColor(Owner));
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if(!IsOwner)
            return;

        Health.Current.OnChange += OnTakeDamage;

        Init();

        Managers.Instance.SetPlayer(this);
        CinemachineCamera = GameObject.FindWithTag("MainCinemachineCamera").GetComponent<CinemachineCamera>();
        CinemachineCamera.Follow = transform;

        Inventory.SetInventoryDataHandler(); 
        QuickSlotHandler.Setup(Inventory);
        ItemHandler.Setup(QuickSlotHandler); 
        PlacementHandler.Setup(QuickSlotHandler); 

        stateMachine = new AlivePlayerStateMachine(this);
    }

    public void Start()
    {
        ForestScene.onCompleted += () =>
        {
            NetworkCommandSystem.Instance.RequestDespawnPlayer(NetworkObject);
        }; 
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    [ServerRpc]
    private void Init()
    {
        
    }

    public void Update()
    {
        if(!IsOwner)
            return;

        stateMachine.Update();
    }


    public void FixedUpdate()
    {
        if(!IsOwner)
            return;

        stateMachine.FixedUpdate();
    }

    [ServerRpc(RequireOwnership = false)]  
    public void TakeDamage(float damage, NetworkConnection conn = null)
    {
        if (isDead)
            return;

        Health.Subtract(damage);

        if (Health.Current.Value <= 0 && conn != null)
        {
            NetworkGameSystem.Instance.UpdatePlayerKillCount(conn); 
        }
    }

    public void OnTakeDamage(float prev, float next, bool asServer)
    {
        if(prev > next)
        {
            onDamaged?.Invoke();

            Managers.Sound.Play(ouchSound);

            if(next <= 0)
            {
                isDead = true;
                onDead?.Invoke(); 
                StartCoroutine(DropItemCoroutine());  
                NetworkGameSystem.Instance.OnPlayerDead(); 
            }
        } 
    }

    public void ChangeWeapon(WeaponController weapon)
    {
        if(!IsOwner)
            return;

        WeaponHandler = weapon;

        int holdAnimationIndex = Managers.Data.Animation.GetIndex(WeaponHandler.holdAnimation);
        int attackAnimationIndex = Managers.Data.Animation.GetIndex(WeaponHandler.attackAnimation);
        float speed = WeaponHandler.attackAnimationSpeed;
        bool isHolding = WeaponHandler != null;

        //OnChangeWeapon(holdAnimationIndex, attackAnimationIndex, speed, isHolding);

        ServerRpcOnChangeWeapon(holdAnimationIndex, attackAnimationIndex, speed, isHolding);
    }

    private void OnChangeWeapon(int holdAnimationIndex, int attackAnimationIndex, float speed, bool isHolding)
    {
        overrideController["Holding"] = Managers.Data.Animation.GetByIndex(holdAnimationIndex);
        overrideController["Attack"] = Managers.Data.Animation.GetByIndex(attackAnimationIndex);
        Animator.SetFloat("AttackSpeed", speed);
        Animator.SetBool(AnimationData.HoldingParameterHash, isHolding);
    }

    [ServerRpc]
    private void ServerRpcOnChangeWeapon(int holdAnimationIndex, int attackAnimationIndex, float speed, bool isHolding)
    {
        ObserverRpcChangeWeapon(holdAnimationIndex, attackAnimationIndex, speed, isHolding);
    }

    [ObserversRpc]
    private void ObserverRpcChangeWeapon(int holdAnimationIndex, int attackAnimationIndex, float speed, bool isHolding)
    {
        OnChangeWeapon(holdAnimationIndex, attackAnimationIndex, speed, isHolding);
    }

    [ServerRpc]
    public void RestoreHunger(float amount) // 음식물 섭취
    {
        Health.Add(amount);

        Managers.Sound.Play(eatSound);
    }

    public bool CanTakeDamage()
    {
        if(isDead)
            return false;
 
        return true;
    }
 
 
    [Server]  
    private void DropItem(Vector3 pos, string DropPrefabPath, int count)
    {
        GameObject prefab = Managers.Resource.Load<GameObject>(DropPrefabPath);
        GameObject item = Instantiate(prefab); 
        float angle = count * 20f; // 45도씩 회전 
        float radius = 1f + (count * 0.05f); // 거리가 점점 멀어짐  
        Vector3 offset = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            1f,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
        item.transform.position = pos + offset; 

        item.GetOrAddComponent<Rigidbody>();
        InstanceFinder.ServerManager.Spawn(item);
    } 
 
    private IEnumerator DropItemCoroutine()
    {
        int cnt = 0;
        for (int i = 0; i < Inventory.ItemStorage.Count; i++)
        {
            ItemSlot itemSlot = Inventory.ItemStorage.GetSlotByIdx(i);
            if (itemSlot.Data == null) 
                continue;

            for (int j = 0; j < itemSlot.Stack; j++)
            {
                DropItem(transform.position, itemSlot.Data.DropPrefabPath, cnt++); 
                yield return new WaitForSeconds(0.2f);
            }

            itemSlot.Setup(null);  
        }
 
        for (int i = 0; i < Inventory.QuickSlotStorage.Count; i++)
        {
            ItemSlot itemSlot = Inventory.QuickSlotStorage.GetSlotByIdx(i);
            if (itemSlot.Data == null)
                continue;

            for (int j = 0; j < itemSlot.Stack; j++)
            {
                DropItem(transform.position, itemSlot.Data.DropPrefabPath, cnt++);  
                yield return new WaitForSeconds(0.2f);
            }
            itemSlot.Setup(null);  

            yield return new WaitForSeconds(0.2f);  
        }
    }
}
