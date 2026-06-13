using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Map;

namespace DeepEarth.Mining
{
    public class MiningSystem : MonoBehaviour
    {
        private static MiningSystem _instance;
        public static MiningSystem Instance => _instance;

        [SerializeField] private Transform spawnPoint;

        private BlockPresenter _currentBlockPresenter;
        private GameObject _currentBlockObject;

        public event Action<BlockType, int> OnResourceMined;
        public event Action OnBlockCleared;

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

        public void Initialize(Transform blockSpawnPoint)
        {
            spawnPoint = blockSpawnPoint;
        }

        public async UniTask SpawnNextBlockAsync()
        {
            // Clear current block if any
            ClearCurrentBlock();

            int depth = GameManager.Instance.CurrentDepth;
            BlockType type = ChooseBlockTypeByDepth(depth);
            string addressableKey = GetAddressableKeyForBlock(type);

            // Load and instantiate from PoolSystem
            _currentBlockObject = await PoolSystem.Instance.GetAsync(addressableKey, spawnPoint);
            if (_currentBlockObject == null)
            {
                Debug.LogError($"Failed to spawn block: {addressableKey}");
                return;
            }

            // Set local position relative to BlockSpawnRoot using depth to slide into view
            _currentBlockObject.transform.localPosition = new Vector3(0, 0, depth);
            _currentBlockObject.transform.localRotation = Quaternion.identity;

            var view = _currentBlockObject.GetComponent<BlockView>();
            if (view == null)
            {
                view = _currentBlockObject.AddComponent<BlockView>();
            }

            var model = new BlockModel(type, depth);
            _currentBlockPresenter = new BlockPresenter(model, view);
            _currentBlockPresenter.OnBlockDestroyed += HandleBlockDestroyed;
        }

        private void ClearCurrentBlock()
        {
            if (_currentBlockPresenter != null)
            {
                _currentBlockPresenter.OnBlockDestroyed -= HandleBlockDestroyed;
                _currentBlockPresenter.Dispose();
                _currentBlockPresenter = null;
            }

            if (_currentBlockObject != null)
            {
                PoolSystem.Instance.Return(_currentBlockObject);
                _currentBlockObject = null;
            }
        }

        private void HandleBlockDestroyed(BlockPresenter presenter)
        {
            // Resource drop logic
            RewardPlayerForBlock(presenter.Model.Type);

            // Play shatter effects
            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetBlockColor());
            EffectSystem.Instance.ShakeCamera(0.25f, 0.12f);

            // Clear and invoke events
            ClearCurrentBlock();
            OnBlockCleared?.Invoke();

            // Notify GameManager to proceed (this triggers depth increase, check combat, check events, etc.)
            GameManager.Instance.OnBlockMined().Forget();
        }

        private void RewardPlayerForBlock(BlockType type)
        {
            int amount = 1;
            if (type == BlockType.Root)
            {
                // Root has high chance to drop HP recovery
                if (UnityEngine.Random.value < 0.4f)
                {
                    StatManager.Instance.Heal(2);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, "+2 HP", Color.green);
                }
            }
            
            // Add resource to GameManager inventory (except Dirt which is junk)
            if (type != BlockType.Dirt)
            {
                OnResourceMined?.Invoke(type, amount);
            }
        }

        private BlockType ChooseBlockTypeByDepth(int depth)
        {
            float rand = UnityEngine.Random.value;

            if (depth < 30) // Very Easy
            {
                if (rand < 0.60f) return BlockType.Dirt;
                if (rand < 0.85f) return BlockType.Stone;
                if (rand < 0.95f) return BlockType.Root;
                return BlockType.Iron;
            }
            else if (depth < 80) // Easy
            {
                if (rand < 0.30f) return BlockType.Dirt;
                if (rand < 0.70f) return BlockType.Stone;
                if (rand < 0.80f) return BlockType.Root;
                if (rand < 0.95f) return BlockType.Iron;
                return BlockType.Silver;
            }
            else if (depth < 150) // Medium
            {
                if (rand < 0.10f) return BlockType.Dirt;
                if (rand < 0.50f) return BlockType.Stone;
                if (rand < 0.60f) return BlockType.Root;
                if (rand < 0.80f) return BlockType.Iron;
                if (rand < 0.92f) return BlockType.Silver;
                if (rand < 0.99f) return BlockType.Gold;
                return BlockType.Diamond;
            }
            else if (depth < 250) // Hard
            {
                if (rand < 0.30f) return BlockType.Stone;
                if (rand < 0.40f) return BlockType.Root;
                if (rand < 0.65f) return BlockType.Iron;
                if (rand < 0.83f) return BlockType.Silver;
                if (rand < 0.95f) return BlockType.Gold;
                return BlockType.Diamond;
            }
            else // Very Hard
            {
                if (rand < 0.20f) return BlockType.Stone;
                if (rand < 0.30f) return BlockType.Root;
                if (rand < 0.50f) return BlockType.Iron;
                if (rand < 0.70f) return BlockType.Silver;
                if (rand < 0.90f) return BlockType.Gold;
                return BlockType.Diamond;
            }
        }

        private string GetAddressableKeyForBlock(BlockType type)
        {
            switch (type)
            {
                case BlockType.Dirt: return AddressableKeys.BlockDirt;
                case BlockType.Stone: return AddressableKeys.BlockStone;
                case BlockType.Root: return AddressableKeys.BlockRoot;
                case BlockType.Iron: return AddressableKeys.BlockIron;
                case BlockType.Silver: return AddressableKeys.BlockSilver;
                case BlockType.Gold: return AddressableKeys.BlockGold;
                case BlockType.Diamond: return AddressableKeys.BlockDiamond;
                default: return AddressableKeys.BlockDirt;
            }
        }
    }
}
