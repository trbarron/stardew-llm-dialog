using System.Collections.Generic;

namespace LLMDialogMod
{
    public static class CharacterConfig
    {
        public static readonly HashSet<string> TargetCharacters = new HashSet<string>
        {
            // Marriage candidates
            "Abigail", "Alex", "Emily", "Harvey", "Leah", "Maru", 
            "Penny", "Sam", "Sebastian", "Shane", "Elliott", "Haley",
            // Non-marriage candidates (most important ones)
            "Robin", "Pierre", "Gus", "Lewis", "Marnie", "Willy", "Wizard",
            "Caroline", "Clint", "Demetrius", "Evelyn", "George", "Jodi", 
            "Kent", "Linus", "Pam", "Sandy", "Jas", "Vincent", "Dwarf", "Krobus"
        };


        public static string GetFallbackDialogue(string characterName, string dialogueKey, string currentDay)
        {
            // Check if it's a gift-related dialogue
            if (dialogueKey.Contains("Gift") || dialogueKey.Contains("AcceptGift"))
            {
                return characterName switch
                {
                    "Abigail" => "Oh, thank you! This is really thoughtful of you.",
                    "Alex" => "Thanks! This is really cool of you.",
                    "Emily" => "How wonderful! This brings such positive energy!",
                    "Harvey" => "Thank you, I appreciate your thoughtfulness.",
                    "Leah" => "How kind of you! This will be perfect for my art.",
                    "Maru" => "Thank you! This is so thoughtful of you.",
                    "Penny" => "Oh my, thank you so much! This is very kind.",
                    "Sam" => "Thanks! This is really awesome of you.",
                    "Sebastian" => "Thanks... I appreciate it.",
                    "Shane" => "Thanks... I appreciate it.",
                    "Elliott" => "How generous of you! Thank you so much.",
                    "Haley" => "Oh, thank you! This is so sweet of you.",
                    // Non-marriage candidates
                    "Robin" => "Thank you! This is very thoughtful of you.",
                    "Pierre" => "Thank you, I appreciate your business!",
                    "Gus" => "Thank you! This is very kind of you.",
                    "Lewis" => "Thank you, I appreciate your thoughtfulness.",
                    "Marnie" => "How sweet of you! Thank you so much.",
                    "Willy" => "Thanks, I appreciate it.",
                    "Wizard" => "Your gift is... interesting. Thank you.",
                    "Caroline" => "How lovely! Thank you so much.",
                    "Clint" => "Thanks... I appreciate it.",
                    "Demetrius" => "Thank you, this is quite thoughtful.",
                    "Evelyn" => "Oh my, thank you so much!",
                    "George" => "Hmph... thanks, I guess.",
                    "Jodi" => "Thank you, this is very kind.",
                    "Kent" => "Thanks, I appreciate it.",
                    "Linus" => "Thank you, friend.",
                    "Pam" => "Thanks... I appreciate it.",
                    "Sandy" => "Thank you! This is so nice of you.",
                    "Jas" => "Thank you! This is so pretty!",
                    "Vincent" => "Thanks! This is so cool!",
                    "Dwarf" => "Gift... good. Thank you.",
                    "Krobus" => "Thank you... friend.",
                    _ => "Thank you so much!"
                };
            }
            
            // Check if it's an event-related dialogue
            if (dialogueKey.Contains("eventSeen") || dialogueKey.Contains("Event"))
            {
                return characterName switch
                {
                    "Abigail" => "That was quite an experience, wasn't it?",
                    "Alex" => "That was pretty intense!",
                    "Emily" => "What a beautiful moment that was!",
                    "Harvey" => "I hope everyone stayed safe during that.",
                    "Leah" => "What a memorable moment that was.",
                    "Maru" => "That was quite fascinating from a scientific perspective.",
                    "Penny" => "That was such a lovely experience.",
                    "Sam" => "That was pretty cool!",
                    "Sebastian" => "Well, that was something...",
                    "Shane" => "Well, that happened...",
                    "Elliott" => "What a poetic moment that was.",
                    "Haley" => "That was actually pretty nice.",
                    // Non-marriage candidates
                    "Robin" => "That was quite an experience!",
                    "Pierre" => "That was quite something, wasn't it?",
                    "Gus" => "That was quite an event!",
                    "Lewis" => "That was quite a memorable occasion.",
                    "Marnie" => "That was quite something!",
                    "Willy" => "That was quite an experience.",
                    "Wizard" => "The mystical energies were quite active during that event.",
                    "Caroline" => "That was quite lovely!",
                    "Clint" => "That was... something.",
                    "Demetrius" => "That was quite fascinating from a scientific perspective.",
                    "Evelyn" => "That was such a lovely experience!",
                    "George" => "Hmph... that was something.",
                    "Jodi" => "That was quite an experience.",
                    "Kent" => "That was... intense.",
                    "Linus" => "That was quite something, friend.",
                    "Pam" => "That was... something.",
                    "Sandy" => "That was quite an experience!",
                    "Jas" => "That was so much fun!",
                    "Vincent" => "That was so cool!",
                    "Dwarf" => "Event... interesting.",
                    "Krobus" => "That was... different.",
                    _ => "That was quite something!"
                };
            }
            
            // Check if it's a resort-related dialogue
            if (dialogueKey.Contains("Resort"))
            {
                return characterName switch
                {
                    "Abigail" => "This place is amazing! So much to explore!",
                    "Alex" => "This place is perfect for a workout!",
                    "Emily" => "The energy here is so positive and uplifting!",
                    "Harvey" => "I hope everyone is being safe here.",
                    "Leah" => "The natural beauty here is so inspiring.",
                    "Maru" => "This place has such interesting architecture!",
                    "Penny" => "This is such a peaceful place.",
                    "Sam" => "This place has great vibes!",
                    "Sebastian" => "Nice place, I guess...",
                    "Shane" => "Nice place, I guess...",
                    "Elliott" => "The ocean views here are absolutely poetic.",
                    "Haley" => "This place is so photogenic!",
                    // Non-marriage candidates
                    "Robin" => "This place has such interesting architecture!",
                    "Pierre" => "This place has great business potential!",
                    "Gus" => "This place has such a great atmosphere!",
                    "Lewis" => "This is quite a nice place for the town.",
                    "Marnie" => "This place is so peaceful!",
                    "Willy" => "The ocean air here is refreshing.",
                    "Wizard" => "The magical energies here are quite strong.",
                    "Caroline" => "This place is so lovely!",
                    "Clint" => "This place is... nice.",
                    "Demetrius" => "This place has interesting geological features.",
                    "Evelyn" => "This place is so beautiful!",
                    "George" => "Hmph... it's okay.",
                    "Jodi" => "This place is quite nice.",
                    "Kent" => "This place is... peaceful.",
                    "Linus" => "This place has good energy, friend.",
                    "Pam" => "This place is... nice.",
                    "Sandy" => "This place reminds me of the desert!",
                    "Jas" => "This place is so pretty!",
                    "Vincent" => "This place is so cool!",
                    "Dwarf" => "Place... good.",
                    "Krobus" => "This place is... different.",
                    _ => "What a lovely place!"
                };
            }
            
            // Default fallback based on character
            return characterName switch
            {
                "Abigail" => $"Hey there! It's {currentDay} - perfect day for an adventure!",
                "Alex" => $"Hey! It's {currentDay} - great day for a workout!",
                "Emily" => $"Good {currentDay}! The energy today is so positive!",
                "Harvey" => $"Good {currentDay}! I hope everyone is staying healthy.",
                "Leah" => $"Good {currentDay}! I'm working on some new art inspired by nature.",
                "Maru" => $"Good {currentDay}! I'm working on an exciting new invention!",
                "Penny" => $"Good {currentDay}! I'm spending time with the children today.",
                "Sam" => $"Hey! It's {currentDay} - perfect day for some music!",
                "Sebastian" => $"It's {currentDay}... another day in this small town.",
                "Shane" => $"*sigh* Another {currentDay}... at least my chickens are happy.",
                "Elliott" => $"Good {currentDay}! The muses are calling today.",
                "Haley" => $"Hey! It's {currentDay} - perfect day for some photos!",
                // Non-marriage candidates
                "Robin" => $"Good {currentDay}! I'm working on some new building projects!",
                "Pierre" => $"Good {currentDay}! Business is looking good today!",
                "Gus" => $"Good {currentDay}! The saloon is ready for customers!",
                "Lewis" => $"Good {currentDay}! I'm taking care of town business.",
                "Marnie" => $"Good {currentDay}! I'm taking care of the animals.",
                "Willy" => $"Good {currentDay}! The fish are biting well today!",
                "Wizard" => $"Good {currentDay}! The mystical energies are strong today.",
                "Caroline" => $"Good {currentDay}! I'm tending to my garden today.",
                "Clint" => $"It's {currentDay}... another day at the forge.",
                "Demetrius" => $"Good {currentDay}! I'm conducting some research.",
                "Evelyn" => $"Good {currentDay}! I'm tending to my flowers.",
                "George" => $"Hmph... it's {currentDay}.",
                "Jodi" => $"Good {currentDay}! I'm taking care of the house.",
                "Kent" => $"It's {currentDay}... another day.",
                "Linus" => $"Good {currentDay}, friend! The mountain air is fresh today.",
                "Pam" => $"It's {currentDay}... another day driving the bus.",
                "Sandy" => $"Good {currentDay}! The desert is beautiful today!",
                "Jas" => $"Hi! It's {currentDay} - I'm playing with my flowers!",
                "Vincent" => $"Hey! It's {currentDay} - I'm playing with my toys!",
                "Dwarf" => $"Day... {currentDay}.",
                "Krobus" => $"Day... {currentDay}... different.",
                _ => $"Hello! It's {currentDay} and I'm doing well."
            };
        }
    }
}
