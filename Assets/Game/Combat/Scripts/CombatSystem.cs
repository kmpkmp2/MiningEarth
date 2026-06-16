using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Map;

namespace DeepEarth.Combat
{
    public class CombatSystem : MonoBehaviour
    {
        private static CombatSystem _instance;
        public static CombatSystem Instance => _instance;

        [SerializeField] private Transform spawnPoint;

        private readonly List<MonsterPresenter> _activePresenters = new List<MonsterPresenter>();
        private readonly List<GameObject> _activeMonsterObjects = new List<GameObject>();

        private UniTaskCompletionSource _combatTcs;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(Transform monsterSpawnPoint)
        {
            spawnPoint = monsterSpawnPoint;
        }

        public async UniTask StartCombatAsync(MonsterType type, int depth)
        {
            ClearActiveMonsters();
            _combatTcs = new UniTaskCompletionSource();

            // Apply CurseInstantDamageOnEncounter if any
            int instantDmg = StatManager.Instance.GetEncounterInstantDamage();
            if (instantDmg > 0)
            {
                StatManager.Instance.TakeDamage(instantDmg);
                EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                EffectSystem.Instance.ShakeCamera(0.25f, 0.1f);
                string msg = $"-{instantDmg} HP" + LocalizationManager.Instance.GetTranslation("curse_tag");
                EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, msg, Color.red);
            }

            if (type == MonsterType.CaveRat)
            {
                await SpawnMonsterInstanceAsync(MonsterType.CaveRat, Vector3.zero, depth);
            }
            else if (type == MonsterType.CaveSpider)
            {
                // Spawn 3 CaveSpiders simultaneously
                await UniTask.WhenAll(
                    SpawnMonsterInstanceAsync(MonsterType.CaveSpider, new Vector3(-1.0f, 0f, 0.5f), depth),
                    SpawnMonsterInstanceAsync(MonsterType.CaveSpider, new Vector3(0f, 0f, 0f), depth),
                    SpawnMonsterInstanceAsync(MonsterType.CaveSpider, new Vector3(1.0f, 0f, 0.5f), depth)
                );
            }

            // Wait until all monsters are dead
            await _combatTcs.Task;

            // Combat finished
            ClearActiveMonsters();

            // Healing item drop chance (35%)
            if (UnityEngine.Random.value < 0.35f)
            {
                InventoryManager.Instance.AddItem("Item_Potion", 1);
                EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, "+1 Potion", Color.green);
            }

            Debug.Log("Combat finished!");
        }

        private async UniTask SpawnMonsterInstanceAsync(MonsterType type, Vector3 localOffset, int depth)
        {
            string key = (type == MonsterType.CaveRat) ? AddressableKeys.MonsterRat : AddressableKeys.MonsterSpider;
            GameObject mGo = await PoolSystem.Instance.GetAsync(key, spawnPoint);

            if (mGo == null)
            {
                Debug.LogError($"Failed to spawn monster: {key}");
                return;
            }

            Vector3 worldPos = spawnPoint.position + spawnPoint.TransformDirection(localOffset);
            mGo.transform.position = worldPos;
            mGo.transform.rotation = spawnPoint.rotation;
            _activeMonsterObjects.Add(mGo);

            var view = mGo.GetComponent<MonsterView>();
            if (view == null)
            {
                view = mGo.AddComponent<MonsterView>();
            }

            var model = new MonsterModel(type, depth);
            var presenter = new MonsterPresenter(model, view);
            _activePresenters.Add(presenter);

            presenter.OnMonsterKilled += HandleMonsterKilled;
        }

        private void HandleMonsterKilled(MonsterPresenter presenter)
        {
            presenter.OnMonsterKilled -= HandleMonsterKilled;

            // Visual feedback on death
            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);

            // Return object to pool
            GameObject go = presenter.View.gameObject;
            _activeMonsterObjects.Remove(go);
            PoolSystem.Instance.Return(go);

            _activePresenters.Remove(presenter);
            presenter.Dispose();

            // If all monsters are killed, complete combat
            if (_activePresenters.Count == 0)
            {
                _combatTcs?.TrySetResult();
            }
        }

        private void ClearActiveMonsters()
        {
            foreach (var pres in _activePresenters)
            {
                pres.Dispose();
            }
            _activePresenters.Clear();

            foreach (var obj in _activeMonsterObjects)
            {
                if (obj != null) PoolSystem.Instance.Return(obj);
            }
            _activeMonsterObjects.Clear();
        }
    }
}
