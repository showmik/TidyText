import os
import re

keys = [
    "BgColor", "PanelBgColor", "BorderColor", "TextColor", "TextMutedColor",
    "AccentColor", "AccentHoverColor", "AccentForegroundColor", "HoverColor", "PressedColor",
    "BgBrush", "PanelBgBrush", "BorderBrush", "TextBrush", "TextMutedBrush",
    "AccentBrush", "AccentHoverBrush", "AccentForegroundBrush", "HoverBrush", "PressedBrush",
    "SoftGlow", "AccentGlow"
]

files = [
    r"H:\dev\TidyText\src\TidyText.App\Styles.xaml",
    r"H:\dev\TidyText\src\TidyText.App\Views\MainWindow.xaml",
    r"H:\dev\TidyText\src\TidyText.App\Views\AIAssistantPanel.xaml"
]

for file_path in files:
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
    
    for key in keys:
        content = content.replace(f"{{StaticResource {key}}}", f"{{DynamicResource {key}}}")
    
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(content)

print("Replacement complete.")
