using Azure.Core;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Teams.Cards.BotFramework.Http;

internal static class HttpPipeline
{
	public static HttpMessageHandler AddTokenCredential(this HttpMessageHandler handler, TokenCredential tokenCredential, params string[] scopes)
	{
		return new TokenCredentialHandler(tokenCredential, scopes)
		{
			InnerHandler = handler
		};
	}

	public static HttpMessageHandler AddResiliency(this HttpMessageHandler handler, Func<ResiliencePipelineBuilder<HttpResponseMessage>, ResiliencePipelineBuilder<HttpResponseMessage>> pipelineBuilder)
	{
		var pipeline = pipelineBuilder(new ResiliencePipelineBuilder<HttpResponseMessage>());
		return new ResilienceHandler(pipeline.Build());
	}
}
