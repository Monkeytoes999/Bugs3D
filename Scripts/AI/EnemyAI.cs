using System.Collections;
using System.Collections.Generic;
using Unity.Bugs.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.Bugs.AI {

    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour {

        public enum AIState
        {
            Patrol,
            Follow,
            Attack,
        }

        public Animator Animator;

        [System.Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        [Header("Parameters")]
        [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
        public float SelfDestructYHeight = -20f;

        [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
        public float PathReachingRadius = 2f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 10f;

        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 0f;

        [Header("Sounds")] [Tooltip("Sound played when recieving damages")]
        public AudioClip DamageTick;

        [Header("VFX")] [Tooltip("The VFX prefab spawned when the enemy dies")]
        public GameObject DeathVfx;

        [Tooltip("The point at which the death VFX is spawned")]
        public Transform DeathVfxSpawnPoint;

        [Header("Loot")] [Tooltip("The object this enemy can drop when dying")]
        public GameObject LootPrefab;

        [Tooltip("The chance the object has to drop")] [Range(0, 1)]
        public float DropRate = 1f;

        [Header("Damage")][Tooltip("Enemy Attack Damage")]
        public float AttackDamage = 10;

        [Tooltip("Attack Speed")]
        public float AttackFrequency = 1f;
        
        [Header("Mobile")][Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
        [Range(0f, 1f)]
        public float AttackStopDistanceRatio = 0.1f;

        [Tooltip("The shakiness of the enemy's movement")]
        public float maxShake = 5;

        [Header("Sound")] public AudioClip MovementSound;
        public MinMaxFloat PitchDistortionMovementSpeed;


        public AIState AiState { get; private set; }
        AudioSource m_AudioSource;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        float m_LastTimeDamaged = float.NegativeInfinity;
        float m_lastTimeAttacked = float.NegativeInfinity;

        public PatrolPath PatrolPath { get; set; }
        public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
        public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;
        public NavMeshAgent NavMeshAgent { get; private set; }
        public DetectionModule DetectionModule { get; private set; }

        int m_PathDestinationNodeIndex;
        EnemyManager m_EnemyManager;
        ActorsManager m_ActorsManager;
        Health m_Health;
        Actor m_Actor;
        Collider[] m_SelfColliders;
        GameLoop m_gameLoop;
        bool m_WasDamagedThisFrame;
        NavigationModule m_NavigationModule;
        public UnityAction onAttack;

        float currentXOffset = 0;
        float currentZOffset = 0;

        void Start() {
            m_EnemyManager = FindObjectOfType<EnemyManager>();
            DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyAI>(m_EnemyManager, this);

            m_ActorsManager = FindObjectOfType<ActorsManager>();
            DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyAI>(m_ActorsManager, this);

            m_EnemyManager.RegisterEnemy(this);

            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyAI>(m_Health, this, gameObject);

            m_Actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyAI>(m_Actor, this, gameObject);

            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_SelfColliders = GetComponentsInChildren<Collider>();

            m_gameLoop = FindObjectOfType<GameLoop>();
            DebugUtility.HandleErrorIfNullFindObject<GameLoop, EnemyAI>(m_gameLoop, this);

            // Subscribe to damage & death actions
            m_Health.OnDie += OnDie;
            m_Health.OnDamaged += OnDamaged;

            var detectionModules = GetComponentsInChildren<DetectionModule>();
            DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyAI>(detectionModules.Length, this,
                gameObject);
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyAI>(detectionModules.Length,
                this, gameObject);
            DetectionModule = detectionModules[0];
            DetectionModule.onDetectedTarget += OnDetectedTarget;
            DetectionModule.onLostTarget += OnLostTarget;
            onAttack += DetectionModule.OnAttack;

            var navigationModules = GetComponentsInChildren<NavigationModule>();
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyAI>(detectionModules.Length,
                this, gameObject);
            if (navigationModules.Length > 0)
            {
                m_NavigationModule = navigationModules[0];
                NavMeshAgent.speed = m_NavigationModule.MoveSpeed;
                NavMeshAgent.angularSpeed = m_NavigationModule.AngularSpeed;
                NavMeshAgent.acceleration = m_NavigationModule.Acceleration;
            }

            
            AiState = AIState.Patrol;

            m_AudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
            m_AudioSource.clip = MovementSound;
            m_AudioSource.Play();
        }

        void Update() {
            EnsureIsWithinLevelBounds();
            // if (DetectionModule.IsTargetVeryClose) {
            //     GameObject.Find("Player").GetComponent<Health>().TakeDamage(1, gameObject);
            // }


            DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

            m_WasDamagedThisFrame = false;

            UpdateAiStateTransitions();
            UpdateCurrentAiState();

            float moveSpeed = NavMeshAgent.velocity.magnitude;
            if (Random.value > .95) {
                currentXOffset = Random.Range(-1*maxShake, maxShake);
                currentZOffset = Random.Range(-1*maxShake, maxShake);
            }
            
            Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

            m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max,
               moveSpeed / NavMeshAgent.speed);
        
        }

        void UpdateAiStateTransitions() {
            switch (AiState) {
                case AIState.Follow:
                    if (IsSeeingTarget && IsTargetInAttackRange) {
                        AiState = AIState.Attack;
                        SetNavDestination(transform.position);
                    }
                    break;
                case AIState.Attack:
                    if (!IsTargetInAttackRange) {
                        AiState = AIState.Follow;
                    }
                    break;
            }
        }

        void UpdateCurrentAiState()
        {
            Vector3 AdjustedOrientation = Vector3.zero;
            switch (AiState) {
                case AIState.Follow:
                    AdjustedOrientation = KnownDetectedTarget.transform.position;
                    AdjustedOrientation.x = AdjustedOrientation.x + currentXOffset;
                    AdjustedOrientation.z = AdjustedOrientation.z + currentZOffset;
                    SetNavDestination(AdjustedOrientation);
                    OrientTowards(AdjustedOrientation);
                    break;
                case AIState.Attack:
                    AdjustedOrientation = KnownDetectedTarget.transform.position;
                    AdjustedOrientation.x = AdjustedOrientation.x + currentXOffset;
                    AdjustedOrientation.z = AdjustedOrientation.z + currentZOffset;
                    if (Vector3.Distance(AdjustedOrientation, DetectionModule.DetectionSourcePoint.position)
                        >= (AttackStopDistanceRatio * DetectionModule.AttackRange))
                    {
                        SetNavDestination(AdjustedOrientation);
                    }
                    else
                    {
                        SetNavDestination(transform.position);
                    }

                    OrientTowards(AdjustedOrientation);
                    TryAttack(AdjustedOrientation);
                    break;
            }
        }

        void EnsureIsWithinLevelBounds() {
            if (transform.position.y < SelfDestructYHeight)
            {
                Destroy(gameObject);
                return;
            }
        }

        void OnAttack() {
            Animator.SetTrigger(k_AnimAttackParameter);
        }

        void OnDetectedTarget() {
            if (AiState == AIState.Patrol) {
                AiState = AIState.Follow;
            }

            Animator.SetBool(k_AnimAlertedParameter, true);
        }

        void OnLostTarget() {
            if (AiState == AIState.Follow || AiState == AIState.Attack) {
                AiState = AIState.Patrol;
            }

            Animator.SetBool(k_AnimAlertedParameter, false);
        }

        void OnDamaged(float damage, GameObject damageSource) {
            if (damageSource && !damageSource.GetComponent<EnemyAI>()) {
                DetectionModule.OnDamaged(damageSource);
                
                Animator.SetTrigger(k_AnimOnDamagedParameter);
                m_LastTimeDamaged = Time.time;
            
                if (DamageTick && !m_WasDamagedThisFrame)
                    AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);
            
                m_WasDamagedThisFrame = true;
            }
        }

        public void OrientTowards(Vector3 lookPosition) {
            Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
            if (lookDirection.sqrMagnitude != 0f) {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
            }
        }

        bool IsPathValid() {
            return PatrolPath && PatrolPath.PathNodes.Count > 0;
        }

        public void ResetPathDestination() {
            m_PathDestinationNodeIndex = 0;
        }

        public void SetPathDestinationToClosestNode()
        {
            if (IsPathValid()) {
                int closestPathNodeIndex = 0;
                for (int i = 0; i < PatrolPath.PathNodes.Count; i++) {
                    float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                    if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex)) {
                        closestPathNodeIndex = i;
                    }
                }

                m_PathDestinationNodeIndex = closestPathNodeIndex;
            } else {
                m_PathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath()
        {
            if (IsPathValid()) {
                return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
            }
            else {
                return transform.position;
            }
        }

        public void SetNavDestination(Vector3 destination) {
            if (NavMeshAgent) {
                NavMeshAgent.SetDestination(destination);
            }
        }

        public void UpdatePathDestination(bool inverseOrder = false) {
            if (IsPathValid()) {
                if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius) {
                    m_PathDestinationNodeIndex =
                        inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                    if (m_PathDestinationNodeIndex < 0) {
                        m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                    }

                    if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count) {
                        m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                    }
                }
            }
        }

        void OnDie() {
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);

            m_EnemyManager.UnregisterEnemy(this);

            if (TryDropItem())
            {
                Instantiate(LootPrefab, transform.position, Quaternion.identity);
            }

            m_gameLoop.bugKilled();
            Destroy(gameObject, DeathDuration);
        }

        public bool TryAttack(Vector3 enemyPosition) {
            if (m_gameLoop.gameOver)
                return false;

            bool didFire = false;

            //Make this a conditional eventually. This right now is not great;
            if (Time.time > m_lastTimeAttacked + AttackFrequency) {
                didFire = true;
                m_lastTimeAttacked = Time.time;
                GameObject.Find("Player").GetComponent<Health>().TakeDamage(AttackDamage, gameObject);
                //KnownDetectedTarget.GetComponent<Health>().TakeDamage(AttackDamage, gameObject);
            }

            if (didFire) {
                OnAttack();
                if (onAttack != null) {
                    onAttack.Invoke();
                }
            }

            return didFire;
        }

        public bool TryDropItem()
        {
            if (DropRate == 0 || LootPrefab == null)
                return false;
            else if (DropRate == 1)
                return true;
            else
                return (Random.value <= DropRate);
        }

    }
}
