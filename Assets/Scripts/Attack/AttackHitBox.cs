using System.Collections.Generic;
using UnityEngine;
using Blue.Entity.Common;
using Blue.Interface;

namespace Blue.Attack
{
    public class AttackHitBox : MonoBehaviour
    {
        private bool isAttacking = false;
        private HashSet<IAttackable> alreadyHitTargets = new HashSet<IAttackable>();
        private MonoBehaviour owner;
        private int power;

        public void Initialize(MonoBehaviour owner, int power)
        {
            this.owner = owner;
            this.power = power;
        }

        public void StartAttack()
        {
            isAttacking = true;
            alreadyHitTargets.Clear();
        }

        public void EndAttack()
        {
            isAttacking = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAttacking) return;

            // ボーンごとに分けたコライダーでも同じ所有者へ解決される
            if (EntityHit.TryResolve(other, out IAttackable target, out BodyPart part))
            {
                if (alreadyHitTargets.Contains(target)) return;

                alreadyHitTargets.Add(target);

                AttackData attackData = new AttackData(owner, target, power, AttackType.Melee, other.ClosestPointOnBounds(transform.position), part);
                target.Damage(attackData);
            }
        }
    }
}