using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace LLMDialogMod
{
    public class AIDialogueGenerator
    {
        private readonly HttpClient client;
        private readonly Dictionary<string, string> dialogCache;
        private readonly ModConfig config;
        private readonly IMonitor monitor;

        public AIDialogueGenerator(HttpClient httpClient, Dictionary<string, string> cache, ModConfig modConfig, IMonitor modMonitor)
        {
            client = httpClient;
            dialogCache = cache;
            config = modConfig;
            monitor = modMonitor;
        }

        public string GenerateAIDialogue(string characterName, string dialogueKey, string originalDialogue, string currentDay)
        {
            // Check cache first to avoid unnecessary API calls
            string cacheKey = $"{characterName}_{dialogueKey}_{currentDay}";
            if (dialogCache.ContainsKey(cacheKey))
            {
                monitor.Log($"Using cached dialogue for {characterName} (Key: {dialogueKey})", LogLevel.Debug);
                return dialogCache[cacheKey];
            }
            
            try
            {
                monitor.Log($"Generating AI dialogue for {characterName} (Key: {dialogueKey})", LogLevel.Info);
                
                // Create character context
                var characterContext = config.CharacterContexts.GetValueOrDefault(characterName, "A villager in Stardew Valley.");
                var playerContext = config.PlayerDescription;
                
                // Create the prompt for the AI
                var prompt = $@"You are {characterName} from Stardew Valley. Generate a short, natural dialogue response (1-2 sentences max) that fits the character's personality and the current context.

Character Context: {characterContext}
Player Context: {playerContext}
Current Day: {currentDay}
Dialogue Key: {dialogueKey}
Original Dialogue: {originalDialogue}

Generate a response that:
- Stays true to {characterName}'s personality
- Is appropriate for the day and context
- Sounds natural and conversational
- Is 1-2 sentences maximum
- Doesn't include any game mechanics or meta references

Response:";

                var messages = new[]
                {
                    new Message { role = "user", content = prompt }
                };

                var requestBody = new
                {
                    model = config.OpenAIModel,
                    messages = messages,
                    max_tokens = 100,
                    temperature = 0.8
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                monitor.Log($"Sending request to OpenAI API...", LogLevel.Debug);
                var response = client.PostAsync("https://api.openai.com/v1/chat/completions", content).Result;
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseContent);
                    
                    if (apiResponse.Choices != null && apiResponse.Choices.Length > 0)
                    {
                        var aiDialogue = apiResponse.Choices[0].Message.content.Trim();
                        monitor.Log($"AI Generated: '{aiDialogue}'", LogLevel.Debug);
                        
                        // Cache the generated dialogue
                        dialogCache[cacheKey] = aiDialogue;
                        return aiDialogue;
                    }
                }
                else
                {
                    monitor.Log($"OpenAI API Error: {response.StatusCode} - {response.Content.ReadAsStringAsync().Result}", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error generating AI dialogue: {ex.Message}", LogLevel.Error);
            }
            
            // Fallback to a simple response if AI fails
            string fallbackDialogue = $"Hello! I'm {characterName} and it's {currentDay}. How are you doing?";
            dialogCache[cacheKey] = fallbackDialogue;
            return fallbackDialogue;
        }
    }
}

