using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Map;

namespace DeepEarth.UI
{
    [Serializable]
    public class NodeIconEntry
    {
        public RoomType Type;
        public Sprite   Icon;
    }

    /// <summary>ScriptableObject that maps each RoomType to a display sprite.</summary>
    [CreateAssetMenu(fileName = "NodeIconData", menuName = "DeepEarth/Map/NodeIconData")]
    public class NodeIconData : ScriptableObject
    {
        [SerializeField] private List<NodeIconEntry> entries = new List<NodeIconEntry>();

        public Sprite GetIcon(RoomType type)
        {
            var entry = entries.Find(e => e.Type == type);
            return entry?.Icon;
        }
    }
}
