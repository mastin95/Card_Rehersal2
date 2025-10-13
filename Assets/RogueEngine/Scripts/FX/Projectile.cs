using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.Client;

namespace RogueEngine.FX
{

    public class Projectile : MonoBehaviour
    {
        public float speed = 5f;
        public float duration = 5f;
        public GameObject explode_fx;
        public AudioClip explode_audio;

        [HideInInspector]
        public int damage; //Damage dealth by projectile, to delay HP display by this amount

        private Transform source;
        private Transform target;
        private Vector3 source_offset;
        private Vector3 target_offset;
        private float timer = 0f;

        public void DelayDamage()
        {
            BoardCharacter tcharacter = target?.GetComponentInParent<BoardCharacter>();
            if (tcharacter != null)
            {
                //Delay visual HP so that the HP dont change before projectile hit
                tcharacter.DelayDamage(damage, 8f / speed);
            }
        }

        void Update()
        {
            timer += Time.deltaTime;

            if (source == null || target == null)
            {
                Destroy(gameObject);
                return;
            }

            if (timer > duration)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 spos = transform.position;
            Vector3 tpos = target.position + target_offset;
            Vector3 dir = (tpos - spos);
            transform.position += dir.normalized * Mathf.Min(dir.magnitude, 1f) * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, dir.normalized);

            if (dir.magnitude < 0.2f)
            {
                FXTool.DoFX(explode_fx, target.position);
                AudioTool.Get().PlaySFX("fx", explode_audio);
                Destroy(gameObject);
            }
        }

        public void SetSource(Transform source)
        {
            this.source = source;
            transform.position = source.position;
        }

        public void SetSource(Transform source, Vector3 offset)
        {
            this.source = source;
            source_offset = offset;
            transform.position = source.position + source_offset;
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public void SetTarget(Transform target, Vector3 offset)
        {
            this.target = target;
            target_offset = offset;
        }

    }
}