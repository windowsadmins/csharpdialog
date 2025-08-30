using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CSharpDialog.Core.Models
{
    public class ThemeConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("windowStyle")]
        public Dictionary<string, object>? WindowStyle { get; set; }

        [JsonPropertyName("buttonStyle")]
        public Dictionary<string, object>? ButtonStyle { get; set; }

        [JsonPropertyName("progressStyle")]
        public Dictionary<string, object>? ProgressStyle { get; set; }

        [JsonPropertyName("textStyle")]
        public Dictionary<string, object>? TextStyle { get; set; }

        [JsonPropertyName("listStyle")]
        public Dictionary<string, object>? ListStyle { get; set; }

        [JsonPropertyName("iconStyle")]
        public Dictionary<string, object>? IconStyle { get; set; }

        [JsonPropertyName("animations")]
        public AnimationConfiguration? Animations { get; set; }

        [JsonPropertyName("brandConfiguration")]
        public BrandConfiguration? BrandConfiguration { get; set; }

        [JsonPropertyName("customProperties")]
        public Dictionary<string, object>? CustomProperties { get; set; }
    }

    public class AnimationConfiguration
    {
        [JsonPropertyName("enableAnimations")]
        public bool EnableAnimations { get; set; } = true;

        [JsonPropertyName("fadeInDuration")]
        public int FadeInDuration { get; set; } = 300;

        [JsonPropertyName("fadeOutDuration")]
        public int FadeOutDuration { get; set; } = 200;

        [JsonPropertyName("progressAnimation")]
        public bool ProgressAnimation { get; set; } = true;

        [JsonPropertyName("buttonHoverAnimation")]
        public bool ButtonHoverAnimation { get; set; } = true;

        [JsonPropertyName("listItemAnimation")]
        public bool ListItemAnimation { get; set; } = true;

        [JsonPropertyName("customAnimations")]
        public Dictionary<string, object>? CustomAnimations { get; set; }
    }

    public class BrandConfiguration
    {
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("logoPath")]
        public string LogoPath { get; set; } = string.Empty;

        [JsonPropertyName("primaryColor")]
        public string PrimaryColor { get; set; } = string.Empty;

        [JsonPropertyName("secondaryColor")]
        public string SecondaryColor { get; set; } = string.Empty;

        [JsonPropertyName("accentColor")]
        public string AccentColor { get; set; } = string.Empty;

        [JsonPropertyName("fontFamily")]
        public string FontFamily { get; set; } = string.Empty;

        [JsonPropertyName("customCss")]
        public string CustomCss { get; set; } = string.Empty;

        [JsonPropertyName("watermarkEnabled")]
        public bool WatermarkEnabled { get; set; } = false;

        [JsonPropertyName("watermarkText")]
        public string WatermarkText { get; set; } = string.Empty;

        [JsonPropertyName("brandProperties")]
        public Dictionary<string, object>? BrandProperties { get; set; }
    }

    public class StyleProperty
    {
        public string Element { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public bool Important { get; set; } = false;
    }

    public class StyleSheet
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public List<StyleProperty> Properties { get; set; } = new();

        [JsonPropertyName("cssRules")]
        public List<string> CssRules { get; set; } = new();

        [JsonPropertyName("mediaQueries")]
        public Dictionary<string, List<StyleProperty>>? MediaQueries { get; set; }
    }
}
