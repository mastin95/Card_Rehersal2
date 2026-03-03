using UnityEngine;
using UnityEditor;
using RogueEngine;
using RogueEngine.Gameplay;

public class CardCreator
{
    [InitializeOnLoadMethod]
    private static void CreateCard()
    {
        string cardPath = "Assets/RogueEngine/Resources/Cards/PhantomStrike.asset";
        if (AssetDatabase.LoadAssetAtPath<CardData>(cardPath) != null)
        {
            return;
        }

        Debug.Log("Creating Phantom Strike Card...");

        // --- 1. Create Attack Ability (Deal 5 Damage) ---
        
        // 1a. Create Effect: Damage
        // EffectDamage has no specific value field, it uses AbilityData.value
        EffectDamage effectDamage = ScriptableObject.CreateInstance<EffectDamage>();
        effectDamage.ignore_shield = false;
        effectDamage.shield_only = false;
        
        string damagePath = "Assets/RogueEngine/Resources/Abilities/PhantomStrike_Damage.asset";
        AssetDatabase.CreateAsset(effectDamage, damagePath);

        // 1b. Create Ability: Attack
        AbilityData abilityAttack = ScriptableObject.CreateInstance<AbilityData>();
        abilityAttack.id = "phantom_strike_attack";
        abilityAttack.title = "Phantom Skeleton Attack";
        abilityAttack.desc = "Deal <value> damage.";
        abilityAttack.trigger = AbilityTrigger.OnPlay;
        abilityAttack.target = AbilityTarget.SelectTarget;
        abilityAttack.value = 5; // Configures damage amount
        abilityAttack.effects = new EffectData[] { effectDamage };
        
        string abilityAttackPath = "Assets/RogueEngine/Resources/Abilities/PhantomStrike_Ability_Attack.asset";
        AssetDatabase.CreateAsset(abilityAttack, abilityAttackPath);


        // --- 2. Create Stealth Ability (Gain Evasive) ---

        // 2a. Load Status: Evasive
        // Note: We use AbilityData.status array instead of an Effect
        StatusData statusEvasive = Resources.Load<StatusData>("Status/evasive");
        
        if (statusEvasive == null)
        {
            Debug.LogError("Could not find Status/evasive. Please check the path.");
            // Try to find it by type if file name differs, but we saw 'evasive.asset' in the list.
        }

        // 2b. Create Ability: Stealth
        AbilityData abilityStealth = ScriptableObject.CreateInstance<AbilityData>();
        abilityStealth.id = "phantom_strike_stealth";
        abilityStealth.title = "Phantom Stealth";
        abilityStealth.desc = "Gain <b>Evasive</b>.";
        abilityStealth.trigger = AbilityTrigger.OnPlay;
        abilityStealth.target = AbilityTarget.CharacterSelf; // Targets self
        abilityStealth.value = 1; // Duration/Stacks for the status
        abilityStealth.status = new StatusData[] { statusEvasive };
        // No effects needed, just status
        
        string abilityStealthPath = "Assets/RogueEngine/Resources/Abilities/PhantomStrike_Ability_Stealth.asset";
        AssetDatabase.CreateAsset(abilityStealth, abilityStealthPath);


        // --- 3. Create Card ---
        CardData card = ScriptableObject.CreateInstance<CardData>();
        
        // ID & Display
        card.id = "phantom_strike";
        card.title = "Phantom Strike";
        // art_icon/full left null (default)

        // Stats
        card.card_type = CardType.Skill;
        card.team = Resources.Load<TeamData>("Teams/neutral"); 
        card.rarity = Resources.Load<RarityData>("Rarities/1-common");
        card.mana = 1;

        // Abilities: Add BOTH abilities
        card.abilities = new AbilityData[] { abilityAttack, abilityStealth };

        // Upgrades (Defaults)
        card.level_max = 1;
        card.cost = 100; // Shop

        // Text
        card.text = "Deal <value> damage. Gain <b>Evasive</b>.";
        
        // Availability
        card.availability = CardAvailability.Available;

        AssetDatabase.CreateAsset(card, cardPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Phantom Strike Card Created Successfully at " + cardPath);
    }
}
