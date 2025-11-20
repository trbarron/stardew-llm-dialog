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
    }
}
