using System;
using System.Collections;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.Combat
{
    public class BossCoreView : MonoBehaviour
    {
        public event Action<BossCoreView> OnCoreDestroyed;

        public BlockType OreType { get; private set; }
        private int _hp;
        private bool _destroyed;
        private Material _instancedMaterial;
        private Color _originalColor;

        public void Setup(BlockType oreType, int maxHp, Color color)
        {
            OreType = oreType;
            _hp = maxHp;
            _destroyed = false;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                _instancedMaterial = mr.material;
                _instancedMaterial.color = color;
                _originalColor = color;
            }
        }

        private void OnMouseDown()
        {
            if (_destroyed) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.BossCombat) return;

            int dmg = StatManager.Instance.GetAttackDamage();
            _hp -= dmg;
            if (_hp < 0) _hp = 0;

            EffectSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 0.5f, dmg.ToString(), Color.yellow);
            EffectSystem.Instance.ShakeCamera(0.1f, 0.04f);
            StartCoroutine(CoFlash());

            Debug.Log($"[Boss]\nCore Hit\nOre : {OreType}\nDamage : {dmg}\nHP : {_hp}");

            if (_hp <= 0)
            {
                _destroyed = true;
                OnCoreDestroyed?.Invoke(this);
            }
        }

        private IEnumerator CoFlash()
        {
            if (_instancedMaterial == null) yield break;
            _instancedMaterial.color = Color.white;
            yield return new WaitForSeconds(0.12f);
            if (!_destroyed) _instancedMaterial.color = _originalColor;
        }

        private void OnDestroy()
        {
            if (_instancedMaterial != null)
                Destroy(_instancedMaterial);
        }
    }
}
