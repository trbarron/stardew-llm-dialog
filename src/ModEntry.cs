using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using StardewModdingAPI;
using StardewValley;

namespace LLMDialogMod
{
    public class ModEntry : Mod
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly Dictionary<string, string> dialogCache = new Dictionary<string, string>();
        private ModConfig Config;
        private AIDialogueGenerator aiGenerator;
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _mainThreadActions = new();
        private readonly HashSet<string> _processedDialogues = new();
        private DateTime _lastDialogueUpdate = DateTime.MinValue;
        private string _lastProcessedDialogue = "";

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("LLM Dialog Mod Starting Initialization", LogLevel.Info);
            
            try
            {
                Config = helper.ReadConfig<ModConfig>();
                Monitor.Log("Configuration loaded successfully", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error loading configuration: {ex.Message}", LogLevel.Error);
                Config = new ModConfig();
            }
            
            // Set up OpenAI API client
            if (!string.IsNullOrEmpty(Config.OpenAIApiKey))
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.OpenAIApiKey);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error configuring OpenAI API client: {ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                Monitor.Log("WARNING: OpenAI API key not configured - mod will use fallback dialogs", LogLevel.Warn);
            }

            // Register events
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            
            // Initialize AI dialogue generator
            aiGenerator = new AIDialogueGenerator(client, dialogCache, Config, Monitor);

            Monitor.Log("LLM Dialog Mod Initialized (On-Demand Mode)", LogLevel.Info);
        }

        private void OnUpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            // Process actions queued for the main thread
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action();
            }

            // Only run logic if a dialogue is active
            if (!Game1.dialogueUp || Game1.currentSpeaker == null)
            {
                _lastProcessedDialogue = "";
                return;
            }

            // Check if we have a dialogue to process
            if (Game1.currentSpeaker.CurrentDialogue.Count > 0)
            {
                var currentDialogue = Game1.currentSpeaker.CurrentDialogue.Peek();
                string originalText = currentDialogue.getCurrentDialogue();
                string characterName = Game1.currentSpeaker.Name;
                string uniqueId = $"{characterName}:{originalText}";

                // Skip if this is the same dialogue we just processed (cooldown check)
                if (uniqueId == _lastProcessedDialogue && (DateTime.Now - _lastDialogueUpdate).TotalMilliseconds < 500)
                {
                    return;
                }

                // Skip if already processed or is our placeholder
                if (_processedDialogues.Contains(uniqueId) || originalText == "..." || originalText == "Thinking...")
                {
                    return;
                }

                // Only process target characters
                if (!CharacterConfig.TargetCharacters.Contains(characterName))
                {
                    return;
                }

                // Mark as processed to avoid loops
                _processedDialogues.Add(uniqueId);
                _lastProcessedDialogue = uniqueId;
                _lastDialogueUpdate = DateTime.Now;

                Monitor.Log($"Intercepted dialogue for {characterName}", LogLevel.Debug);
                
                // Get current day for context
                var dayOfWeek = (int)(Game1.stats.DaysPlayed % 7);
                string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
                string currentDay = dayNames[dayOfWeek];
                string dialogueKey = "Interaction"; 

                // Construct cache key
                string cacheKey = $"{characterName}_{dialogueKey}_{currentDay}_{originalText.GetHashCode()}";
                
                if (dialogCache.ContainsKey(cacheKey))
                {
                    // Use cached dialogue immediately
                    string cachedResponse = dialogCache[cacheKey];
                    
                    // Mark as processed before updating to prevent re-interception
                    string cachedResponseId = $"{characterName}:{cachedResponse}";
                    _processedDialogues.Add(cachedResponseId);
                    _lastProcessedDialogue = cachedResponseId;
                    _lastDialogueUpdate = DateTime.Now;
                    
                    UpdateDialogueBox(characterName, cachedResponse, originalText);
                    Monitor.Log($"Used cached dialogue for {characterName}", LogLevel.Debug);
                }
                else
                {
                    // Not cached, show loading and wait up to 4 seconds for API response
                    string loadingId = $"{characterName}:...";
                    _processedDialogues.Add(loadingId);
                    _lastProcessedDialogue = loadingId;
                    _lastDialogueUpdate = DateTime.Now;
                    UpdateDialogueBox(characterName, "...", originalText);
                    
                    Task.Run(async () =>
                    {
                        var generateTask = aiGenerator.GenerateAIDialogueAsync(characterName, dialogueKey, originalText, currentDay);
                        
                        // Wait up to 4 seconds for the response
                        if (await Task.WhenAny(generateTask, Task.Delay(8000)) == generateTask)
                        {
                            // Task completed within 4 seconds
                            string aiResponse = await generateTask;
                            dialogCache[cacheKey] = aiResponse;

                            // Schedule UI update on main thread
                            _mainThreadActions.Enqueue(() =>
                            {
                                // Check if dialogue is still up and it's the same speaker
                                if (Game1.dialogueUp && Game1.currentSpeaker != null && 
                                    Game1.currentSpeaker.Name == characterName &&
                                    Game1.currentSpeaker.CurrentDialogue.Count > 0)
                                {
                                    var currentText = Game1.currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue();
                                    if (currentText == "...")
                                    {
                                        // Mark the AI response as processed BEFORE updating to prevent re-interception
                                        string aiResponseId = $"{characterName}:{aiResponse}";
                                        _processedDialogues.Add(aiResponseId);
                                        _lastProcessedDialogue = aiResponseId;
                                        _lastDialogueUpdate = DateTime.Now;
                                        
                                        UpdateDialogueBox(characterName, aiResponse, "...");
                                        Monitor.Log($"Updated dialogue for {characterName}", LogLevel.Debug);
                                    }
                                }
                            });
                        }
                        else
                        {
                            // Timeout - show original dialogue
                            Monitor.Log($"API call took longer than 4 seconds for {characterName}, showing original dialogue", LogLevel.Debug);
                            _mainThreadActions.Enqueue(() =>
                            {
                                if (Game1.dialogueUp && Game1.currentSpeaker != null && 
                                    Game1.currentSpeaker.Name == characterName &&
                                    Game1.currentSpeaker.CurrentDialogue.Count > 0)
                                {
                                    var currentText = Game1.currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue();
                                    if (currentText == "...")
                                    {
                                        UpdateDialogueBox(characterName, originalText, "...");
                                        Monitor.Log($"Showing original dialogue for {characterName} due to timeout", LogLevel.Debug);
                                    }
                                }
                            });
                            
                            // Continue waiting for the response to cache it
                            string aiResponse = await generateTask;
                            dialogCache[cacheKey] = aiResponse;
                        }
                    });
                }
            }
        }

        private void UpdateDialogueBox(string characterName, string newText, string originalText)
        {
            if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox && 
                Game1.currentSpeaker != null && 
                Game1.currentSpeaker.CurrentDialogue.Count > 0)
            {
                // Safety check: Ensure we are replacing the correct dialogue
                // If the user advanced the conversation, the current text will be different
                var currentDialogue = Game1.currentSpeaker.CurrentDialogue.Peek();
                if (currentDialogue.getCurrentDialogue() != originalText)
                {
                    Monitor.Log("Dialogue advanced before AI response ready - skipping replacement", LogLevel.Debug);
                    return;
                }

                // Replace the current dialogue
                Game1.currentSpeaker.CurrentDialogue.Pop(); // Remove original
                Game1.currentSpeaker.CurrentDialogue.Push(new Dialogue(Game1.currentSpeaker, null, newText)); // Add AI
                
                // Force refresh by recreating the box
                Game1.activeClickableMenu = new StardewValley.Menus.DialogueBox(Game1.currentSpeaker.CurrentDialogue.Peek());
            }
        }

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Monitor.Log("SMAPI Content API Ready", LogLevel.Info);
            Monitor.Log("Dialogue assets will be intercepted and modified automatically", LogLevel.Info);
        }
    }

    public class ModConfig
    {
        public string OpenAIApiKey { get; set; } = "";
        public string OpenAIModel { get; set; } = "gpt-3.5-turbo";
        public string PlayerDescription { get; set; } = "A former corporate worker who moved to Stardew Valley to escape city life and run their grandfather's old farm. They're learning to be a farmer and getting to know the local community.";
        
        public Dictionary<string, string> CharacterContexts { get; set; } = new Dictionary<string, string>
        {
            // Marriage candidates
            ["Abigail"] = "A young woman who loves adventure, video games, and exploring. She's brave, independent, and has a rebellious streak. She enjoys playing the flute and going on adventures.",
            ["Alex"] = "An athletic young man who loves sports and working out. He's confident, sometimes cocky, but has a good heart. He dreams of being a professional athlete and loves football.",
            ["Emily"] = "A spiritual and creative woman who works as a bartender. She loves crystals, meditation, and fashion. She's optimistic, sees the good in everyone, and has a unique sense of style.",
            ["Harvey"] = "The town's doctor who is careful, anxious, and worries about everyone's health. He loves planes and radio-controlled aircraft. He's gentle, caring, and sometimes overly cautious.",
            ["Leah"] = "An artist who lives in a cottage by the forest. She's creative, nature-loving, and independent. She enjoys foraging, making art, and spending time in nature. She's passionate about her work and the environment.",
            ["Maru"] = "A brilliant inventor and nurse who loves science and building gadgets. She's helpful, curious about how things work, and dreams of making scientific discoveries. She's Demetrius's daughter.",
            ["Penny"] = "A kind, gentle teacher who loves children and reading. She's shy but caring, and dreams of having her own family. She's very patient and nurturing.",
            ["Sam"] = "A musician who loves playing guitar and hanging out with friends. He works at JojaMart but dreams of being in a band. He's friendly, energetic, and loves music.",
            ["Sebastian"] = "A programmer who loves motorcycles and wants to escape to the city. He's introverted but loyal to his friends. He's Robin's son and Maru's stepbrother.",
            ["Shane"] = "A troubled young man who works at JojaMart. He's initially rude and depressed, dealing with alcoholism and depression. He loves his chickens and has a soft spot for them. He's gruff but has a good heart underneath.",
            ["Elliott"] = "A romantic writer who lives by the beach in a small cabin. He's passionate about literature, loves the ocean, and is very poetic and philosophical.",
            ["Haley"] = "Initially vain and focused on appearance, but has a kind heart underneath. She loves photography and fashion. She's Emily's sister and can be shallow at first but grows as a person.",
            // Non-marriage candidates
            ["Robin"] = "The town carpenter who builds and repairs buildings. She's hardworking, friendly, and takes pride in her craftsmanship. She's Demetrius's wife and Maru's mother.",
            ["Pierre"] = "The owner of Pierre's General Store. He's competitive with JojaMart, loves capitalism, and can be a bit greedy. He's Caroline's husband and Abigail's father.",
            ["Gus"] = "The owner of the Stardrop Saloon. He's friendly, welcoming, and loves cooking. He provides a social hub for the town and is always ready with a meal.",
            ["Lewis"] = "The mayor of Pelican Town. He's responsible, takes his duties seriously, and has been mayor for many years. He has a secret relationship with Marnie.",
            ["Marnie"] = "The owner of Marnie's Ranch who sells animals and animal supplies. She's caring, loves animals, and has a secret relationship with Mayor Lewis.",
            ["Willy"] = "The fisherman who runs the fish shop on the beach. He's experienced, loves the ocean, and teaches fishing. He's gruff but kind-hearted.",
            ["Wizard"] = "A mysterious wizard who lives in a tower. He's knowledgeable about magic, ancient secrets, and the supernatural. He's wise but sometimes cryptic.",
            ["Caroline"] = "Pierre's wife and Abigail's mother. She's friendly, enjoys gardening and tea, and has a mysterious past connection to the Wizard.",
            ["Clint"] = "The town blacksmith who repairs tools and breaks geodes. He's shy, lonely, and has a crush on Emily. He's skilled but lacks confidence.",
            ["Demetrius"] = "A scientist and Robin's husband, Maru's father. He's analytical, loves research, and can be overly scientific in his approach to life.",
            ["Evelyn"] = "George's wife and Alex's grandmother. She's sweet, caring, and loves gardening. She's very supportive of her family.",
            ["George"] = "Evelyn's husband and Alex's grandfather. He's grumpy, uses a wheelchair, and can be difficult but has a good heart underneath.",
            ["Jodi"] = "Sam and Vincent's mother, Kent's wife. She's a caring mother who worries about her family, especially when Kent was away at war.",
            ["Kent"] = "Jodi's husband, Sam and Vincent's father. He's a veteran who struggles with PTSD, but loves his family deeply.",
            ["Linus"] = "A homeless man who lives in a tent by the mountain. He's philosophical, values simplicity, and chooses to live off the grid.",
            ["Pam"] = "Penny's mother who works as a bus driver. She's alcoholic, can be rude, but cares about her daughter despite her problems.",
            ["Sandy"] = "A shopkeeper in the Calico Desert. She's friendly, runs the Oasis shop, and enjoys the desert lifestyle.",
            ["Jas"] = "A young girl who lives with Marnie and Shane. She's sweet, innocent, and loves flowers and animals.",
            ["Vincent"] = "Jodi and Kent's younger son, Sam's brother. He's a playful child who loves toys and games.",
            ["Dwarf"] = "A mysterious dwarf who lives in the mines. He's ancient, speaks in old language, and has knowledge of the valley's history.",
            ["Krobus"] = "A shadow person who lives in the sewers. He's initially hostile but can become a friend. He's lonely and misunderstood."
        };
    }
}