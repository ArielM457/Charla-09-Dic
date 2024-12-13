namespace App_to_Fires
{
    public class KeywordOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;

        public KeywordOrchestrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartRecognitionAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var keywordService = scope.ServiceProvider.GetRequiredService<KeywordRecognitionService>();
            await keywordService.StartListeningAsync();
        }
    }
}
