// TidyText/Model/Casing/DefaultSentenceCaseLexicon.cs
using System;
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>Default, immutable lexicon. Easy to extend via PRs or by swapping implementations.</summary>
    public sealed class DefaultSentenceCaseLexicon : ISentenceCaseLexicon
    {
        public static ISentenceCaseLexicon Instance { get; } = new DefaultSentenceCaseLexicon();

        private DefaultSentenceCaseLexicon() { }

        public ISet<string> NonTerminalAbbreviations { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mr","mrs","ms","dr","prof","sr","jr","st",
            "vs","v","etc","e.g","eg","i.e","ie",
            "a.m","am","p.m","pm",
            "u.s","u.s.a","us","usa","u.k","uk","u.n","un",
            "ai", "a.i."
        };

        public ISet<string> UpperShortStopwords { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            // articles, conjunctions, prepositions (existing)
            "A","AN","THE","AND","OR","NOR","BUT","SO",
            "TO","IN","ON","AT","OF","BY","AS",
            "IS","AM","ARE","WAS","WERE","BE","BEEN",
            "DO","DID","DONE",
            "FOR","FROM","WITH","WITHOUT","OVER","UNDER",
            "OUT","OFF","UP","DOWN",
            "NEW","ALL","ANY","NOT","ONE","TWO",

            // pronouns & determiners
            "I","ME","MY","YOU","YOUR","WE","US","OUR",
            "HE","HIM","HIS","SHE","HER","IT","ITS",
            "THEY","THEM","THEIR","THIS","THAT","THESE","THOSE",

            // comparatives / conditionals / misc short words
            "IF","THAN","THEN","PER","ET","AL",

            // common modals/auxiliaries (≤3)
            "CAN","MAY","HAS","HAD"
        };

        public ISet<string> UpperAcronyms { get; } = new HashSet<string>(StringComparer.Ordinal)
{
            // 2–3 letters
            "AI","ML","API","SDK","CLI","UI","UX","ID","IP","DNS","TCP","UDP","SSL","TLS","SSH",
            "CPU","GPU","RAM","ROM","SSD","HDD","USB","WPF","GPT","US","USA","UK","EU","UN","UAE","SLS",
            "AM","PM",
            "AP","MLA","BB","AMA","NY","APA",
            // trusted 4+ letters
            "NASA","HTTP","HTTPS","HTML","JSON","XML","SQL","UUID","GUID","JPEG","PNG","WASM","WLAN","SSID"
        };


        public IDictionary<string, string> ProperCaseMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // brands / products (key: any case; value: canonical case)
            ["iphone"] = "iPhone",
            ["ipad"] = "iPad",
            ["macos"] = "macOS",
            ["ebay"] = "eBay",
            ["mcdonald’s"] = "McDonald’s",
            ["macdonald"] = "MacDonald",
            ["macdonald's"] = "MacDonald's",
            ["macdonald’s"] = "MacDonald’s",
            ["pixel"] = "Pixel",
            ["galaxy"] = "Galaxy",
            ["thinkpad"] = "ThinkPad",
            ["macbook"] = "MacBook",
            ["windows"] = "Windows",
            ["android"] = "Android",
            ["ios"] = "iOS",
            ["google"] = "Google",
            ["facebook"] = "Facebook",
            ["twitter"] = "Twitter",
            ["linkedin"] = "LinkedIn",
            ["youtube"] = "YouTube",
            ["whatsapp"] = "WhatsApp",
            ["messenger"] = "Messenger",
            ["instagram"] = "Instagram",
            ["tiktok"] = "TikTok",
            ["snapchat"] = "Snapchat",
            ["spotify"] = "Spotify",
            ["netflix"] = "Netflix",
            ["amazon"] = "Amazon",
            ["alexa"] = "Alexa",
            ["siri"] = "Siri",
            ["cortana"] = "Cortana",
            ["bing"] = "Bing",
            ["github"] = "GitHub",
            ["gitlab"] = "GitLab",
            ["stackoverflow"] = "Stack Overflow",
            ["reddit"] = "Reddit",
            ["zoom"] = "Zoom",
            ["slack"] = "Slack",
            ["teams"] = "Teams",
            ["skype"] = "Skype",
            ["dropbox"] = "Dropbox",
            ["onedrive"] = "OneDrive",
            ["icloud"] = "iCloud",
            ["adobe"] = "Adobe",
            ["photoshop"] = "Photoshop",
            ["illustrator"] = "Illustrator",
            ["premiere"] = "Premiere",
            ["aftereffects"] = "After Effects",
            ["figma"] = "Figma",
            ["notion"] = "Notion",
            ["trello"] = "Trello",
            ["asana"] = "Asana",
            ["jira"] = "Jira",
            ["confluence"] = "Confluence",
            ["zoom"] = "Zoom",
            ["uber"] = "Uber",
            ["lyft"] = "Lyft",
            ["tesla"] = "Tesla",
            ["bmw"] = "BMW",
            ["mercedes"] = "Mercedes",
            ["audi"] = "Audi",
            ["volkswagen"] = "Volkswagen",
            ["toyota"] = "Toyota",
            ["honda"] = "Honda",
            ["ford"] = "Ford",
            ["chevrolet"] = "Chevrolet",
            ["nissan"] = "Nissan",
            ["hyundai"] = "Hyundai",
            ["kia"] = "Kia",
            ["sony"] = "Sony",
            ["samsung"] = "Samsung",
            ["lg"] = "LG",
            ["panasonic"] = "Panasonic",
            ["philips"] = "Philips",
            ["dell"] = "Dell",
            ["hp"] = "HP",
            ["lenovo"] = "Lenovo",
            ["acer"] = "Acer",
            ["asus"] = "ASUS",
            ["msi"] = "MSI",
            ["alienware"] = "Alienware",
            ["surface"] = "Surface",
            ["playstation"] = "PlayStation",
            ["xbox"] = "Xbox",
            ["nintendo"] = "Nintendo",
            ["switch"] = "Switch",
            ["wii"] = "Wii",
            ["ds"] = "DS",
            ["claude"] = "Claude",
            ["sonnet"] = "Sonnet",
            ["gemini"] = "Gemini",
            ["mac"] = "Mac",
            ["cmd"] = "Cmd",
            ["ctrl"] = "Ctrl",
        };


        public ISet<string> ProperCaseTokens { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // seed with what you need; contributors can expand
            "Claude","Sonnet","Gemini"
        };

        public ISet<string> BrandTokens { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Claude","Gemini","iPhone","iPad","Pixel","Galaxy","MacBook","ThinkPad"
        };

        public ISet<string> BrandSuffixes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Pro","Max","Ultra","Plus","Mini"
        };

        public ISet<string> HonorificBases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mr","mrs","ms","dr","prof","sr","jr","st"
        };
    }
}
