using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "MerchantQuoteData", menuName = "DeepEarth/MerchantQuoteData")]
    public class MerchantQuoteData : ScriptableObject
    {
        public List<string> quoteLocKeys = new List<string>();

        public string legendaryRelicQuoteKey = "merchant_quote_legendary_relic";
        public string legendaryItemQuoteKey = "merchant_quote_legendary_item";
        public string noFundsQuoteKey = "merchant_quote_no_funds";

        public string GetRandomQuoteKey()
        {
            if (quoteLocKeys == null || quoteLocKeys.Count == 0) return string.Empty;
            return quoteLocKeys[Random.Range(0, quoteLocKeys.Count)];
        }
    }
}
