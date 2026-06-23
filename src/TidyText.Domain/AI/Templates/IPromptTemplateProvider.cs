using System.Collections.Generic;

namespace TidyText.Domain.AI.Templates
{
    public interface IPromptTemplateProvider
    {
        IReadOnlyList<IPromptTemplate> GetTemplates();
    }
}
