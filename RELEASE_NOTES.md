# TidyText v2.0.0 Release Notes

Welcome to **TidyText v2.0.0**! This is a monumental update that has been years in the making. I have completely rewritten the application from the ground up, transitioning it into a modern, feature-rich WPF application.

This release introduces an incredibly powerful AI Assistant, a stunning new UI with full theme support, advanced text processing features, and a robust, fully-tested architecture under the hood.

---

## 🌟 Major Highlights

### 🤖 The All-New AI Assistant
The most significant addition to TidyText is the brand-new AI Assistant, fully integrated to help you rewrite, summarize, and refine your text.
* **Extensive Provider Support:** Connect natively to Cloud APIs including **Anthropic, DeepSeek, Gemini, and OpenAI**.
* **Privacy-First Local AI:** Run completely offline using **Ollama** or **Local LM** integrations.
* **Streaming Diffs & Review Mode:** Watch the AI's suggestions stream into your editor in real-time, and seamlessly review and accept changes using the new Diff Review mode.
* **Persistent History:** A new collapsible AI History sidebar automatically saves your interactions to disk, complete with expandable details, delete, and clear history functionality.
* **Custom Prompts & Templates:** Utilize five built-in prompt templates or write your own custom prompts with dedicated model selections per AI provider.

### 🎨 Stunning Modern Interface
The UI has been entirely redesigned for a premium, modern Windows experience.
* **Custom Window Chrome:** Enjoy a sleek, custom-built title bar that integrates the application toolbar and branding.
* **Light & Dark Themes:** Full support for beautiful Light and Dark themes that automatically save your preferences.
* **Fluid Animations:** Every interaction feels alive with fade-in and slide-up view transitions, animated button states (hover, press, disabled), and slim, animated scrollbars.
* **Redesigned Windows:** Both the Settings and About windows have been completely overhauled with beautiful, card-based layouts and SVG iconography.

### 🧹 Advanced Text Processing & Statistics
* **Bulletproof History:** Introduced robust Undo/Redo functionality with full keyboard shortcuts and atomic saving.
* **Smarter Statistics:** Swapped out older formulas for the **LIX Readability Score**. Word, character, and sentence counting algorithms are now **Markdown-aware**, ignoring code blocks and bullet points to prevent skewed metrics.
* **Granular Text Cleanup:** New options include **Markdown stripping** and the ability to selectively trim whitespace at just the start or end of lines.
* **Strategy-Based Casing:** A completely refactored text casing engine flawlessly handles Sentence case, Title Case, UPPERCASE, and lowercase.

### 🏗️ Complete Architectural Rewrite
TidyText isn't just new on the outside; the entire backend has been re-architected.
* **Clean Architecture:** Split the monolithic codebase into highly cohesive `TidyText.Domain` and `TidyText.Infrastructure` layers.
* **Dependency Injection & Messaging:** Introduced full Dependency Injection (DI) and swapped the old Mediator pattern for a highly decoupled, lightweight Messenger system.
* **Rock-Solid Stability:** The release includes a massive new suite of WPF unit tests and edge-case testing, ensuring algorithms like the `MarkdownProcessor` and `TextStatistics` perform flawlessly.

---
*Thank you for using TidyText! I can't wait for you to experience everything I've built in version 2.0.0.*
