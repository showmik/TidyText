using System.Collections.Generic;

namespace TidyText.Core.AI.Templates
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
                new TranslateTemplate()
                // More templates can be added here
            };
        }
    }

    public class GrammarCheckTemplate : IPromptTemplate
    {
        public string Name => "Check Grammar";
        public string Description => "Fix grammar, spelling, and punctuation errors.";
        public string Icon => "🔍";
        public string SystemPrompt => "You are an expert copy editor. Fix any grammatical, spelling, or punctuation errors in the provided text. Return ONLY the corrected text without any conversational filler or explanations.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Please correct the following text:\n\n{text}";
        }
    }

    public class RewriteTemplate : IPromptTemplate
    {
        public string Name => "Rewrite";
        public string Description => "Rewrite text in a specific tone.";
        public string Icon => "✍️";
        public string SystemPrompt => "You are an expert writer. Rewrite the provided text according to the user's instructions. Return ONLY the rewritten text without conversational filler.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            string tone = variables != null && variables.TryGetValue("tone", out var t) ? t : "professional and clear";
            return $"Rewrite the following text to be {tone}:\n\n{text}";
        }
    }

    public class WriteProposalTemplate : IPromptTemplate
    {
        public string Name => "Write a Proposal";
        public string Description => "Generate a professional proposal from notes.";
        public string Icon => "📝";
        public string SystemPrompt => "You are a professional business consultant. Convert the provided notes or outline into a formal, well-structured business proposal. Use markdown formatting where appropriate. Do not include conversational filler.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Create a professional proposal based on these notes:\n\n{text}";
        }
    }

    public class WriteEmailTemplate : IPromptTemplate
    {
        public string Name => "Write Email";
        public string Description => "Draft an email from key points.";
        public string Icon => "📧";
        public string SystemPrompt => "You are an expert communicator. Draft an email based on the provided points. Ensure it has a clear subject line and appropriate greeting/sign-off. Return ONLY the email draft.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            string tone = variables != null && variables.TryGetValue("tone", out var t) ? t : "professional";
            return $"Draft a {tone} email based on these points:\n\n{text}";
        }
    }

    public class SummarizeTemplate : IPromptTemplate
    {
        public string Name => "Summarize";
        public string Description => "Condense long text into key points.";
        public string Icon => "📋";
        public string SystemPrompt => "You are a precise summarizer. Summarize the provided text into key points. Use bullet points and keep it concise. Return ONLY the summary.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Summarize the following text:\n\n{text}";
        }
    }

    public class MakeConciseTemplate : IPromptTemplate
    {
        public string Name => "Make Concise";
        public string Description => "Shorten verbose text while preserving meaning.";
        public string Icon => "✂️";
        public string SystemPrompt => "You are an expert editor. Make the provided text as concise as possible while preserving its core meaning and tone. Return ONLY the concise text.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Make the following text more concise:\n\n{text}";
        }
    }

    public class TranslateTemplate : IPromptTemplate
    {
        public string Name => "Translate to English";
        public string Description => "Translate the text into fluent English.";
        public string Icon => "🌍";
        public string SystemPrompt => "You are an expert translator. Translate the provided text into fluent, natural-sounding English. Return ONLY the translated text without conversational filler.";

        public string GetPrompt(string text, IDictionary<string, string>? variables = null)
        {
            return $"Translate the following text to English:\n\n{text}";
        }
    }
}
