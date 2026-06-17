using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TidyText.Core.AI;
using TidyText.Core.AI.Templates;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class AITests
    {
        private class MockProvider : IAIProvider
        {
            public string Name => "MockProvider";
            public bool IsAvailable { get; set; } = true;
            public bool WillThrow { get; set; } = false;

            public Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default)
            {
                if (WillThrow)
                {
                    throw new InvalidOperationException("Simulated error.");
                }

                return Task.FromResult(AIResponse.Success($"Mocked response for: {prompt}"));
            }
        }

        [Test]
        public async Task Router_Returns_Error_If_Provider_Not_Found()
        {
            var router = new AIProviderRouter(new List<IAIProvider>());
            var result = await router.RouteAsync("Unknown", "Hello", new AIOptions());
            
            Assert.That(result.IsError, Is.True);
            Assert.That(result.ErrorMessage, Does.Contain("not found"));
        }

        [Test]
        public async Task Router_Returns_Error_If_Provider_Not_Available()
        {
            var provider = new MockProvider { IsAvailable = false };
            var router = new AIProviderRouter(new[] { provider });
            var result = await router.RouteAsync("MockProvider", "Hello", new AIOptions());
            
            Assert.That(result.IsError, Is.True);
            Assert.That(result.ErrorMessage, Does.Contain("is not available"));
        }

        [Test]
        public async Task Router_Returns_Error_If_Provider_Throws()
        {
            var provider = new MockProvider { WillThrow = true };
            var router = new AIProviderRouter(new[] { provider });
            var result = await router.RouteAsync("MockProvider", "Hello", new AIOptions());
            
            Assert.That(result.IsError, Is.True);
            Assert.That(result.ErrorMessage, Does.Contain("Simulated error"));
        }

        [Test]
        public async Task Router_Returns_Success_When_Valid()
        {
            var provider = new MockProvider();
            var router = new AIProviderRouter(new[] { provider });
            var result = await router.RouteAsync("MockProvider", "Hello", new AIOptions());
            
            Assert.That(result.IsError, Is.False);
            Assert.That(result.Text, Is.EqualTo("Mocked response for: Hello"));
        }

        [Test]
        public void PromptTemplateEngine_Returns_BuiltIn_Templates()
        {
            var engine = new PromptTemplateEngine();
            var templates = engine.GetBuiltInTemplates();
            
            Assert.That(templates.Count, Is.GreaterThan(0));
            Assert.That(templates.Any(t => t.Name == "Check Grammar"), Is.True);
            Assert.That(templates.Any(t => t.Name == "Rewrite"), Is.True);
        }

        [Test]
        public void RewriteTemplate_Uses_Tone_Variable_If_Provided()
        {
            var engine = new PromptTemplateEngine();
            var rewrite = engine.GetBuiltInTemplates().First(t => t.Name == "Rewrite");
            
            var promptWithoutTone = rewrite.GetPrompt("some text", null);
            Assert.That(promptWithoutTone, Does.Contain("professional and clear"));
            
            var promptWithTone = rewrite.GetPrompt("some text", new Dictionary<string, string> { { "tone", "casual" } });
            Assert.That(promptWithTone, Does.Contain("casual"));
            Assert.That(promptWithTone, Does.Not.Contain("professional and clear"));
        }
    }
}
