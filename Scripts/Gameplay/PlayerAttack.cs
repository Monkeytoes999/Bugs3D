using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Bugs.Game;

namespace Unity.Bugs.Gameplay {   

    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAttack : MonoBehaviour {

        [Header("Details")][Tooltip("Attack Speed")]
        public float AttackSpeed;

        [Tooltip("Attack Damage")]
        public float damage;

        [Tooltip("Projectile Prefab")]
        public ProjectileBase ProjectilePrefab;

        [Tooltip("Fire From")]
        public Transform FireLocation;

        Vector3 LastFireFrom;
        Vector3 InheritedVelocity;

        PlayerInputHandler m_InputHandler;

        float lastFired = 0;

        void Awake() {
            LastFireFrom = FireLocation.position;
        }


        void Start() {
            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerWeaponsManager>(m_InputHandler, this,
                gameObject);
        }

        
        void Update() {
            if (lastFired == 0 || (Time.time - lastFired > AttackSpeed)) {
                if (m_InputHandler.GetFireInputDown() || m_InputHandler.GetFireInputHeld()) {
                    if (Time.deltaTime > 0) {
                        InheritedVelocity = (FireLocation.position - LastFireFrom) / Time.deltaTime;
                        LastFireFrom = FireLocation.position;
                    }

                    Fire();

                    lastFired = Time.time;
                }
            }
        }

        public virtual void Fire() {}
    }
}