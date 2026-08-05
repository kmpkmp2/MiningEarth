using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Map
{
    [Serializable]
    public class RoomWeightEntry
    {
        public RoomType Type;
        [Range(0f, 100f)]
        public float Weight;
    }

    /// <summary>
    /// ScriptableObject holding room-type assignment parameters.
    /// Fixed floors (Treasure / Rest) are specified in 1-indexed floor numbers matching the design spec.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomGenerationConfig", menuName = "DeepEarth/Map/RoomGenerationConfig")]
    public class RoomGenerationConfig : ScriptableObject
    {
        [Header("고정 방 (1-indexed floor)")]
        [SerializeField] private int _treasureFloor = 9;
        [SerializeField] private int _restFloor     = 50;

        [Header("가중치 랜덤 배정 (Treasure 제외)")]
        [SerializeField] private List<RoomWeightEntry> _weights = new List<RoomWeightEntry>
        {
            new RoomWeightEntry { Type = RoomType.Mine,     Weight = 55f },
            new RoomWeightEntry { Type = RoomType.Monster,  Weight = 15f },
            new RoomWeightEntry { Type = RoomType.Event,    Weight = 8f  },
            new RoomWeightEntry { Type = RoomType.Merchant, Weight = 5f  },
            new RoomWeightEntry { Type = RoomType.Elite,    Weight = 7f  },
            new RoomWeightEntry { Type = RoomType.Rest,     Weight = 10f },
        };

        /// <summary>Zero-based index of the fixed Treasure floor.</summary>
        public int TreasureFloorIndex => _treasureFloor - 1;

        /// <summary>Zero-based index of the fixed Rest floor (last floor before Boss).</summary>
        public int RestFloorIndex => _restFloor - 1;

        /// <summary>Picks a random room type from the weight table.</summary>
        public RoomType PickRoomType(IRandomProvider rng) => PickRoomType(rng, 1f);

        /// <summary>Picks a random room type from the weight table, with an optional multiplier applied only to RoomType.Event
        /// (Adventurer 캐릭터의 시작 유물 "낡은 나침반" 효과 — Event 노드 등장확률 가산).</summary>
        public RoomType PickRoomType(IRandomProvider rng, float eventWeightMultiplier)
        {
            float total = 0f;
            foreach (var e in _weights)
                total += (e.Type == RoomType.Event) ? e.Weight * eventWeightMultiplier : e.Weight;

            float roll       = rng.Range(0f, total);
            float cumulative = 0f;
            foreach (var e in _weights)
            {
                cumulative += (e.Type == RoomType.Event) ? e.Weight * eventWeightMultiplier : e.Weight;
                if (roll <= cumulative) return e.Type;
            }
            return RoomType.Mine;
        }
    }
}
