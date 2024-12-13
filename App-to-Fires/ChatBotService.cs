using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

public class ChatbotService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;

    public ChatbotService(IConfiguration configuration)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var endpoint = config["AzureOAIEndpoint"];
        var key = config["AzureOAIKey"];
        _deploymentName = config["AzureOAIDeploymentName"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(_deploymentName))
        {
            throw new ArgumentException("Por favor verifica tu archivo appsettings.json para valores faltantes o incorrectos.");
        }

        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    public async Task<string> GetChatbotResponseAsync(string userMessage, string eventContext)
    {
        var systemMessage = $"Eres el asistente Jarvis para un evento específico, debes ser directo. Contexto del evento: {eventContext}";
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage)
            },
            Temperature = 0.5f,
            MaxTokens = 800
        };

        try
        {
            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(
                chatCompletionsOptions,
                CancellationToken.None);

            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            return $"Lo siento, hubo un error: {ex.Message}";
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly ChatbotService _chatbotService;
    private readonly SpeechService _speechService;

    public ChatbotController(ChatbotService chatbotService, SpeechService speechService)
    {
        _chatbotService = chatbotService;
        _speechService = speechService;
    }

    [HttpPost("text")]
    public async Task<IActionResult> GetTextResponse([FromBody] ChatRequest request)
    {
        var response = await _chatbotService.GetChatbotResponseAsync(
            request.UserMessage,
            request.EventContext);

        return Ok(new { response });
    }

    [HttpPost("speech")]
    public async Task<IActionResult> GetSpeechResponse()
    {
        try
        {
            string userMessage = await _speechService.RecognizeSpeechAsync();

            if (string.IsNullOrEmpty(userMessage))
            {
                return BadRequest("No se pudo reconocer ningún mensaje de voz.");
            }

            var response = await _chatbotService.GetChatbotResponseAsync(
                userMessage,
                "Contexto por defecto"); 

            await _speechService.SpeakTextAsync(response);

            return Ok(new { userMessage, response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}

public class ChatRequest
{
    public string UserMessage { get; set; }
    public string EventContext { get; set; }
}