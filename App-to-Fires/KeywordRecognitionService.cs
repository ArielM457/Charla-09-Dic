using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace App_to_Fires
{
    public class KeywordRecognitionService : IDisposable
    {
        private readonly KeywordRecognizer _keywordRecognizer;
        private readonly KeywordRecognitionModel _keywordModel;
        private readonly SpeechService _speechService;
        private readonly ChatbotService _chatbotService;
        private bool isListening = false;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<string> OnKeywordRecognized;

        public KeywordRecognitionService(
            IConfiguration configuration,
            SpeechService speechService,
            ChatbotService chatbotService)
        {
            _speechService = speechService;
            _chatbotService = chatbotService;

            _keywordModel = KeywordRecognitionModel.FromFile("Reconocimiento-Jarvis.table");

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            _keywordRecognizer = new KeywordRecognizer(audioConfig);


            //EVENTO
            _keywordRecognizer.Recognized += KeywordRecognizer_Recognized;
        }

        private async void KeywordRecognizer_Recognized(object sender, KeywordRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                OnKeywordRecognized?.Invoke(this, "Jarvis detectado");
                await StartConversationAsync();
            }
        }

        private async Task StartConversationAsync()
        {
            try
            {
                // Detener temporalmente el reconocimiento de palabra clave
                await StopListeningAsync();

                // Iniciar el reconocimiento de voz y obtener el mensaje del usuario
                string userMessage = await _speechService.RecognizeSpeechAsync();

                if (!string.IsNullOrEmpty(userMessage))
                {
                    // Obtener respuesta del chatbot
                    string response = await _chatbotService.GetChatbotResponseAsync(
                        userMessage,
                        "Eres el Asistente Jarvis");

                    // Convertir la respuesta a voz
                    await _speechService.SpeakTextAsync(response);
                }
            }
            finally
            {
                // Reanudar el reconocimiento de palabra clave
                await StartListeningAsync();
            }
        }

        public Task StartListeningAsync()
        {
            if (!isListening)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                isListening = true;

                return Task.Run(async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await _keywordRecognizer.RecognizeOnceAsync(_keywordModel);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error en el reconocimiento: {ex.Message}");
                        }
                    }
                });
            }

            return Task.CompletedTask;
        }

        public async Task StopListeningAsync()
        {
            if (isListening)
            {
                _cancellationTokenSource?.Cancel();
                await _keywordRecognizer.StopRecognitionAsync();
                isListening = false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _keywordRecognizer.Dispose();
            _keywordModel.Dispose();
        }
    }
}
