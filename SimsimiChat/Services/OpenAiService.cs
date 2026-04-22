using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SimsimiChat.Configuration;
using SimsimiChat.Models;
using SimsimiChat.Models.Dtos;

namespace SimsimiChat.Services;

internal class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}

internal class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent Content { get; set; } = null!;
}

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; } = new();
}

public interface IAiService
{
    Task<string> GetResponseAsync(string userMessage, RudenessLevel rudenessLevel, IReadOnlyList<Message>? sessionMessages = null);
}

public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiService> _logger;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public OpenAiService(HttpClient httpClient, IOptions<OpenAiSettings> settings, ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured");
        }
    }

    public async Task<string> GetResponseAsync(string userMessage, RudenessLevel rudenessLevel, IReadOnlyList<Message>? sessionMessages = null)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("User message cannot be empty", nameof(userMessage));
        }

        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var fullPrompt = BuildConversationPrompt(userMessage, rudenessLevel, sessionMessages);

                var requestBody = new
                {
                    contents = new object[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = GetTemperatureByRudeness(rudenessLevel),
                        maxOutputTokens = 500
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"{GeminiApiBaseUrl}/{_settings.Model}:generateContent?key={_settings.ApiKey}";
                System.Console.WriteLine($"===== GEMINI API REQUEST (Attempt {retryCount + 1}/{maxRetries}) =====");
                System.Console.WriteLine($"URL: {url}");
                System.Console.WriteLine($"Model: {_settings.Model}");
                System.Console.WriteLine($"Request Body: {jsonContent}");
                System.Console.WriteLine($"===============================");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"===== GEMINI API ERROR =====");
                    System.Console.WriteLine($"Status Code: {response.StatusCode}");
                    System.Console.WriteLine($"Error: {errorContent}");
                    System.Console.WriteLine($"URL: {url}");
                    System.Console.WriteLine($"============================");

                    // Retry on 503 (Service Unavailable) or 429 (Too Many Requests)
                    if ((int)response.StatusCode == 503 || (int)response.StatusCode == 429)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            int waitMs = (int)Math.Pow(2, retryCount) * 1000; // 2s, 4s, 8s
                            System.Console.WriteLine($"⏳ API overloaded. Waiting {waitMs}ms before retry {retryCount}/{maxRetries - 1}...");
                            _logger.LogWarning($"Gemini API {response.StatusCode}, retrying in {waitMs}ms (attempt {retryCount}/{maxRetries - 1})");
                            await Task.Delay(waitMs);
                            continue; // Retry
                        }
                    }

                    _logger.LogError($"Gemini API error: {response.StatusCode} - {errorContent}");
                    return "Sorry, I'm having trouble connecting. Please try again later.";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"===== GEMINI API RESPONSE =====");
                System.Console.WriteLine($"Status Code: {response.StatusCode}");
                System.Console.WriteLine($"Response: {responseContent}");
                System.Console.WriteLine($"================================");

                var geminiResponse = System.Text.Json.JsonSerializer.Deserialize<GeminiResponse>(responseContent);

                if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
                {
                    _logger.LogWarning("Empty response from Gemini API");
                    return "I couldn't generate a response. Please try again.";
                }

                var result = geminiResponse.Candidates[0].Content.Parts[0].Text;

                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("Empty message content from Gemini API");
                    return "I couldn't generate a response. Please try again.";
                }

                _logger.LogInformation($"AI response generated successfully with rudeness level: {rudenessLevel}");
                return result;
            }
            catch (HttpRequestException ex)
            {
                retryCount++;
                if (retryCount < maxRetries)
                {
                    int waitMs = (int)Math.Pow(2, retryCount) * 1000;
                    System.Console.WriteLine($"⏳ HTTP error: {ex.Message}. Waiting {waitMs}ms before retry {retryCount}/{maxRetries - 1}...");
                    _logger.LogWarning(ex, $"HTTP error on attempt {retryCount}, retrying in {waitMs}ms");
                    await Task.Delay(waitMs);
                    continue;
                }

                _logger.LogError(ex, "HTTP error calling Gemini API after all retries");
                return "Sorry, I'm having trouble connecting. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting response from Gemini on attempt {retryCount + 1}");
                return "An error occurred while processing your request.";
            }
        }

        // All retries failed
        _logger.LogError("All retry attempts failed for Gemini API");
        return "Sorry, the AI service is temporarily unavailable. Please try again later.";
    }

    private string GetSystemPrompt(RudenessLevel rudenessLevel)
    {
        return rudenessLevel switch
        {
            RudenessLevel.Polite => 
                "You are a helpful, polite, and respectful assistant. Always provide thoughtful, well-mannered responses. Use formal language and treat the user with utmost respect.",
            
            RudenessLevel.Neutral => 
                "You are a straightforward, direct assistant. Provide clear and concise answers without unnecessary pleasantries. Be factual and efficient.",
            
            RudenessLevel.Casual => 
                "You are a friendly and relaxed assistant. Use conversational language, humor, and casual tone while remaining helpful. Be approachable and warm.",
            
            RudenessLevel.Sarcastic => 
                "You are a witty assistant with a sarcastic sense of humor. Use clever sarcasm and witty remarks, but never cross into being genuinely offensive or hurtful. Keep it lighthearted.",
            
            RudenessLevel.Rude => 
                "You are a blunt and no-nonsense assistant. Be direct and blunt in your responses. Use bold language and don't sugarcoat things. However, never be abusive or discriminatory.",
            
            _ => 
                "You are a helpful assistant. Provide clear and concise answers."
        };
    }

    private string BuildConversationPrompt(string userMessage, RudenessLevel rudenessLevel, IReadOnlyList<Message>? sessionMessages)
    {
        var builder = new StringBuilder();
        builder.AppendLine(GetSystemPrompt(rudenessLevel));
        builder.AppendLine();
        builder.AppendLine("Use the session context below to keep the conversation consistent. If the context conflicts with the latest user message, prioritize the latest user message.");

        var sanitizedMessages = sessionMessages?
            .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Content))
            .OrderBy(m => m.CreatedAt ?? DateTime.MinValue)
            .ToList() ?? new List<Message>();

        if (sanitizedMessages.Count > 0)
        {
            var summary = BuildSessionSummary(sanitizedMessages);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                builder.AppendLine();
                builder.AppendLine("Session summary:");
                builder.AppendLine(summary);
            }

            var recentTranscript = BuildRecentTranscript(sanitizedMessages);
            if (!string.IsNullOrWhiteSpace(recentTranscript))
            {
                builder.AppendLine();
                builder.AppendLine("Recent conversation:");
                builder.AppendLine(recentTranscript);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Latest user message:");
        builder.AppendLine($"User: {userMessage.Trim()}");
        builder.AppendLine();
        builder.Append("Reply as the assistant, taking the full session context into account.");

        return builder.ToString();
    }

    private string BuildSessionSummary(IReadOnlyList<Message> sessionMessages)
    {
        const int summarySourceLimit = 12;
        const int summaryItemCharLimit = 220;

        if (sessionMessages.Count <= 6)
        {
            return "This session is still short. Use the recent conversation directly.";
        }

        var olderMessages = sessionMessages
            .Take(Math.Max(0, sessionMessages.Count - 6))
            .TakeLast(summarySourceLimit)
            .Select(message =>
            {
                var speaker = NormalizeSenderType(message.SenderType);
                var content = TrimContent(message.Content, summaryItemCharLimit);
                return $"{speaker}: {content}";
            });

        return string.Join("\n", olderMessages);
    }

    private string BuildRecentTranscript(IReadOnlyList<Message> sessionMessages)
    {
        const int recentMessageLimit = 6;
        const int transcriptCharLimit = 350;

        return string.Join(
            "\n",
            sessionMessages
                .TakeLast(recentMessageLimit)
                .Select(message =>
                {
                    var speaker = NormalizeSenderType(message.SenderType);
                    var content = TrimContent(message.Content, transcriptCharLimit);
                    return $"{speaker}: {content}";
                }));
    }

    private static string NormalizeSenderType(string? senderType)
    {
        return string.Equals(senderType, "Bot", StringComparison.OrdinalIgnoreCase)
            ? "Assistant"
            : "User";
    }

    private static string TrimContent(string content, int maxLength)
    {
        var normalized = content.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}...";
    }

    private float GetTemperatureByRudeness(RudenessLevel rudenessLevel)
    {
        return rudenessLevel switch
        {
            RudenessLevel.Polite => 0.7f,
            RudenessLevel.Neutral => 0.5f,
            RudenessLevel.Casual => 0.8f,
            RudenessLevel.Sarcastic => 0.85f,
            RudenessLevel.Rude => 0.6f,
            _ => 0.7f
        };
    }
}

