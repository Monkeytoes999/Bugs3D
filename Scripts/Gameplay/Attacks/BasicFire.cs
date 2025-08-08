using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Bugs.Game;


namespace Unity.Bugs.Gameplay {    

    public class BasicFire : PlayerAttack {

        [Tooltip("Audio source for shooting")]
        public AudioSource AudioSource;

        [Tooltip("Audio file to play when shooting")]
        public AudioClip shootSfx;

        public override void Fire() {
            ProjectileBase newProjectile = Instantiate(ProjectilePrefab, gameObject.transform.Find("AimPoint").transform.position,
                Quaternion.LookRotation(gameObject.transform.Find("Main Camera").transform.forward));
            newProjectile.Shoot(gameObject, Vector3.zero, damage);
            AudioSource.PlayOneShot(shootSfx);
        }
    }
}