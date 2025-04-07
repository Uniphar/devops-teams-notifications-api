using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Teams.Cards.BotFramework.Extensions;

internal static class ResilienceExtensions
{
	public static ResiliencePipelineBuilder<HttpResponseMessage> AddDefaultRetry(this ResiliencePipelineBuilder<HttpResponseMessage> builder)
		=> builder.AddRetry(new HttpRetryStrategyOptions());
}
