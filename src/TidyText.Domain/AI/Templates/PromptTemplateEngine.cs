using System.Collections.Generic;

namespace TidyText.Domain.AI.Templates
{
    public interface IPromptTemplate
    {
        string Name { get; }
        string Description { get; }
        string Icon { get; }
        string SystemPrompt { get; }
        string GetPrompt(string text, IDictionary<string, string>? variables = null);
    }

    public class PromptTemplateEngine
    {
        public IReadOnlyList<IPromptTemplate> GetBuiltInTemplates()
        {
            return new List<IPromptTemplate>
            {
                new GrammarCheckTemplate(),
                new RewriteTemplate(),
                new WriteProposalTemplate(),
                new WriteEmailTemplate(),
                new SummarizeTemplate(),
                new MakeConciseTemplate(),
                new TranslateTemplate(),
                new ExtractActionItemsTemplate(),
                new ExpandTextTemplate(),
                new SimplifyTemplate(),
                new CleanFormattingTemplate(),
                new GenerateTitlesTemplate()
                // More templates can be added here
            };
        }
    }

    public class GrammarCheckTemplate : IPromptTemplate
    {
        public string Name => "Check Grammar";
        public string Description => "Fix grammar, spelling, and punctuation errors.";
        public string Icon => "🔍";
        public string SystemPrompt => "You are a world-class copy editor. Your task is to correct grammatical, spelling, and punctuation errors in the provided text while strictly preserving the author's original voice, vocabulary, and intent. Do not heavily rewrite the text unless it is structurally incoherent. Return ONLY the corrected text. Never include conversational filler, greetings, or explanations.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Please correct the following text:\n\n<text>\n{text}\n</text>";
        }
    }

    public class RewriteTemplate : IPromptTemplate
    {
        public string Name => "Rewrite";
        public string Description => "Rewrite text in a specific tone.";
        public string Icon => "✍️";
        public string SystemPrompt => "You are an expert copywriter. Your task is to rewrite the provided text to match a specific tone or instruction. Maintain all factual information and core meaning. Do not hallucinate or add new facts. Return ONLY the rewritten text without conversational filler or explanations.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            string tone = variables != null && variables.TryGetValue("tone", out var t) ? t : "professional and clear";
            return $"Rewrite the following text to be {tone}:\n\n<text>\n{text}\n</text>";
        }
    }

    public class WriteProposalTemplate : IPromptTemplate
    {
        public string Name => "Write a Proposal";
        public string Description => "Generate a professional proposal from notes.";
        public string Icon => "📝";
        public string SystemPrompt => "You are a senior business consultant and proposal writer. Your task is to convert raw notes or outlines into a highly persuasive, formally structured business proposal. Use clear markdown formatting, including an Executive Summary, Objectives, Proposed Solution, and Next Steps. Maintain a confident, professional tone. If information is missing, use bracketed placeholders like [Insert Date]. Return ONLY the proposal.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Create a professional proposal based on these notes:\n\n<notes>\n{text}\n</notes>";
        }
    }

    public class WriteEmailTemplate : IPromptTemplate
    {
        public string Name => "Write Email";
        public string Description => "Draft an email from key points.";
        public string Icon => "📧";
        public string SystemPrompt => "You are an expert executive communicator. Draft a polished email based on the provided key points. The email should be concise, clear, and actionable. Avoid outdated clichés like 'I hope this email finds you well'. Include a strong Subject Line. Use bracketed placeholders like [Name] for missing details. Return ONLY the email draft.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            string tone = variables != null && variables.TryGetValue("tone", out var t) ? t : "professional";
            return $"Draft a {tone} email based on these points:\n\n<points>\n{text}\n</points>";
        }
    }

    public class SummarizeTemplate : IPromptTemplate
    {
        public string Name => "Summarize";
        public string Description => "Condense long text into key points.";
        public string Icon => "📋";
        public string SystemPrompt => "You are an expert analyst. Your task is to distill long text into its most critical information. Provide a 1-2 sentence high-level summary, followed by 3-5 concise bullet points highlighting the key arguments, data, or takeaways. Do not include your own opinions or conversational filler. Return ONLY the summary.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Summarize the following text:\n\n<text>\n{text}\n</text>";
        }
    }

    public class MakeConciseTemplate : IPromptTemplate
    {
        public string Name => "Make Concise";
        public string Description => "Shorten verbose text while preserving meaning.";
        public string Icon => "✂️";
        public string SystemPrompt => "You are a ruthless but careful editor. Your task is to make the provided text as concise and impactful as possible. Eliminate redundancies, wordiness, passive voice, and weak verbs. You must preserve the core meaning, tone, and critical details of the original text. Return ONLY the shortened text without conversational filler or explanations.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Make the following text more concise:\n\n<text>\n{text}\n</text>";
        }
    }

    public class TranslateTemplate : IPromptTemplate
    {
        public string Name => "Translate to English";
        public string Description => "Translate the text into fluent English.";
        public string Icon => "🌍";
        public string SystemPrompt => "You are a native, fluent English speaker and professional translator. Translate the provided text into natural-sounding English. Translate idioms conceptually rather than literally to ensure they make sense to an English reader. Match the tone and formality of the original text. Return ONLY the English translation without conversational filler or explanations.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Translate the following text to English:\n\n<text>\n{text}\n</text>";
        }
    }

    public class ExtractActionItemsTemplate : IPromptTemplate
    {
        public string Name => "Extract Action Items";
        public string Description => "Generate a checklist of to-dos from notes or emails.";
        public string Icon => "✅";
        public string SystemPrompt => "You are a highly organized project manager. Your task is to read the provided notes or email and extract all implicit and explicit action items. Return ONLY a formatted markdown checklist of these action items. Do not include conversational filler.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Extract action items from the following text:\n\n<text>\n{text}\n</text>";
        }
    }

    public class ExpandTextTemplate : IPromptTemplate
    {
        public string Name => "Expand Text";
        public string Description => "Add detail and length to short text or bullet points.";
        public string Icon => "➕";
        public string SystemPrompt => "You are an expert ghostwriter. Your task is to take the provided short text or bullet points and expand them into a fully fleshed-out, detailed, and professional text. Maintain the original core intent but elaborate on the concepts. Return ONLY the expanded text.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Expand the following text with more detail:\n\n<text>\n{text}\n</text>";
        }
    }

    public class SimplifyTemplate : IPromptTemplate
    {
        public string Name => "Simplify (ELI5)";
        public string Description => "Rewrite complex text so anyone can understand it.";
        public string Icon => "🧠";
        public string SystemPrompt => "You are an expert teacher. Your task is to take complex, technical, or academic text and rewrite it so that a layman or a smart 5th grader could easily understand it. Use simple analogies if necessary. Return ONLY the simplified text without conversational filler.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Simplify the following text to be easily understood:\n\n<text>\n{text}\n</text>";
        }
    }

    public class CleanFormattingTemplate : IPromptTemplate
    {
        public string Name => "Clean Formatting";
        public string Description => "Fix messy line breaks and spacing.";
        public string Icon => "🧹";
        public string SystemPrompt => "You are an expert formatting assistant. Your task is to take the provided messy text (e.g., from PDF copy-paste or bad OCR) and fix all the random line breaks, weird spacing, and broken paragraphs. DO NOT change the actual words, just fix the formatting. Return ONLY the clean text.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Clean the formatting of the following text:\n\n<text>\n{text}\n</text>";
        }
    }

    public class GenerateTitlesTemplate : IPromptTemplate
    {
        public string Name => "Generate Titles";
        public string Description => "Create 5 catchy titles or headlines for the text.";
        public string Icon => "💡";
        public string SystemPrompt => "You are an expert copywriter. Your task is to read the provided text and generate exactly 5 strong, catchy, and professional titles or headlines for it. Return ONLY a numbered list of the titles without conversational filler.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Generate 5 titles for the following text:\n\n<text>\n{text}\n</text>";
        }
    }
}
