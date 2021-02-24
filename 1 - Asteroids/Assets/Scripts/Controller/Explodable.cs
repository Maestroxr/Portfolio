using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class Explodable : Shootable, ICyclic<Shootable>
    {

        [SerializeField]
        private AsteroidSettings settings;
        

        public override void WasShot(Shot shot)
        {
            base.WasShot(shot);
            Explode(shot);
        }


        private void Explode(Shot shot)
        {
            var collidesWith = Physics.OverlapSphere(transform.position, settings.AsteroidExplosionRadius);
            for (int i = 0; i < collidesWith.Length; i++)
            {
                var shootable = collidesWith[i].gameObject.GetComponent<Shootable>();
                if (shootable != null)
                {
                    shootable.WasShot(shot);
                }
            }
        }
    }
}