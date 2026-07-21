using System.Collections.Generic;
using DeepEarth.Core;

namespace DeepEarth.Event
{
    public class EventOption
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public List<EffectType> Effects { get; private set; } = new List<EffectType>();
        public RelicData RelicReward { get; private set; }
        public string ItemRewardKey { get; private set; }

        public EventOption(string title, string description, List<EffectType> effects)
        {
            Title = title;
            Description = description;
            Effects = effects;
            RelicReward = null;
            ItemRewardKey = null;
        }

        public EventOption(RelicData relic)
        {
            Title = relic.nameLocKey;
            Description = relic.descLocKey;
            Effects = new List<EffectType>();
            RelicReward = relic;
            ItemRewardKey = null;
        }

        public EventOption(string titleLocKey, string descLocKey, string itemRewardKey)
        {
            Title = titleLocKey;
            Description = descLocKey;
            Effects = new List<EffectType>();
            RelicReward = null;
            ItemRewardKey = itemRewardKey;
        }
    }

    public class EventData
    {
        public string EventTitle { get; private set; }
        public string EventDescription { get; private set; }
        public bool IsTombstone { get; private set; }
        public List<EventOption> Options { get; private set; } = new List<EventOption>();

        public EventData(string title, string description, bool isTombstone, List<EventOption> options)
        {
            EventTitle = title;
            EventDescription = description;
            IsTombstone = isTombstone;
            Options = options;
        }
    }
}
