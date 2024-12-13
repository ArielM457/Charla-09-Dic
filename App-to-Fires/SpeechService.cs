using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

public class SpeechService : IDisposable
{
    private readonly SpeechConfig _speechConfig;
    private readonly AudioConfig _audioConfig;
    private readonly SpeechSynthesizer _speechSynthesizer;
    private readonly SpeechRecognizer _speechRecognizer;

    private bool _disposed = false; // Flag para asegurar que Dispose se llame solo una vez.

    public SpeechService(IConfiguration configuration)
    {
        var speechKey = configuration["AzureSpeech:ApiKey"];
        var speechRegion = configuration["AzureSpeech:Region"];

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            throw new ArgumentException("Configuración de Azure Speech faltante. Verifica tu appsettings.json");
        }

        _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _speechConfig.SpeechRecognitionLanguage = "es-ES"; 
        _speechConfig.SpeechSynthesisVoiceName = "es-ES-AlvaroNeural"; // Reemplaza con la voz que desees

        _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        _speechSynthesizer = new SpeechSynthesizer(_speechConfig, _audioConfig); // Proporciona la configuración de audio al sintetizador.
        _speechRecognizer = new SpeechRecognizer(_speechConfig, _audioConfig); // Usa la configuración adecuada para el reconocimiento.
    }

    public async Task<string> RecognizeSpeechAsync()
    {
        try
        {
            var result = await _speechRecognizer.RecognizeOnceAsync();
            switch (result.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    return result.Text;
                case ResultReason.NoMatch:
                    return "No se pudo reconocer el habla.";
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    return $"Reconocimiento cancelado: {cancellation.Reason}";
                default:
                    return "Error en el reconocimiento de voz.";
            }
        }
        catch (Exception ex)
        {
            return $"Error en el reconocimiento de voz: {ex.Message}";
        }
    }

    public async Task SpeakTextAsync(string text)
    {
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SpeechService)); // Evitar usar el objeto después de ser eliminado.
            }

            var result = await _speechSynthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // La síntesis se completó correctamente
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new Exception($"Síntesis cancelada: {cancellation.Reason}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error en la síntesis de voz: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return; // Evitar llamar a Dispose múltiples veces.
        }

        _speechSynthesizer.Dispose();
        _speechRecognizer.Dispose();
        _disposed = true;
    }
}