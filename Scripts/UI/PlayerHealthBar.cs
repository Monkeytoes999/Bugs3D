using Unity.Bugs.Game;
using Unity.Bugs.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Bugs.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        [Tooltip("Image component displaying current health")]
        public Image HealthFillImage;

        Health m_PlayerHealth;

        void Start()
        {
            PlayerController playerController = GameObject.FindObjectOfType<PlayerController>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerController, PlayerHealthBar>(playerController, this);

            m_PlayerHealth = playerController.GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerHealthBar>(m_PlayerHealth, this, playerController.gameObject);
        }

        void Update()
        {
            HealthFillImage.fillAmount = m_PlayerHealth.CurrentHealth / m_PlayerHealth.MaxHealth;
        }
    }
}