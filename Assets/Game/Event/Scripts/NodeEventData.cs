using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Event
{
    [CreateAssetMenu(fileName = "NodeEvent_New", menuName = "DeepEarth/Event/NodeEventData")]
    public class NodeEventData : ScriptableObject
    {
        [Header("Identity")]
        public NodeEventID  eventID;
        public NodeEventCategory category;

        [Header("Display")]
        public string titleLocKey;
        public string descLocKey;
        public string iconKey;        // Addressable key for event icon/illustration

        [Header("Behaviour")]
        public bool isAutomatic;       // true = single "Continue" button, no real choice
        public List<NodeEventChoice> choices = new List<NodeEventChoice>();

        [Header("Selection Weight")]
        [Range(1, 100)] public int weight = 10;
    }

    [Serializable]
    public class NodeEventChoice
    {
        public string titleLocKey;
        public string descLocKey;
    }
}
