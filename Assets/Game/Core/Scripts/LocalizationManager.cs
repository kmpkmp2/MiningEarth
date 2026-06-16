using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager _instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LocalizationManager");
                    _instance = go.AddComponent<LocalizationManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public string CurrentLanguageCode => SaveManager.CurrentData.Language;

        public event Action OnLanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTranslations();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeTranslations()
        {
            // English translations
            var en = new Dictionary<string, string>
            {
                { "hud_hp", "HP: {0} / {1}" },
                { "hud_depth", "Depth: {0}m" },
                { "hud_bag", "Bag: {0} / {1}" },
                { "hud_iron", "Iron: {0}" },
                { "hud_silver", "Silver: {0}" },
                { "hud_gold", "Gold: {0}" },
                { "hud_diamond", "Diamond: {0}" },
                { "hud_inv_full", "Inventory Full!" },
                { "diff_very_easy", "Very Easy (Soil)" },
                { "diff_easy", "Easy (Cavern)" },
                { "diff_medium", "Medium (Deep Cavern)" },
                { "diff_hard", "Hard (Abyss)" },
                { "diff_very_hard", "Very Hard (Core)" },
                { "go_title", "YOU DIED" },
                { "go_depth", "Depth Reached: {0}m" },
                { "go_will_earned", "Will Gathered: +{0}" },
                { "go_total_will", "Total Will: {0}" },
                { "go_upgrade_power", "Mining Power: Lv.{0}" },
                { "go_upgrade_hp", "Max HP: Lv.{0}" },
                { "go_upgrade_attack", "Attack Power: Lv.{0}" },
                { "go_will_cost", "{0} Will" },
                { "go_upgrade_btn", "UPGRADE" },
                { "go_restart_btn", "MAIN MENU" },
                { "combat_monster_encounter", "Monster Encountered!" },
                { "combat_lava", "LAVA! -{0} HP" },
                { "combat_water", "WATER LOG! -{0} HP" },
                { "combat_miss", "MISS" },
                { "curse_tag", " (CURSE!)" },
                { "lang_toggle", "KO" }, // Clicking in EN toggles to KO
                { "hud_settings_btn", "SETTINGS" },
                { "settings_title", "SETTINGS" },
                { "settings_close", "CLOSE" },
                { "settings_lang_label", "Language" },
                { "go_best_depth", "Best Depth: {0}m" },
                
                // Menu translations
                { "menu_title", "DEEP EARTH" },
                { "menu_play", "PLAY" },
                { "menu_upgrade", "UPGRADES" },
                { "menu_shop", "SHOP" },
                { "menu_settings", "SETTINGS" },
                { "menu_back", "BACK" },
                { "menu_shop_coming_soon", "SHOP COMING SOON!" },
                { "menu_will", "Will: {0}" },
                { "menu_gold_topright", "Gold: {0}" },
                { "menu_upgrade_power", "Mining Power: Lv.{0}" },
                { "menu_upgrade_hp", "Max HP: Lv.{0}" },
                { "menu_upgrade_attack", "Attack Power: Lv.{0}" },
                { "menu_upgrade_inventory", "Bag Capacity: Lv.{0}" },
                { "go_upgrade_inventory", "Bag Capacity: Lv.{0}" },
                
                // Character popup English keys
                { "char_popup_title", "Character Select" },
                { "char_select", "Select" },
                { "char_selected", "Selected" },
                { "char_unlock", "Unlock" },
                { "char_locked", "Locked" },
                { "char_cost_label", "Cost:" },
                { "char_owned_resources", "Owned Resources:" },
                { "char_btn_open", "CHARACTER" },

                { "char_prisoner_name", "Prisoner" },
                { "char_prisoner_desc", "Starting point of all growth. No special passive ability." },
                { "char_mercenary_name", "Mercenary" },
                { "char_mercenary_desc", "Specialized in combat.\nPassive: Attack Power +1\nStarting: Attack Power +1" },
                { "char_miner_name", "Miner" },
                { "char_miner_desc", "Specialized in block breaking.\nPassive: Mining Power +1\nStarting: Mining Power +1" },
                { "char_graverobber_name", "Grave Robber" },
                { "char_graverobber_desc", "Specialized in rare resource farming.\nPassive: 10% chance to yield +10% extra Iron/Silver/Gold/Diamond (min +1)." },
                
                // Event titles
                { "event_chest_title", "Treasure Chest" },
                { "event_tomb_title", "Ancient Tombstone" },
                { "event_chest_desc", "You discovered an old miner's supply chest! Select one upgrade:" },
                { "event_tomb_desc", "An eerie tombstone lies in the darkness. Reading its runes offers powerful buffs, but comes with a heavy curse..." },
                
                // Buffs & Curses Choice Options
                { "event_opt_mining_title", "Mining Gear Upgrade" },
                { "event_opt_mining_desc", "Increases Attack Damage (+1)" },
                { "event_opt_hp_title", "Heart Crystal" },
                { "event_opt_hp_desc", "Increases Maximum HP (+2)" },
                { "event_opt_inv_title", "Expanded Inventory" },
                { "event_opt_inv_desc", "Increases Inventory Capacity (+5)" },
                { "event_opt_stealth_title", "Stealth Cloak" },
                { "event_opt_stealth_desc", "Decreases Monster Encounter Rate (-15%)" },
                { "event_opt_hat_title", "Hard Hat" },
                { "event_opt_hat_desc", "Decreases Trap/Hazard Encounter Rate (-15%)" },
                { "event_opt_demonic_title", "Demonic Power" },
                { "event_opt_demonic_desc", "Significantly increase Attack Damage (+2)\nCurse: Decreases Max HP (-2)" },
                { "event_opt_greed_title", "Greed Pact" },
                { "event_opt_greed_desc", "Significantly increase Inventory Capacity (+10)\nCurse: Increases Monster Encounter Rate (+25%)" },
                { "event_opt_fortitude_title", "Forbidden Fortitude" },
                { "event_opt_fortitude_desc", "Significantly increase Max HP (+4)\nCurse: Increases Hazard/Trap Rate (+25%)" },
                { "event_opt_reckless_title", "Reckless Strike" },
                { "event_opt_reckless_desc", "Significantly increase Attack Damage (+2)\nCurse: 15% chance for mining swings to fail" },
                { "event_opt_vampiric_title", "Vampiric Eye" },
                { "event_opt_vampiric_desc", "Decreases Monster Encounter Rate (-15%)\nCurse: Takes damage (-1 HP) immediately on encountering a monster" },

                // Boss Translations
                { "boss_title", "BOSS" },
                { "boss_rat_name", "Giant Cave Rat" },
                { "boss_spider_name", "Queen Spider" },
                { "boss_golem_name", "Rock Golem" },
                { "boss_worm_name", "Lava Worm" },
                { "boss_titan_name", "Crystal Titan" },
                { "boss_reward_title", "BOSS DEFEATED" },
                { "boss_reward_subtitle", "Select one powerful run-local buff:" },
                { "boss_buff_atk", "Attack Power +2" },
                { "boss_buff_atk_desc", "Increases attack power by +2 for the rest of this run." },
                { "boss_buff_hp", "Max HP +5" },
                { "boss_buff_hp_desc", "Increases maximum HP by +5 and heals 5 HP for this run." },
                { "boss_buff_mining", "Mining Power +2" },
                { "boss_buff_mining_desc", "Increases mining power by +2 for the rest of this run." },
                { "boss_buff_mineral", "Mineral Gain +20%" },
                { "boss_buff_mineral_desc", "Increases all resource yields by +20% for this run." },
                { "boss_buff_spawn", "Encounters -20%" },
                { "boss_buff_spawn_desc", "Decreases normal monster encounter probability by -20% for this run." },
                { "boss_buff_heal", "Heal Drops +15%" },
                { "boss_buff_heal_desc", "Increases healing item drop rate from monsters by +15% for this run." },
                { "boss_rare_revive", "Legendary Resurrection" },
                { "boss_rare_revive_desc", "Upon death, revive once with 50% HP (Rare)." },
                { "boss_rare_boss_dmg", "Boss Slayer" },
                { "boss_rare_boss_dmg_desc", "Deals +50% extra damage to Bosses (Rare)." },
                { "boss_rare_mineral_50", "Miner's Fortune" },
                { "boss_rare_mineral_50_desc", "Increases all resource yields by +50% for this run (Rare)." },
                { "boss_rare_event_double", "Double Blessing" },
                { "boss_rare_event_double_desc", "All event choice buffs are applied twice (Rare)." },
                
                // Effect System
                { "hud_relic_btn", "RELIC" },
                { "hud_inventory_btn", "BAG" },
                { "relic_popup_title", "RELICS & EFFECTS" },
                { "inventory_popup_title", "INVENTORY DETAILS" },
                { "relic_popup_type_label", "Type: {0}" },
                { "relic_popup_effect_label", "Effect: {0}" },

                // Inventory System
                { "hud_quantity_label", "Quantity:" },
                { "hud_hp_heal", "HP Healed!" },
                { "inv_confirm_drop_title", "Really drop?" },
                { "inv_confirm_yes", "Confirm" },
                { "inv_confirm_no", "Cancel" },
                { "item_stone_name", "Stone" },
                { "item_stone_desc", "A heavy piece of stone." },
                { "item_wood_name", "Wood" },
                { "item_wood_desc", "Dry wood firewood." },
                { "item_iron_name", "Iron" },
                { "item_iron_desc", "Cold iron ore. Used for equipment crafting." },
                { "item_silver_name", "Silver" },
                { "item_silver_desc", "Shining silver ore." },
                { "item_gold_name", "Gold" },
                { "item_gold_desc", "Glittering gold ore." },
                { "item_diamond_name", "Diamond" },
                { "item_diamond_desc", "Brilliant diamond gem." },
                { "item_potion_name", "Health Potion" },
                { "item_potion_desc", "Restores 5 HP." },
                { "item_key_name", "Chest Key" },
                { "item_key_desc", "Looks like it can open locked chests." },
                { "item_chest_name", "Treasure Box" },
                { "item_chest_desc", "Contains random rewards." },
                { "item_special_name", "Elixir" },
                { "item_special_desc", "A special elixir with mysterious energy. Restores 10 HP." },
                
                { "effect_type_CharacterPassive", "CharacterPassive" },
                { "effect_type_BossReward", "BossReward" },
                { "effect_type_Buff", "Buff" },
                { "effect_type_Debuff", "Debuff" },
                { "effect_type_Special", "Special" },
                
                { "effect_passive_miner_desc", "Mining Power +1" },
                { "effect_passive_mercenary_desc", "Attack Power +1" },
                { "effect_passive_graverobber_desc", "10% chance for +10% extra resource" },
                
                { "effect_buff_attack_name", "Attack Power Up" },
                { "effect_buff_attack_desc", "Attack Power +{0}" },
                { "effect_buff_maxhp_name", "Max HP Up" },
                { "effect_buff_maxhp_desc", "Maximum HP +{0}" },
                { "effect_buff_inventory_name", "Bag Expansion" },
                { "effect_buff_inventory_desc", "Inventory Capacity +{0}" },
                { "effect_buff_monsterspawn_name", "Safe Zone" },
                { "effect_buff_monsterspawn_desc", "Monster Spawn Rate -{0}%" },
                { "effect_buff_hazardspawn_name", "Hazard Safety" },
                { "effect_buff_hazardspawn_desc", "Trap Spawn Rate -{0}%" },
                
                { "effect_curse_attack_name", "Weakness Curse" },
                { "effect_curse_attack_desc", "Attack Power -{0}" },
                { "effect_curse_maxhp_name", "Decay Curse" },
                { "effect_curse_maxhp_desc", "Maximum HP -{0}" },
                { "effect_curse_monsterspawn_name", "Ominous Sign" },
                { "effect_curse_monsterspawn_desc", "Monster Spawn Rate +{0}%" },
                { "effect_curse_hazardspawn_name", "Calamity Curse" },
                { "effect_curse_hazardspawn_desc", "Trap Spawn Rate +{0}%" },
                { "effect_curse_instantdamage_name", "Blood Spill" },
                { "effect_curse_instantdamage_desc", "Takes -{0} HP instantly on monster encounter" },
                { "effect_curse_miningfail_name", "Broken Gear" },
                { "effect_curse_miningfail_desc", "Mining Swing Fail Chance +{0}%" },
                
                { "effect_boss_attack_name", "Cave Rat Slayer" },
                { "effect_boss_attack_desc", "Attack Power +2" },
                { "effect_boss_maxhp_name", "Boss Fortitude" },
                { "effect_boss_maxhp_desc", "Maximum HP +5" },
                { "effect_boss_mining_name", "Destroyer" },
                { "effect_boss_mining_desc", "Mining Power +2" },
                { "effect_boss_mineral20_name", "Greed Symbol" },
                { "effect_boss_mineral20_desc", "Mineral Yield +20%" },
                { "effect_boss_spawndecrease_name", "Stealth" },
                { "effect_boss_spawndecrease_desc", "Monster Spawn Rate -20%" },
                { "effect_boss_healdrop_name", "Life Essence" },
                { "effect_boss_healdrop_desc", "Monster Heal Drop Chance +15%" },
                { "effect_boss_revive_name", "Phoenix Feather" },
                { "effect_boss_revive_desc", "Revive 1 time with 100% HP" },
                { "effect_boss_bossdamage_name", "Giant Hunter" },
                { "effect_boss_bossdamage_desc", "Damage to Bosses +50%" },
                { "effect_boss_mineral50_name", "Millionaire" },
                { "effect_boss_mineral50_desc", "Mineral Yield +50%" },
                { "effect_boss_doubleevent_name", "Double Blessing" },
                { "effect_boss_doubleevent_desc", "Doubles event buff effects" }
            };

            // Korean translations
            var ko = new Dictionary<string, string>
            {
                { "hud_hp", "체력: {0} / {1}" },
                { "hud_depth", "깊이: {0}m" },
                { "hud_bag", "가방: {0} / {1}" },
                { "hud_iron", "철: {0}" },
                { "hud_silver", "은: {0}" },
                { "hud_gold", "금: {0}" },
                { "hud_diamond", "다이아: {0}" },
                { "hud_inv_full", "가방이 가득 찼습니다!" },
                { "diff_very_easy", "매우 쉬움 (흙)" },
                { "diff_easy", "쉬움 (동굴)" },
                { "diff_medium", "보통 (깊은 동굴)" },
                { "diff_hard", "어려움 (심연)" },
                { "diff_very_hard", "매우 어려움 (지하 핵)" },
                { "go_title", "사망하셨습니다" },
                { "go_depth", "도달한 깊이: {0}m" },
                { "go_will_earned", "획득한 의지: +{0}" },
                { "go_total_will", "보유한 의지: {0}" },
                { "go_upgrade_power", "채굴력: Lv.{0}" },
                { "go_upgrade_hp", "최대 체력: Lv.{0}" },
                { "go_upgrade_attack", "공격력: Lv.{0}" },
                { "go_will_cost", "{0} 의지" },
                { "go_upgrade_btn", "업그레이드" },
                { "go_restart_btn", "메인 메뉴로" },
                { "combat_monster_encounter", "몬스터 출현!" },
                { "combat_lava", "용암! -{0} 체력" },
                { "combat_water", "침수! -{0} 체력" },
                { "combat_miss", "빗나감" },
                { "curse_tag", " (저주!)" },
                { "lang_toggle", "EN" }, // Clicking in KO toggles to EN
                { "hud_settings_btn", "설정" },
                { "settings_title", "설정" },
                { "settings_close", "종료" },
                { "settings_lang_label", "언어" },
                { "go_best_depth", "최고 깊이: {0}m" },
                
                // Menu translations
                { "menu_title", "디프 어스" },
                { "menu_play", "게임 시작" },
                { "menu_upgrade", "업그레이드" },
                { "menu_shop", "상점" },
                { "menu_settings", "설정" },
                { "menu_back", "뒤로가기" },
                { "menu_shop_coming_soon", "상점 준비 중!" },
                { "menu_will", "보유한 의지: {0}" },
                { "menu_gold_topright", "보유 골드: {0}" },
                { "menu_upgrade_power", "채굴력: Lv.{0}" },
                { "menu_upgrade_hp", "최대 체력: Lv.{0}" },
                { "menu_upgrade_attack", "공격력: Lv.{0}" },
                { "menu_upgrade_inventory", "인벤토리 용량: Lv.{0}" },
                { "go_upgrade_inventory", "인벤토리 용량: Lv.{0}" },
                
                // Character popup Korean keys
                { "char_popup_title", "캐릭터 선택" },
                { "char_select", "선택" },
                { "char_selected", "선택됨" },
                { "char_unlock", "해금" },
                { "char_locked", "잠김" },
                { "char_cost_label", "해금 비용:" },
                { "char_owned_resources", "보유 자원:" },
                { "char_btn_open", "캐릭터" },

                { "char_prisoner_name", "죄인" },
                { "char_prisoner_desc", "모든 성장의 시작점. 특별한 고유 능력이 없습니다." },
                { "char_mercenary_name", "용병" },
                { "char_mercenary_desc", "전투에 특화된 캐릭터.\n패시브: 기본 공격력 +1\n시작 능력: 기본 공격력 +1" },
                { "char_miner_name", "광부" },
                { "char_miner_desc", "블록 파괴에 특화된 캐릭터.\n패시브: 추가 채굴력 +1\n시작 능력: 채굴력 +1" },
                { "char_graverobber_name", "도굴꾼" },
                { "char_graverobber_desc", "희귀 자원 수급에 특화된 캐릭터.\n패시브: 철/은/금/다이아 획득 시 10% 확률로 10% 추가 획득 (최소 +1)." },
                
                // Event titles
                { "event_chest_title", "보물 상자" },
                { "event_tomb_title", "고대의 무덤" },
                { "event_chest_desc", "오래된 광부의 보급 상자를 발견했습니다! 업그레이드 하나를 선택하세요:" },
                { "event_tomb_desc", "어둠 속에 섬뜩한 무덤이 놓여 있습니다. 이 룬을 읽으면 강력한 버프를 얻지만, 무거운 저주가 따릅니다..." },
                
                // Buffs & Curses Choice Options
                { "event_opt_mining_title", "채굴 장비 업그레이드" },
                { "event_opt_mining_desc", "공격력 증가 (+1)" },
                { "event_opt_hp_title", "하트 크리스탈" },
                { "event_opt_hp_desc", "최대 체력 증가 (+2)" },
                { "event_opt_inv_title", "인벤토리 확장" },
                { "event_opt_inv_desc", "인벤토리 용량 증가 (+5)" },
                { "event_opt_stealth_title", "은신의 망토" },
                { "event_opt_stealth_desc", "몬스터 조우 확률 감소 (-15%)" },
                { "event_opt_hat_title", "안전모" },
                { "event_opt_hat_desc", "위험/함정 조우 확률 감소 (-15%)" },
                { "event_opt_demonic_title", "악마의 힘" },
                { "event_opt_demonic_desc", "공격력 대폭 증가 (+2)\n저주: 최대 체력 감소 (-2)" },
                { "event_opt_greed_title", "탐욕의 계약" },
                { "event_opt_greed_desc", "인벤토리 용량 대폭 증가 (+10)\n저주: 몬스터 조우 확률 증가 (+25%)" },
                { "event_opt_fortitude_title", "금지된 인내" },
                { "event_opt_fortitude_desc", "최대 체력 대폭 증가 (+4)\n저주: 위험/함정 확률 증가 (+25%)" },
                { "event_opt_reckless_title", "무모한 타격" },
                { "event_opt_reckless_desc", "공격력 대폭 증가 (+2)\n저주: 채굴 시 15% 확률로 헛스윙" },
                { "event_opt_vampiric_title", "흡혈귀의 눈" },
                { "event_opt_vampiric_desc", "몬스터 조우 확률 감소 (-15%)\n저주: 몬스터 조우 시 즉시 피해 (-1 체력)" },

                // Boss Translations
                { "boss_title", "보스" },
                { "boss_rat_name", "거대 동굴쥐" },
                { "boss_spider_name", "여왕 거미" },
                { "boss_golem_name", "암석 골렘" },
                { "boss_worm_name", "용암 벌레" },
                { "boss_titan_name", "수정 거신" },
                { "boss_reward_title", "보스 처치 완료" },
                { "boss_reward_subtitle", "이번 런 동안 유지될 강력한 버프 1개를 선택하세요:" },
                { "boss_buff_atk", "공격력 +2" },
                { "boss_buff_atk_desc", "이번 런 동안 공격력이 2 증가합니다." },
                { "boss_buff_hp", "최대 체력 +5" },
                { "boss_buff_hp_desc", "이번 런 동안 최대 체력이 5 증가하고 체력을 5 회복합니다." },
                { "boss_buff_mining", "채굴력 +2" },
                { "boss_buff_mining_desc", "이번 런 동안 채굴력이 2 증가합니다." },
                { "boss_buff_mineral", "광물 획득량 +20%" },
                { "boss_buff_mineral_desc", "이번 런 동안 획득하는 광물이 20% 증가합니다." },
                { "boss_buff_spawn", "몬스터 조우 확률 -20%" },
                { "boss_buff_spawn_desc", "이번 런 동안 일반 몬스터 등장 확률이 20% 감소합니다." },
                { "boss_buff_heal", "회복 아이템 드랍률 +15%" },
                { "boss_buff_heal_desc", "이번 런 동안 전투 승리 시 회복 아이템 드랍률이 15% 증가합니다." },
                { "boss_rare_revive", "사망 시 1회 부활" },
                { "boss_rare_revive_desc", "사망 시 50% 체력으로 즉시 1회 부활합니다 (희귀)." },
                { "boss_rare_boss_dmg", "보스 사냥꾼" },
                { "boss_rare_boss_dmg_desc", "보스 몬스터에게 주는 피해가 50% 증가합니다 (희귀)." },
                { "boss_rare_mineral_50", "광부의 대박" },
                { "boss_rare_mineral_50_desc", "이번 런 동안 모든 광물 획득량이 50% 증가합니다 (희귀)." },
                { "boss_rare_event_double", "이중 축복" },
                { "boss_rare_event_double_desc", "이벤트 선택으로 얻는 버프 효과가 2배로 증가합니다 (희귀)." },
                
                // Effect System
                { "hud_relic_btn", "유물" },
                { "hud_inventory_btn", "가방" },
                { "relic_popup_title", "유물 및 효과" },
                { "inventory_popup_title", "가방 정보" },
                { "relic_popup_type_label", "종류 : {0}" },
                { "relic_popup_effect_label", "효과 : {0}" },

                // Inventory System
                { "hud_quantity_label", "보유 수량:" },
                { "hud_hp_heal", "체력 회복!" },
                { "inv_confirm_drop_title", "정말 버리시겠습니까?" },
                { "inv_confirm_yes", "확인" },
                { "inv_confirm_no", "취소" },
                { "item_stone_name", "돌" },
                { "item_stone_desc", "단단한 돌멩이다." },
                { "item_wood_name", "나무" },
                { "item_wood_desc", "흔하게 볼 수 있는 나무 장작이다." },
                { "item_iron_name", "철" },
                { "item_iron_desc", "차가운 철 광석이다. 장비 제작 등에 쓰인다." },
                { "item_silver_name", "은" },
                { "item_silver_desc", "빛나는 은 광석이다." },
                { "item_gold_name", "금" },
                { "item_gold_desc", "번쩍이는 금 광석이다." },
                { "item_diamond_name", "다이아" },
                { "item_diamond_desc", "눈부시게 빛나는 다이아몬드이다." },
                { "item_potion_name", "회복약" },
                { "item_potion_desc", "체력을 5 회복한다." },
                { "item_key_name", "열쇠" },
                { "item_key_desc", "굳게 닫힌 상자를 열 수 있을 것 같다." },
                { "item_chest_name", "보물상자" },
                { "item_chest_desc", "무언가 좋은 것이 들어있을 것 같은 상자다." },
                { "item_special_name", "특수 소모품" },
                { "item_special_desc", "신비로운 에너지가 깃든 특수 소모품이다. 사용 시 체력을 10 회복한다." },
                
                { "effect_type_CharacterPassive", "CharacterPassive" },
                { "effect_type_BossReward", "BossReward" },
                { "effect_type_Buff", "Buff" },
                { "effect_type_Debuff", "Debuff" },
                { "effect_type_Special", "Special" },
                
                { "effect_passive_miner_desc", "채굴력 +1" },
                { "effect_passive_mercenary_desc", "공격력 +1" },
                { "effect_passive_graverobber_desc", "철/은/금/다이아 획득 시 10% 확률로 10% 추가 획득" },
                
                { "effect_buff_attack_name", "공격력 증가" },
                { "effect_buff_attack_desc", "공격력 +{0}" },
                { "effect_buff_maxhp_name", "최대 체력 증가" },
                { "effect_buff_maxhp_desc", "최대 체력 +{0}" },
                { "effect_buff_inventory_name", "가방 확장" },
                { "effect_buff_inventory_desc", "인벤토리 크기 +{0}" },
                { "effect_buff_monsterspawn_name", "안전 지대" },
                { "effect_buff_monsterspawn_desc", "몬스터 조우 확률 -{0}%" },
                { "effect_buff_hazardspawn_name", "안전 장비" },
                { "effect_buff_hazardspawn_desc", "함정 조우 확률 -{0}%" },
                
                { "effect_curse_attack_name", "쇠약의 저주" },
                { "effect_curse_attack_desc", "공격력 -{0}" },
                { "effect_curse_maxhp_name", "쇠퇴의 저주" },
                { "effect_curse_maxhp_desc", "최대 체력 -{0}" },
                { "effect_curse_monsterspawn_name", "불길한 저주" },
                { "effect_curse_monsterspawn_desc", "몬스터 조우 확률 +{0}%" },
                { "effect_curse_hazardspawn_name", "재앙의 저주" },
                { "effect_curse_hazardspawn_desc", "함정 조우 확률 +{0}%" },
                { "effect_curse_instantdamage_name", "피의 저주" },
                { "effect_curse_instantdamage_desc", "몬스터 조우 시 즉시 피해 -{0} 체력" },
                { "effect_curse_miningfail_name", "부서진 장비" },
                { "effect_curse_miningfail_desc", "채굴 실패 확률 +{0}%" },
                
                { "effect_boss_attack_name", "거대 쥐 토벌자" },
                { "effect_boss_attack_desc", "공격력 +2" },
                { "effect_boss_maxhp_name", "보스의 굳건함" },
                { "effect_boss_maxhp_desc", "최대 체력 +5" },
                { "effect_boss_mining_name", "파괴자" },
                { "effect_boss_mining_desc", "채굴력 +2" },
                { "effect_boss_mineral20_name", "탐욕의 상징" },
                { "effect_boss_mineral20_desc", "광물 획득량 +20%" },
                { "effect_boss_spawndecrease_name", "잠행" },
                { "effect_boss_spawndecrease_desc", "몬스터 조우 확률 -20%" },
                { "effect_boss_healdrop_name", "생명의 정수" },
                { "effect_boss_healdrop_desc", "처치 시 체력 회복 확률 +15%" },
                { "effect_boss_revive_name", "불사조의 깃털" },
                { "effect_boss_revive_desc", "사망 시 1회 부활 (체력 100% 회복)" },
                { "effect_boss_bossdamage_name", "거수 사냥꾼" },
                { "effect_boss_bossdamage_desc", "보스에게 주는 데미지 +50%" },
                { "effect_boss_mineral50_name", "대부호" },
                { "effect_boss_mineral50_desc", "광물 획득량 +50%" },
                { "effect_boss_doubleevent_name", "이중 축복" },
                { "effect_boss_doubleevent_desc", "이벤트 선택 시 얻는 버프 효과 2배" }
            };

            _translations["en"] = en;
            _translations["ko"] = ko;
        }

        public string GetTranslation(string key)
        {
            string code = CurrentLanguageCode;
            if (string.IsNullOrEmpty(code) || !_translations.ContainsKey(code))
            {
                code = "en"; // Default fallback
            }

            if (_translations[code].TryGetValue(key, out string val))
            {
                return val;
            }

            // Fallback to English
            if (_translations["en"].TryGetValue(key, out string fallbackVal))
            {
                return fallbackVal;
            }

            return key; // Return key itself if not found
        }

        public string GetFormatted(string key, params object[] args)
        {
            try
            {
                string format = GetTranslation(key);
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error formatting translation for key '{key}': {ex.Message}");
                return key;
            }
        }

        public void SetLanguage(string langCode)
        {
            if (langCode == "ko" || langCode == "en")
            {
                SaveManager.CurrentData.Language = langCode;
                SaveManager.Save();
                OnLanguageChanged?.Invoke();
            }
        }

        public void ToggleLanguage()
        {
            string next = (CurrentLanguageCode == "ko") ? "en" : "ko";
            SetLanguage(next);
        }
    }
}
