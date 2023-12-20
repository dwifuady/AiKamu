using Refit;

namespace AiKamu.Commands.OpenAi;

public interface IOpenAiApi
{
    [Post("/v1/chat/completions")]
    [Headers("Authorization: Bearer")]
    Task<OpenAiResponse> ChatCompletion(OpenAiRequest request);

    [Post("/v1/images/generations")]
    [Headers("Authorization: Bearer")]
    Task<ImageGenerationResponse> GenerateImages(ImageGenerationRequest request);
}