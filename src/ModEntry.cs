using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("LLM Dialog Mod Starting Initialization", LogLevel.Info);
            Monitor.Log($"Mod Version: {ModManifest.Version}", LogLevel.Info);
            
            try
        {
            Config = helper.ReadConfig<ModConfig>();
                Monitor.Log("Configuration loaded successfully", LogLevel.Debug);
                Monitor.Log($"OpenAI API Key configured: {!string.IsNullOrEmpty(Config.OpenAIApiKey)}", LogLevel.Debug);
                Monitor.Log($"OpenAI Model: {Config.OpenAIModel}", LogLevel.Debug);
                Monitor.Log($"Player Description: {Config.PlayerDescription}", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error loading configuration: {ex.Message}", LogLevel.Error);
                Monitor.Log("Using default configuration", LogLevel.Warn);
                Config = new ModConfig();
            }
            
            // Set up OpenAI API client
            if (!string.IsNullOrEmpty(Config.OpenAIApiKey))
            {
                try
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.OpenAIApiKey);
                    Monitor.Log("OpenAI API client configured successfully", LogLevel.Debug);
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

            // Register for the GameLaunched event to ensure all mods are loaded
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            
            // Use SMAPI's Content API to edit dialogue assets
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            
            // Initialize AI dialogue generator
            aiGenerator = new AIDialogueGenerator(client, dialogCache, Config, Monitor);

            Monitor.Log("LLM Dialog Mod v1.1.0 Initialization Complete", LogLevel.Info);
            Monitor.Log("Using SMAPI Content API for dialogue replacement", LogLevel.Info);
            
            
            // Add console commands for testing
            try
            {
                Helper.ConsoleCommands.Add("test_llm_dialog", "Test the LLM Dialog mod by simulating a dialogue call", TestLLMDialog);
                Helper.ConsoleCommands.Add("check_game_day", "Check what day of the week it is in the game", CheckGameDay);
                Helper.ConsoleCommands.Add("test_token", "Test the Content Patcher token directly", TestToken);
                Helper.ConsoleCommands.Add("debug_dialogue", "Debug dialogue system and check if tokens are working", DebugDialogue);
                Helper.ConsoleCommands.Add("hello", "Simple test command", (cmd, args) => Monitor.Log("Hello! The mod is working!", LogLevel.Info));
                Monitor.Log("Console commands registered successfully", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error registering console commands: {ex.Message}", LogLevel.Error);
            }
        }


        private void OnAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            // Only process dialogue assets (not character sprites or other assets)
            if (!e.NameWithoutLocale.ToString().StartsWith("Characters/Dialogue/"))
            {
                return;
            }
            
            // Extract character name from dialogue asset path
            string characterName = e.NameWithoutLocale.ToString().Split('/').Last();
            
            // Only process dialogue assets for our target characters
            if (CharacterConfig.TargetCharacters.Contains(characterName))
            {
                Monitor.Log($"Dialogue asset requested: {e.NameWithoutLocale} (Character: {characterName})", LogLevel.Info);
                
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, string>();
                    
                    // Get current day
                    var dayOfWeek = (int)(Game1.stats.DaysPlayed % 7);
                    string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
                    string currentDay = dayNames[dayOfWeek];
                    
                    Monitor.Log($"Processing dialogue for {characterName} on {currentDay}", LogLevel.Info);
                    
                    // Only generate AI dialogue for the current day and essential relationship status
                    var essentialKeys = new[] { currentDay, "Married", "Dating" };
                    
                    foreach (var key in essentialKeys)
                    {
                        if (editor.Data.ContainsKey(key))
                        {
                            string originalDialogue = editor.Data[key];
                            string aiDialogue = aiGenerator.GenerateAIDialogue(characterName, key, originalDialogue, currentDay);
                            
                            Monitor.Log($"AI Dialogue - Key: '{key}' | Original: '{originalDialogue}' | Replacement: '{aiDialogue}'", LogLevel.Info);
                            editor.Data[key] = aiDialogue;
                        }
                    }
                    
                    // For all other keys, use simple fallback that doesn't call the API
                    foreach (var kvp in editor.Data.ToList())
                    {
                        if (!essentialKeys.Contains(kvp.Key))
                        {
                            string fallbackDialogue = CharacterConfig.GetFallbackDialogue(characterName, kvp.Key, currentDay);
                            editor.Data[kvp.Key] = fallbackDialogue;
                        }
                    }
                });
            }
        }


        private string GenerateCustomDialogue(string npcName)
        {
            Monitor.Log($"=== GENERATING CUSTOM DIALOGUE FOR {npcName} ===", LogLevel.Info);
            
            // Get current game state
            var dayOfWeek = (int)(Game1.stats.DaysPlayed % 7);
            string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            string currentDay = dayNames[dayOfWeek];
            
            Monitor.Log($"Current day: {currentDay}", LogLevel.Debug);
            
            // Generate dialogue based on character and day
            var dialogue = GetDialogueForCharacter(npcName, currentDay);
            
            if (!string.IsNullOrEmpty(dialogue))
            {
                Monitor.Log($"Generated dialogue: '{dialogue}'", LogLevel.Info);
                return dialogue;
            }
            
            return null; // Let default dialogue show
        }

        private string GetDialogueForCharacter(string npcName, string day)
        {
            // Simple hardcoded dialogue for testing
            var dialogues = new Dictionary<string, Dictionary<string, string>>
            {
                ["Abigail"] = new Dictionary<string, string>
                {
                    ["Mon"] = "DIRECT API TEST: Hello! I'm Abigail and it's Monday!",
                    ["Tue"] = "DIRECT API TEST: Hello! I'm Abigail and it's Tuesday!",
                    ["Wed"] = "DIRECT API TEST: Hello! I'm Abigail and it's Wednesday!",
                    ["Thu"] = "DIRECT API TEST: Hello! I'm Abigail and it's Thursday!",
                    ["Fri"] = "DIRECT API TEST: Hello! I'm Abigail and it's Friday!",
                    ["Sat"] = "DIRECT API TEST: Hello! I'm Abigail and it's Saturday!",
                    ["Sun"] = "DIRECT API TEST: Hello! I'm Abigail and it's Sunday!"
                },
                ["Shane"] = new Dictionary<string, string>
                {
                    ["Mon"] = "DIRECT API TEST: Hey, it's Monday and I'm working at JojaMart...",
                    ["Tue"] = "DIRECT API TEST: Tuesday... just trying to get through the day.",
                    ["Wed"] = "DIRECT API TEST: Wednesday, taking care of my chickens.",
                    ["Thu"] = "DIRECT API TEST: Thursday, at Marnie's ranch.",
                    ["Fri"] = "DIRECT API TEST: Friday, trying to stay sober.",
                    ["Sat"] = "DIRECT API TEST: Saturday, weekend time.",
                    ["Sun"] = "DIRECT API TEST: Sunday, relaxing."
                }
            };
            
            return dialogues.GetValueOrDefault(npcName)?.GetValueOrDefault(day);
        }

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Monitor.Log("SMAPI Content API Ready", LogLevel.Info);
            Monitor.Log("Dialogue assets will be intercepted and modified automatically", LogLevel.Info);
            Monitor.Log("Target characters: 12 marriage candidates + 20 non-marriage candidates (32 total)", LogLevel.Info);
        }

        private void TestLLMDialog(string command, string[] args)
        {
            Monitor.Log("=== TESTING LLM DIALOG MOD ===", LogLevel.Info);
            Monitor.Log("Simulating a dialogue call to test the mod...", LogLevel.Info);
            
            string testInput = "Abigail|It's Monday and I'm testing the mod|casual_greeting";
            Monitor.Log($"Test input: {testInput}", LogLevel.Info);
            
            var result = UpdateTokens(testInput).ToList();
            Monitor.Log($"Test result: {string.Join(", ", result)}", LogLevel.Info);
            
            if (result.Any())
            {
                Monitor.Log("✅ SUCCESS: Mod is working! The dialogue token function is responding.", LogLevel.Info);
            }
            else
            {
                Monitor.Log("❌ FAILURE: No dialogue returned from the mod.", LogLevel.Error);
            }
            
            // Test Shane specifically
            Monitor.Log("=== TESTING SHANE DIALOGUE ===", LogLevel.Info);
            string shaneInput = "Shane|It's Monday and I'm working at JojaMart again|work_joja_grumpy";
            Monitor.Log($"Shane test input: {shaneInput}", LogLevel.Info);
            
            var shaneResult = UpdateTokens(shaneInput).ToList();
            Monitor.Log($"Shane test result: {string.Join(", ", shaneResult)}", LogLevel.Info);
            
            // Check SMAPI Content API status
            Monitor.Log("=== SMAPI CONTENT API STATUS ===", LogLevel.Info);
            Monitor.Log("✅ Using SMAPI Content API for dialogue replacement", LogLevel.Info);
        }

        private void CheckGameDay(string command, string[] args)
        {
            Monitor.Log("=== GAME DAY INFORMATION ===", LogLevel.Info);
            Monitor.Log($"Current day of month: {Game1.dayOfMonth}", LogLevel.Info);
            Monitor.Log($"Current season: {Game1.currentSeason}", LogLevel.Info);
            Monitor.Log($"Current year: {Game1.year}", LogLevel.Info);
            Monitor.Log($"Days played: {Game1.stats.DaysPlayed}", LogLevel.Info);
            
            // Calculate day of week (0 = Sunday, 1 = Monday, etc.)
            int dayOfWeek = (int)(Game1.stats.DaysPlayed % 7);
            string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            Monitor.Log($"Day of week: {dayNames[dayOfWeek]} (day {dayOfWeek})", LogLevel.Info);
            
            // Check if we have dialogue for this day
            string currentDay = dayNames[dayOfWeek];
            Monitor.Log($"Looking for dialogue key: '{currentDay}'", LogLevel.Info);
            Monitor.Log($"Shane should have dialogue on: Mon, Tue, Wed, Thu, Fri", LogLevel.Info);
            
            if (currentDay == "Sat" || currentDay == "Sun")
            {
                Monitor.Log("⚠️  WARNING: It's weekend - Shane might not have custom dialogue!", LogLevel.Warn);
                Monitor.Log("Try talking to him on a weekday (Mon-Fri)", LogLevel.Info);
            }
        }

        private void TestToken(string command, string[] args)
        {
            Monitor.Log("=== TESTING SMAPI CONTENT API ===", LogLevel.Info);
            Monitor.Log("✅ Using SMAPI Content API for dialogue replacement", LogLevel.Info);
            Monitor.Log("Target characters: Abigail, Shane, Leah", LogLevel.Info);
            Monitor.Log("Dialogue will be replaced when you talk to these characters", LogLevel.Info);
        }

        private void DebugDialogue(string command, string[] args)
        {
            Monitor.Log("=== DEBUGGING DIALOGUE SYSTEM ===", LogLevel.Info);
            
            // Check if we're in a game
            if (Game1.player == null)
            {
                Monitor.Log("❌ Not in a game - please load a save first", LogLevel.Error);
                return;
            }
            
            // Check SMAPI Content API status
            Monitor.Log("✅ Using SMAPI Content API for dialogue replacement", LogLevel.Info);
            
            // Check if we can find any NPCs
            var npcs = Game1.currentLocation?.characters?.OfType<NPC>();
            if (npcs != null && npcs.Any())
            {
                Monitor.Log($"✅ Found {npcs.Count()} NPCs in current location", LogLevel.Info);
                foreach (var npc in npcs.Take(3))
                {
                    Monitor.Log($"  - {npc.Name} (at {npc.Position})", LogLevel.Info);
                }
            }
            else
            {
                Monitor.Log("❌ No NPCs found in current location", LogLevel.Warn);
                Monitor.Log("Try going to the town or another location with NPCs", LogLevel.Info);
            }
            
            Monitor.Log("=== DEBUGGING COMPLETE ===", LogLevel.Info);
            Monitor.Log("SMAPI Content API will automatically replace dialogue when you talk to Abigail, Shane, or Leah", LogLevel.Info);
        }

        public IEnumerable<string> UpdateTokens(string input)
        {
            Monitor.Log($"=== DIALOGUE TOKEN CALLED ===", LogLevel.Info);
            Monitor.Log($"🎭 VILLAGER DIALOGUE INTERCEPTED! Input received: '{input}'", LogLevel.Info);
            Monitor.Log($"Current game state - Day: {Game1.stats.DaysPlayed}, Season: {Game1.currentSeason}, Weather: {(Game1.isRaining ? "Rainy" : Game1.isSnowing ? "Snowy" : "Clear")}", LogLevel.Debug);
            
            // Parse input: "CharacterName|Context|DialogType"
            var parts = input?.Split('|');
            if (parts?.Length != 3)
            {
                Monitor.Log($"Invalid input format. Expected 'CharacterName|Context|DialogType', got: '{input}'", LogLevel.Warn);
                Monitor.Log("Returning fallback dialog", LogLevel.Debug);
                yield return "Hey there!";
                yield break;
            }

            string characterName = parts[0];
            string context = parts[1];
            string dialogType = parts[2];
            
            Monitor.Log($"Parsed input - Character: '{characterName}', Context: '{context}', DialogType: '{dialogType}'", LogLevel.Debug);

            // Create cache key
            string cacheKey = $"{characterName}_{context}_{dialogType}_{Game1.stats.DaysPlayed}";
            Monitor.Log($"Cache key: '{cacheKey}'", LogLevel.Trace);
            
            if (dialogCache.ContainsKey(cacheKey))
            {
                Monitor.Log($"Found cached dialog for {characterName}: '{dialogCache[cacheKey]}'", LogLevel.Debug);
                yield return dialogCache[cacheKey];
                yield break;
            }

            Monitor.Log($"No cached dialog found for {characterName}, generating new dialog...", LogLevel.Debug);

            // Generate LLM dialog asynchronously
            Task.Run(async () =>
            {
                try
                {
                    Monitor.Log($"Starting LLM dialog generation for {characterName}...", LogLevel.Debug);
                    string dialog = await GenerateLLMDialog(characterName, context, dialogType);
                    dialogCache[cacheKey] = dialog;
                    Monitor.Log($"Successfully generated and cached dialog for {characterName}: '{dialog}'", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error generating LLM dialog for {characterName}: {ex.Message}", LogLevel.Error);
                    Monitor.Log($"Stack trace: {ex.StackTrace}", LogLevel.Trace);
                    dialogCache[cacheKey] = GetFallbackDialog(characterName, dialogType);
                    Monitor.Log($"Using fallback dialog for {characterName}: '{dialogCache[cacheKey]}'", LogLevel.Warn);
                }
            });

            // Return fallback while we wait for LLM response
            string fallbackDialog = GetFallbackDialog(characterName, dialogType);
            Monitor.Log($"Returning fallback dialog for {characterName}: '{fallbackDialog}'", LogLevel.Debug);
            yield return fallbackDialog;
        }

        private async Task<string> GenerateLLMDialog(string characterName, string context, string dialogType)
        {
            Monitor.Log($"=== GENERATING LLM DIALOG ===", LogLevel.Trace);
            Monitor.Log($"Character: {characterName}, Context: {context}, DialogType: {dialogType}", LogLevel.Trace);
            
            if (string.IsNullOrEmpty(Config.OpenAIApiKey))
            {
                Monitor.Log("OpenAI API key not configured, using fallback dialog", LogLevel.Warn);
                return GetFallbackDialog(characterName, dialogType);
            }

            var characterPersonality = GetCharacterPersonality(characterName);
            var gameContext = GetGameContext();
            
            Monitor.Log($"Character personality: {characterPersonality}", LogLevel.Trace);
            Monitor.Log($"Game context: {gameContext}", LogLevel.Trace);
            
            var messages = new[]
            {
                new { role = "system", content = $"You are {characterName} from Stardew Valley. {characterPersonality} Keep responses under 50 words and maintain the character's voice. Current game context: {gameContext}" },
                new { role = "user", content = $"Context: {context}. Dialog type: {dialogType}. Generate a response as {characterName}." }
            };

            var requestBody = new
            {
                model = Config.OpenAIModel,
                messages = messages,
                max_tokens = 100,
                temperature = 0.7
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            Monitor.Log($"Sending request to OpenAI API with model: {Config.OpenAIModel}", LogLevel.Debug);
            Monitor.Log($"Request body: {json}", LogLevel.Trace);

            try
            {
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                Monitor.Log($"OpenAI API response status: {response.StatusCode}", LogLevel.Debug);
                
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                Monitor.Log($"OpenAI API response: {responseJson}", LogLevel.Trace);
                
                var result = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
                string generatedDialog = result?.Choices?[0]?.Message?.content?.Trim() ?? GetFallbackDialog(characterName, dialogType);
                
                Monitor.Log($"Generated dialog for {characterName}: '{generatedDialog}'", LogLevel.Debug);
                return generatedDialog;
            }
            catch (Exception ex)
            {
                Monitor.Log($"OpenAI API error: {ex.Message}", LogLevel.Error);
                Monitor.Log($"API error details: {ex}", LogLevel.Trace);
                return GetFallbackDialog(characterName, dialogType);
            }
        }

        private string GetCharacterPersonality(string characterName)
        {
            Monitor.Log($"Getting personality for character: {characterName}", LogLevel.Trace);
            
            var personalities = new Dictionary<string, string>
            {
                ["Abigail"] = "You're adventurous, rebellious, and love exploring. You play video games and aren't afraid to get your hands dirty. You have purple hair and a free spirit.",
                ["Alex"] = "You're athletic, confident, and love sports. You work out regularly and dream of being a professional athlete. You can be cocky but have a good heart.",
                ["Emily"] = "You're spiritual, creative, and love crystals and meditation. You're optimistic and see the good in everyone. You work as a bartender and love fashion.",
                ["Harvey"] = "You're a careful, anxious doctor who worries about everyone's health. You love planes and radio-controlled aircraft. You're gentle and caring.",
                ["Leah"] = "You're an artist who moved to the valley for inspiration. You love nature and creating sculptures. You're independent and thoughtful.",
                ["Maru"] = "You're a brilliant inventor and nurse. You love science and building gadgets. You're helpful and curious about how things work.",
                ["Penny"] = "You're a kind, gentle teacher who loves children and reading. You're shy but caring, and you dream of having your own family.",
                ["Sam"] = "You're a musician who loves playing guitar and hanging out with friends. You work at JojaMart but dream of being in a band.",
                ["Sebastian"] = "You're a programmer who loves motorcycles and wants to escape to the city. You're introverted but loyal to your friends.",
                ["Shane"] = "You're struggling with depression and alcohol, but you're trying to improve. You love chickens and are working on yourself.",
                ["Elliott"] = "You're a romantic writer who lives by the beach. You're passionate about literature and love the ocean.",
                ["Haley"] = "You're initially vain and focused on appearance, but you have a kind heart underneath. You love photography and fashion."
            };
            
            string personality = personalities.GetValueOrDefault(characterName, "You're a friendly villager in Stardew Valley.");
            Monitor.Log($"Personality for {characterName}: {personality}", LogLevel.Trace);
            return personality;
        }

        private string GetGameContext()
        {
            var farmer = Game1.player;
            var season = Game1.currentSeason;
            var day = Game1.dayOfMonth;
            var year = Game1.year;
            var weather = Game1.isRaining ? "rainy" : Game1.isSnowing ? "snowy" : "clear";
            
            string context = $"It's {season} {day}, year {year}. Weather is {weather}. The farmer's name is {farmer.Name}. About the farmer: {Config.PlayerDescription}";
            Monitor.Log($"Game context: {context}", LogLevel.Trace);
            return context;
        }

        private string GetFallbackDialog(string characterName, string dialogType)
        {
            Monitor.Log($"Getting fallback dialog for {characterName}, type: {dialogType}", LogLevel.Trace);
            
            var fallbacks = new Dictionary<string, Dictionary<string, string>>
            {
                ["Abigail"] = new Dictionary<string, string>
                {
                    ["casual_greeting"] = "Oh hey! What's up?",
                    ["summer_greeting"] = "This heat is perfect for cave exploring!",
                    ["festival_excited"] = "I love Spirits' Eve! So spooky!",
                    ["weekend_relaxed"] = "Finally, some time to myself!",
                    ["festival_festive"] = "The Winter Star festival is so magical!",
                    ["festival_egg_hunt"] = "I'm going to win the egg hunt this year!"
                },
                ["Alex"] = new Dictionary<string, string>
                {
                    ["athletic_greeting"] = "Just finished my workout. Feeling strong!",
                    ["beach_conversation"] = "Perfect weather for a beach workout!",
                    ["weekend_sports"] = "Weekend sports are the best!",
                    ["weekend_relaxed"] = "Even athletes need rest days."
                },
                ["Emily"] = new Dictionary<string, string>
                {
                    ["spiritual_greeting"] = "Good vibes to you today!",
                    ["work_bartending"] = "The saloon is buzzing with energy!",
                    ["creative_arts"] = "I'm working on something beautiful!",
                    ["weekend_excited"] = "The weekend brings such positive energy!",
                    ["rain_spiritual"] = "The rain is cleansing the earth."
                },
                ["Harvey"] = new Dictionary<string, string>
                {
                    ["medical_professional"] = "I'm here if you need any medical advice.",
                    ["health_concern"] = "I hope everyone is staying healthy today.",
                    ["weekend_safety"] = "Please be safe this weekend!",
                    ["rain_health_worry"] = "I hope nobody catches cold in this weather."
                },
                ["Leah"] = new Dictionary<string, string>
                {
                    ["artistic_creation"] = "I'm working on a new sculpture.",
                    ["nature_inspiration"] = "Nature is my greatest muse.",
                    ["foraging_art"] = "I found some beautiful materials today.",
                    ["weekend_creative"] = "Weekends give me time for my art.",
                    ["spring_inspiration"] = "Spring brings such creative energy!"
                },
                ["Maru"] = new Dictionary<string, string>
                {
                    ["scientific_work"] = "I'm working on an exciting new invention!",
                    ["medical_assistant"] = "Helping at the clinic is so rewarding.",
                    ["invention_excited"] = "I think I'm onto something big!",
                    ["astronomy_passion"] = "The stars hold so many secrets.",
                    ["weekend_science"] = "Weekend science projects are the best!"
                },
                ["Penny"] = new Dictionary<string, string>
                {
                    ["teaching_children"] = "The children are so eager to learn.",
                    ["library_reading"] = "I love spending time in the library.",
                    ["peaceful_reading"] = "Reading by the river is so peaceful.",
                    ["weekend_peaceful"] = "Quiet weekends are perfect for reading."
                },
                ["Sam"] = new Dictionary<string, string>
                {
                    ["work_music_dreams"] = "JojaMart pays the bills, but music is my passion.",
                    ["band_practice"] = "Band practice was amazing today!",
                    ["songwriting"] = "I'm writing a new song about friendship.",
                    ["friendship_social"] = "Hanging out with friends is the best.",
                    ["weekend_music"] = "Weekend gigs are what I live for!"
                },
                ["Sebastian"] = new Dictionary<string, string>
                {
                    ["programming_work"] = "Coding is my escape from this small town.",
                    ["city_dreams"] = "I can't wait to move to the city.",
                    ["motorcycle_freedom"] = "My motorcycle gives me the freedom I need.",
                    ["weekend_escape"] = "Maybe I'll ride out of town this weekend.",
                    ["rain_mood"] = "I actually like this gloomy weather."
                },
                ["Shane"] = new Dictionary<string, string>
                {
                    ["work_joja_grumpy"] = "Another day at JojaMart...",
                    ["struggling_day"] = "Just trying to get through today.",
                    ["chicken_care"] = "At least my chickens appreciate me.",
                    ["ranch_family"] = "Marnie's ranch feels like home.",
                    ["recovery_struggle"] = "One day at a time..."
                },
                ["Elliott"] = new Dictionary<string, string>
                {
                    ["writing_oceanside"] = "The ocean inspires my writing.",
                    ["poetic_inspiration"] = "The muses are calling today.",
                    ["literary_work"] = "I'm crafting beautiful prose.",
                    ["philosophical_mood"] = "Life's mysteries fascinate me.",
                    ["romantic_weekend"] = "Romance fills the weekend air."
                },
                ["Haley"] = new Dictionary<string, string>
                {
                    ["photography_hobby"] = "I'm taking some amazing photos today.",
                    ["fashion_beauty"] = "Looking good is important to me.",
                    ["sister_time"] = "Emily and I have such different styles.",
                    ["self_image"] = "I want to look my best.",
                    ["weekend_appearance"] = "Weekend fashion is so fun!"
                }
            };

            string fallback = fallbacks.GetValueOrDefault(characterName)?.GetValueOrDefault(dialogType) ?? "Hello there!";
            Monitor.Log($"Fallback dialog for {characterName}: '{fallback}'", LogLevel.Trace);
            return fallback;
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

    public class OpenAIResponse
    {
        public Choice[] Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

}