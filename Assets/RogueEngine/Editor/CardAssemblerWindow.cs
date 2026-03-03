using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using RogueEngine;
using RogueEngine.Gameplay;
using System;
using System.Reflection;

public class CardAssemblerWindow : EditorWindow
{
    // --- Basic Info ---
    private string cardID = "new_card";
    private string cardTitle = "New Card";
    private string cardDesc = "Card Description";
    private CardType cardType = CardType.Skill;
    private int manaCost = 1;
    private int shopCost = 100;

    // --- References ---
    private Sprite cardSprite;
    private TeamData teamData;
    private RarityData rarityData;

    // --- Character / Folder Selection ---
    private int selectedCharacterIndex = 0;
    private string[] characterFolders = new string[0];
    private string cardsRootPath = "Assets/RogueEngine/Resources/Cards";

    // --- Abilities Section ---
    private class AbilityEntry
    {
        public AbilityTrigger trigger = AbilityTrigger.OnPlay;
        public AbilityTarget target = AbilityTarget.SelectTarget;
        public int value = 5;
        public int selectedEffectIndex = 0;
        public int selectedStatusIndex = 0;
    }
    
    private List<AbilityEntry> abilityEntries = new List<AbilityEntry>() { new AbilityEntry() };
    
    // UI Global Scroll Position
    private Vector2 mainScrollPosition = Vector2.zero;

    // Available Types to select from Memory
    private string[] effectTypeNames = new string[0];
    private Type[] effectTypes = new Type[0];
    
    private StatusData[] availableStatuses = new StatusData[0];
    private string[] statusNames = new string[0];

    [MenuItem("Rogue Engine/Card Assembler")]
    public static void ShowWindow()
    {
        GetWindow<CardAssemblerWindow>("Card Assembler");
    }

    private void OnEnable()
    {
        LoadCharacterFolders();
        LoadEffectTypes();
        LoadStatuses();

        // Default Loads
        teamData = Resources.Load<TeamData>("Teams/neutral");
        rarityData = Resources.Load<RarityData>("Rarities/1-common");
    }

    private void LoadCharacterFolders()
    {
        if (Directory.Exists(cardsRootPath))
        {
            string[] dirs = Directory.GetDirectories(cardsRootPath);
            characterFolders = new string[dirs.Length + 1];
            characterFolders[0] = "Root (Cards)";
            for (int i = 0; i < dirs.Length; i++)
            {
                characterFolders[i + 1] = new DirectoryInfo(dirs[i]).Name;
            }
        }
        else
        {
            characterFolders = new string[] { "Root (Cards)" };
        }
    }

    private void LoadEffectTypes()
    {
        // Find all classes that inherit from EffectData via Reflection
        var allEffectTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(domainAssembly => domainAssembly.GetTypes())
            .Where(type => typeof(EffectData).IsAssignableFrom(type) && !type.IsAbstract)
            .ToArray();

        effectTypes = new Type[allEffectTypes.Length + 1];
        effectTypeNames = new string[allEffectTypes.Length + 1];

        effectTypes[0] = null; // None option
        effectTypeNames[0] = "--- NO EFFECT ---";

        for (int i = 0; i < allEffectTypes.Length; i++)
        {
            effectTypes[i + 1] = allEffectTypes[i];
            effectTypeNames[i + 1] = allEffectTypes[i].Name;
        }
    }

    private void LoadStatuses()
    {
        availableStatuses = Resources.LoadAll<StatusData>("Status");
        statusNames = new string[availableStatuses.Length + 1];
        
        statusNames[0] = "--- NO STATUS ---";
        for (int i = 0; i < availableStatuses.Length; i++)
        {
            statusNames[i + 1] = availableStatuses[i].name;
        }
    }

    private void OnGUI()
    {
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

        GUILayout.Label("Assemble New Card", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Basic Info Section
        GUILayout.Label("1. Basic Information", EditorStyles.boldLabel);
        cardID = EditorGUILayout.TextField("Card ID (Internal)", cardID);
        cardTitle = EditorGUILayout.TextField("Card Title (Display)", cardTitle);
        
        EditorGUILayout.LabelField("Description");
        cardDesc = EditorGUILayout.TextArea(cardDesc, GUILayout.Height(50));
        
        cardType = (CardType)EditorGUILayout.EnumPopup("Card Type", cardType);
        manaCost = EditorGUILayout.IntField("Mana Cost", manaCost);
        shopCost = EditorGUILayout.IntField("Shop Cost", shopCost);

        EditorGUILayout.Space();

        // 2. Organization Section
        GUILayout.Label("2. Organization & Visuals", EditorStyles.boldLabel);
        selectedCharacterIndex = EditorGUILayout.Popup("Save in Folder", selectedCharacterIndex, characterFolders);
        teamData = (TeamData)EditorGUILayout.ObjectField("Team (Faction)", teamData, typeof(TeamData), false);
        rarityData = (RarityData)EditorGUILayout.ObjectField("Rarity", rarityData, typeof(RarityData), false);
        cardSprite = (Sprite)EditorGUILayout.ObjectField("Card Art (Optional)", cardSprite, typeof(Sprite), false);

        EditorGUILayout.Space();

        // 3. Abilities Section
        GUILayout.Label("3. Abilities", EditorStyles.boldLabel);
        
        // Use an IntField instead of IntSlider so there's no hard limit (or set a very high one).
        int newAbilityCount = EditorGUILayout.IntField("Number of Abilities", abilityEntries.Count);
        newAbilityCount = Mathf.Max(1, newAbilityCount); // At least 1

        // Dynamically adjust the list size based on the number input
        while (abilityEntries.Count < newAbilityCount)
        {
            abilityEntries.Add(new AbilityEntry());
        }
        while (abilityEntries.Count > newAbilityCount)
        {
            abilityEntries.RemoveAt(abilityEntries.Count - 1);
        }

        // Render each ability field
        for (int i = 0; i < abilityEntries.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label($"Ability {i + 1}", EditorStyles.boldLabel);
            abilityEntries[i].trigger = (AbilityTrigger)EditorGUILayout.EnumPopup("Trigger", abilityEntries[i].trigger);
            abilityEntries[i].target = (AbilityTarget)EditorGUILayout.EnumPopup("Target", abilityEntries[i].target);
            abilityEntries[i].value = EditorGUILayout.IntField("Power/Value", abilityEntries[i].value);
            
            abilityEntries[i].selectedEffectIndex = EditorGUILayout.Popup("Main Effect", abilityEntries[i].selectedEffectIndex, effectTypeNames);
            abilityEntries[i].selectedStatusIndex = EditorGUILayout.Popup("Apply Status", abilityEntries[i].selectedStatusIndex, statusNames);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // 4. Assemble Button
        if (GUILayout.Button("Assemble & Create Card", GUILayout.Height(40)))
        {
            AssembleCard();
        }
        
        if (GUILayout.Button("Refresh Data (If you added new Effects/Statuses)", GUILayout.Height(20)))
        {
            OnEnable();
        }

        EditorGUILayout.EndScrollView();
    }

    private void AssembleCard()
    {
        if (string.IsNullOrEmpty(cardID))
        {
            Debug.LogError("Card Assembler: Card ID cannot be empty.");
            return;
        }

        // Determine Save Path
        string folderName = characterFolders[selectedCharacterIndex];
        string characterFolderPath = folderName == "Root (Cards)" ? cardsRootPath : cardsRootPath + "/" + folderName;
        
        string abilitiesFolderPath = characterFolderPath + "/Abilities";
        
        // Ensure directories exist
        if (!AssetDatabase.IsValidFolder(characterFolderPath))
        {
            string parentPath = folderName == "Root (Cards)" ? "Assets/RogueEngine/Resources" : cardsRootPath;
            string newFolder = folderName == "Root (Cards)" ? "Cards" : folderName;
            AssetDatabase.CreateFolder(parentPath, newFolder);
        }

        if (!AssetDatabase.IsValidFolder(abilitiesFolderPath))
        {
            AssetDatabase.CreateFolder(characterFolderPath, "Abilities");
        }

        string finalCardPath = characterFolderPath + $"/{cardID}.asset";

        if (AssetDatabase.LoadAssetAtPath<CardData>(finalCardPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Overwrite Warning", $"A card with ID {cardID} already exists at {finalCardPath}. Overwrite?", "Yes", "No"))
            {
                return;
            }
        }

        List<AbilityData> generatedAbilities = new List<AbilityData>();

        for (int i = 0; i < abilityEntries.Count; i++)
        {
            AbilityData ab = BuildAbility(i + 1, abilityEntries[i].trigger, abilityEntries[i].target, abilityEntries[i].value, abilityEntries[i].selectedEffectIndex, abilityEntries[i].selectedStatusIndex, abilitiesFolderPath);
            if (ab != null) generatedAbilities.Add(ab);
        }

        // Build Final Card
        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        newCard.id = cardID;
        newCard.title = cardTitle;
        newCard.text = cardDesc;
        newCard.card_type = cardType;
        newCard.mana = manaCost;
        newCard.cost = shopCost;
        newCard.level_max = 1;
        newCard.team = teamData;
        newCard.rarity = rarityData;
        newCard.art_icon = cardSprite; // Placeholder or assigned art
        newCard.availability = CardAvailability.Available;
        newCard.abilities = generatedAbilities.ToArray();

        AssetDatabase.CreateAsset(newCard, finalCardPath);
        
        // Save and Refresh
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Card '{cardTitle}' assembled successfully at {finalCardPath}</color>");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newCard;
    }

    private AbilityData BuildAbility(int index, AbilityTrigger trigger, AbilityTarget target, int value, int effectIndex, int statusIndex, string saveFolder)
    {
        // If neither effect nor status is selected, creating an ability makes no sense.
        if (effectIndex == 0 && statusIndex == 0) return null;

        AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
        ability.id = $"{cardID}_ability{index}";
        ability.title = $"{cardTitle} Ability {index}";
        ability.trigger = trigger;
        ability.target = target;
        ability.value = value;

        // Process Effect
        if (effectIndex > 0)
        {
            Type specificEffectType = effectTypes[effectIndex];
            EffectData effectInstance = (EffectData)ScriptableObject.CreateInstance(specificEffectType);
            
            string effectPath = saveFolder + $"/{cardID}_effect{index}.asset";
            AssetDatabase.CreateAsset(effectInstance, effectPath);
            
            ability.effects = new EffectData[] { effectInstance };
        }

        // Process Status
        if (statusIndex > 0)
        {
            StatusData statusInstance = availableStatuses[statusIndex - 1]; // -1 because index 0 is "NO STATUS"
            ability.status = new StatusData[] { statusInstance };
        }

        string abilityPath = saveFolder + $"/{cardID}_ability{index}.asset";
        AssetDatabase.CreateAsset(ability, abilityPath);

        return ability;
    }
}
