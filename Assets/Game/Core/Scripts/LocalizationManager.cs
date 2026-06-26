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
                { "achievement_btn_open", "ACHIEVEMENTS" },
                { "achievement_popup_title", "Achievements" },
                { "achievement_unlocked_header", "Achievement Unlocked!" },
                { "achievement_completed_label", "Completed" },
                { "achievement_hidden_name", "???" },
                { "achievement_hidden_desc", "Complete to reveal." },

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
                { "effect_boss_doubleevent_desc", "Doubles event buff effects" },

                { "hud_pickaxe_broken", "Broken" },
                { "pickaxe_broken_alert_title", "Pickaxe is broken!" },
                { "pickaxe_broken_alert_desc", "Mining will now consume HP." },
                { "pickaxe_repair_full", "Durability is already full!" },
                { "pickaxe_repair_not_enough", "Not enough materials!" },

                // Achievement names / descs
                { "ach_first_step_name", "First Step" },
                { "ach_first_step_desc", "Start your first run." },
                { "ach_depth50_name", "Fifty Fathoms" },
                { "ach_depth50_desc", "Reach depth 50." },
                { "ach_depth100_name", "Deep Dive" },
                { "ach_depth100_desc", "Reach depth 100." },
                { "ach_depth200_name", "The Abyss" },
                { "ach_depth200_desc", "Reach depth 200." },
                { "ach_kill10_name", "Monster Slayer" },
                { "ach_kill10_desc", "Defeat 10 monsters." },
                { "ach_kill50_name", "Exterminator" },
                { "ach_kill50_desc", "Defeat 50 monsters." },
                { "ach_boss_first_name", "First Blood" },
                { "ach_boss_first_desc", "Defeat your first boss." },
                { "ach_boss_cave_rat_name", "Rat Catcher" },
                { "ach_boss_cave_rat_desc", "Defeat the Cave Rat boss." },
                { "ach_ore_iron50_name", "Iron Will" },
                { "ach_ore_iron50_desc", "Mine 50 iron ore." },
                { "ach_ore_diamond10_name", "Diamond Hunter" },
                { "ach_ore_diamond10_desc", "Mine 10 diamond ore." },
                { "ach_relic1_name", "Collector" },
                { "ach_relic1_desc", "Collect your first relic." },
                { "ach_relic5_name", "Hoarder" },
                { "ach_relic5_desc", "Collect 5 relics." },
                { "ach_death1_name", "It Hurts" },
                { "ach_death1_desc", "Die for the first time." },
                { "ach_repair1_name", "Handyman" },
                { "ach_repair1_desc", "Repair your pickaxe once." },
                { "ach_lava1_name", "Playing with Fire" },
                { "ach_lava1_desc", "Survive a lava encounter." },
                { "ach_treasure5_name", "Treasure Hunter" },
                { "ach_treasure5_desc", "Open 5 treasure chests." },

                { "effect_type_StatusEffect", "Status Effect" },

                { "status_burn_name", "Burn" },
                { "status_burn_desc", "Takes burn damage each action turn" },

                { "relic_heat_resistant_charm_name", "Heat Resistant Charm" },
                { "relic_heat_resistant_charm_desc", "Burn duration -3 turns" },
                { "relic_frozen_crystal_name", "Frozen Crystal" },
                { "relic_frozen_crystal_desc", "Burn damage -1" },
                { "relic_firefighter_helmet_name", "Firefighter Helmet" },
                { "relic_firefighter_helmet_desc", "Burn duration -2 turns, damage -1" },
                { "relic_purification_ring_name", "Purification Ring" },
                { "relic_purification_ring_desc", "50% chance to negate Burn" },
                { "relic_burning_heart_name", "Burning Heart" },
                { "relic_burning_heart_desc", "Attack +2, Burn duration +3 turns" },
                { "relic_lava_contract_name", "Lava Contract" },
                { "relic_lava_contract_desc", "Resource +25%, Burn damage +1" },
                { "relic_cursed_ash_name", "Cursed Ash" },
                { "relic_cursed_ash_desc", "Max HP +5, Burn duration +5 turns" },
                { "relic_fire_cult_mask_name", "Fire Cult Mask" },
                { "relic_fire_cult_mask_desc", "Monster Attack +2, Burn damage +2" },

                // Shop tabs
                { "shop_tab_pickaxe",    "Pickaxe" },
                { "shop_tab_character",  "Character" },
                { "shop_tab_consumable", "Item" },
                { "shop_tab_special",    "Special" },
                { "shop_owned",          "OWNED" },
                { "shop_info_select",    "Select an item" },

                // Pickaxe Shop
                { "shop_pickaxe_title",        "Pickaxes" },
                { "shop_pickaxe_mining_power", "⛏ Mining Power: {0}" },
                { "shop_pickaxe_durability",   "♦ Max Durability: {0}" },
                { "shop_pickaxe_buy",          "BUY" },
                { "shop_pickaxe_equip",        "EQUIP" },
                { "shop_pickaxe_equipped",     "EQUIPPED" },

                { "pickaxe_wood_name",    "Wood Pickaxe" },
                { "pickaxe_wood_desc",    "A basic pickaxe. Reliable but weak." },
                { "pickaxe_iron_name",    "Iron Pickaxe" },
                { "pickaxe_iron_desc",    "Forged from iron. Breaks ore faster." },
                { "pickaxe_silver_name",  "Silver Pickaxe" },
                { "pickaxe_silver_desc",  "A refined pickaxe. Cuts through stone with ease." },
                { "pickaxe_gold_name",    "Gold Pickaxe" },
                { "pickaxe_gold_desc",    "A heavy pickaxe of great power." },
                { "pickaxe_diamond_name", "Diamond Pickaxe" },
                { "pickaxe_diamond_desc", "The ultimate pickaxe. Nothing can stop it." },

                // Mining Power buff/curse effects
                { "effect_buff_mining_name", "Mining Power Up" },
                { "effect_buff_mining_desc", "Mining Power +{0}" },
                { "effect_curse_mining_name", "Mining Curse" },
                { "effect_curse_mining_desc", "Mining Power -{0}" },

                // New Treasure Relics - Mining
                { "relic_miners_gloves_name", "Miner's Gloves" },
                { "relic_miners_gloves_desc", "Mining Power +1" },
                { "relic_mining_king_helmet_name", "Mining King's Helmet" },
                { "relic_mining_king_helmet_desc", "Mining Power +2" },
                { "relic_broken_drill_name", "Broken Drill" },
                { "relic_broken_drill_desc", "Mining Power +3, Max HP -5" },
                { "relic_ore_detector_name", "Ore Detector" },
                { "relic_ore_detector_desc", "Mining Power +1, Resource Yield +20%" },

                // New Treasure Relics - Combat
                { "relic_warriors_ring_name", "Warrior's Ring" },
                { "relic_warriors_ring_desc", "Attack Power +1" },
                { "relic_mercenary_medal_name", "Mercenary's Medal" },
                { "relic_mercenary_medal_desc", "Attack Power +2" },
                { "relic_blood_contract_name", "Blood Contract" },
                { "relic_blood_contract_desc", "Attack Power +3, Max HP -5" },
                { "relic_hunters_eye_name", "Hunter's Eye" },
                { "relic_hunters_eye_desc", "Attack Power +1, Monster Encounter +10%" },

                // New Tombstone Relics
                { "relic_cursed_pickaxe_name", "Cursed Pickaxe" },
                { "relic_cursed_pickaxe_desc", "Mining Power +2, Burn Damage +1" },
                { "relic_madness_sword_name", "Blade of Madness" },
                { "relic_madness_sword_desc", "Attack Power +2, Monster Encounter +15%" },
                { "relic_cursed_mining_gloves_name", "Cursed Mining Gloves" },
                { "relic_cursed_mining_gloves_desc", "Mining Power +1, Max HP -3" },
                { "relic_blood_rune_name", "Blood Rune" },
                { "relic_blood_rune_desc", "Attack Power +1, Burn Duration +2 turns" }
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
                { "achievement_btn_open", "업적" },
                { "achievement_popup_title", "업적" },
                { "achievement_unlocked_header", "업적 달성!" },
                { "achievement_completed_label", "달성" },
                { "achievement_hidden_name", "???" },
                { "achievement_hidden_desc", "달성 시 공개됩니다." },

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
                { "effect_boss_doubleevent_desc", "이벤트 선택 시 얻는 버프 효과 2배" },

                { "hud_pickaxe_broken", "파손" },
                { "pickaxe_broken_alert_title", "곡괭이가 파손되었습니다." },
                { "pickaxe_broken_alert_desc", "이제 채굴 시 체력을 소모합니다." },
                { "pickaxe_repair_full", "내구도가 이미 최대입니다." },
                { "pickaxe_repair_not_enough", "재료가 부족합니다." },

                // 업적 이름/설명
                { "ach_first_step_name", "첫 발걸음" },
                { "ach_first_step_desc", "첫 번째 런을 시작합니다." },
                { "ach_depth50_name", "50미 아래로" },
                { "ach_depth50_desc", "깊이 50에 도달합니다." },
                { "ach_depth100_name", "심연을 향해" },
                { "ach_depth100_desc", "깊이 100에 도달합니다." },
                { "ach_depth200_name", "지하 심층" },
                { "ach_depth200_desc", "깊이 200에 도달합니다." },
                { "ach_kill10_name", "몬스터 사냥꾼" },
                { "ach_kill10_desc", "몬스터 10마리를 처치합니다." },
                { "ach_kill50_name", "해충 구제사" },
                { "ach_kill50_desc", "몬스터 50마리를 처치합니다." },
                { "ach_boss_first_name", "첫 번째 피" },
                { "ach_boss_first_desc", "첫 번째 보스를 처치합니다." },
                { "ach_boss_cave_rat_name", "쥐 사냥꾼" },
                { "ach_boss_cave_rat_desc", "동굴쥐 보스를 처치합니다." },
                { "ach_ore_iron50_name", "철의 의지" },
                { "ach_ore_iron50_desc", "철 광석을 50개 채굴합니다." },
                { "ach_ore_diamond10_name", "다이아몬드 사냥꾼" },
                { "ach_ore_diamond10_desc", "다이아몬드 광석을 10개 채굴합니다." },
                { "ach_relic1_name", "수집가" },
                { "ach_relic1_desc", "처음으로 유물을 수집합니다." },
                { "ach_relic5_name", "사재기꾼" },
                { "ach_relic5_desc", "유물을 5개 수집합니다." },
                { "ach_death1_name", "아프다" },
                { "ach_death1_desc", "처음으로 사망합니다." },
                { "ach_repair1_name", "수리공" },
                { "ach_repair1_desc", "곡괭이를 한 번 수리합니다." },
                { "ach_lava1_name", "불장난" },
                { "ach_lava1_desc", "용암 조우에서 살아남습니다." },
                { "ach_treasure5_name", "보물 사냥꾼" },
                { "ach_treasure5_desc", "보물 상자를 5개 엽니다." },

                { "effect_type_StatusEffect", "상태이상" },

                { "status_burn_name", "화상" },
                { "status_burn_desc", "행동 시 화상 피해를 입는 지속 상태이상" },

                { "relic_heat_resistant_charm_name", "내열 부적" },
                { "relic_heat_resistant_charm_desc", "화상 지속 시간 3턴 감소" },
                { "relic_frozen_crystal_name", "냉동 수정" },
                { "relic_frozen_crystal_desc", "화상 피해 1 감소" },
                { "relic_firefighter_helmet_name", "소방관 헬멧" },
                { "relic_firefighter_helmet_desc", "화상 지속 2턴, 피해 1 감소" },
                { "relic_purification_ring_name", "정화의 반지" },
                { "relic_purification_ring_desc", "50% 확률로 화상 무효" },
                { "relic_burning_heart_name", "불타는 심장" },
                { "relic_burning_heart_desc", "공격력 +2, 화상 지속 시간 3턴 증가" },
                { "relic_lava_contract_name", "용암 계약" },
                { "relic_lava_contract_desc", "광물 획득량 +25%, 화상 피해 1 증가" },
                { "relic_cursed_ash_name", "저주받은 재" },
                { "relic_cursed_ash_desc", "최대 체력 +5, 화상 지속 시간 5턴 증가" },
                { "relic_fire_cult_mask_name", "불의 교단 가면" },
                { "relic_fire_cult_mask_desc", "몬스터 공격력 +2, 화상 피해 2 증가" },

                // 상점 탭
                { "shop_tab_pickaxe",    "곡괭이" },
                { "shop_tab_character",  "캐릭터" },
                { "shop_tab_consumable", "아이템" },
                { "shop_tab_special",    "특수" },
                { "shop_owned",          "보유 중" },
                { "shop_info_select",    "상품을 선택하세요" },

                // 곡괭이 상점
                { "shop_pickaxe_title",        "곡괭이" },
                { "shop_pickaxe_mining_power", "⛏ 채굴력: {0}" },
                { "shop_pickaxe_durability",   "♦ 최대 내구도: {0}" },
                { "shop_pickaxe_buy",          "구매" },
                { "shop_pickaxe_equip",        "장착" },
                { "shop_pickaxe_equipped",     "장착 중" },

                { "pickaxe_wood_name",    "나무 곡괭이" },
                { "pickaxe_wood_desc",    "기본 곡괭이. 튼튼하지만 약하다." },
                { "pickaxe_iron_name",    "철 곡괭이" },
                { "pickaxe_iron_desc",    "철로 단조된 곡괭이. 광석을 더 빠르게 부순다." },
                { "pickaxe_silver_name",  "은 곡괭이" },
                { "pickaxe_silver_desc",  "정제된 곡괭이. 돌을 쉽게 뚫는다." },
                { "pickaxe_gold_name",    "금 곡괭이" },
                { "pickaxe_gold_desc",    "강력한 힘을 가진 묵직한 곡괭이." },
                { "pickaxe_diamond_name", "다이아 곡괭이" },
                { "pickaxe_diamond_desc", "최강의 곡괭이. 무엇도 막을 수 없다." },

                // 채굴력 버프/디버프 효과
                { "effect_buff_mining_name", "채굴력 증가" },
                { "effect_buff_mining_desc", "채굴력 +{0}" },
                { "effect_curse_mining_name", "채굴력 저주" },
                { "effect_curse_mining_desc", "채굴력 -{0}" },

                // 신규 보물 유물 - 채굴 계열
                { "relic_miners_gloves_name", "광부의 장갑" },
                { "relic_miners_gloves_desc", "채굴력 +1" },
                { "relic_mining_king_helmet_name", "채굴왕의 헬멧" },
                { "relic_mining_king_helmet_desc", "채굴력 +2" },
                { "relic_broken_drill_name", "부서진 드릴" },
                { "relic_broken_drill_desc", "채굴력 +3, 최대 체력 -5" },
                { "relic_ore_detector_name", "광맥 탐지기" },
                { "relic_ore_detector_desc", "채굴력 +1, 광물 획득량 +20%" },

                // 신규 보물 유물 - 전투 계열
                { "relic_warriors_ring_name", "전사의 반지" },
                { "relic_warriors_ring_desc", "공격력 +1" },
                { "relic_mercenary_medal_name", "용병의 훈장" },
                { "relic_mercenary_medal_desc", "공격력 +2" },
                { "relic_blood_contract_name", "피의 계약서" },
                { "relic_blood_contract_desc", "공격력 +3, 최대 체력 -5" },
                { "relic_hunters_eye_name", "사냥꾼의 눈" },
                { "relic_hunters_eye_desc", "공격력 +1, 몬스터 조우 확률 +10%" },

                // 신규 무덤 저주 유물
                { "relic_cursed_pickaxe_name", "저주받은 곡괭이" },
                { "relic_cursed_pickaxe_desc", "채굴력 +2, 화상 피해 +1" },
                { "relic_madness_sword_name", "광기의 검" },
                { "relic_madness_sword_desc", "공격력 +2, 몬스터 조우 확률 +15%" },
                { "relic_cursed_mining_gloves_name", "저주받은 채굴 장갑" },
                { "relic_cursed_mining_gloves_desc", "채굴력 +1, 최대 체력 -3" },
                { "relic_blood_rune_name", "피의 룬" },
                { "relic_blood_rune_desc", "공격력 +1, 화상 지속 시간 +2턴" }
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
