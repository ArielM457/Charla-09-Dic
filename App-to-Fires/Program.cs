using App_to_Fires;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Registrar servicios
builder.Services.AddScoped<ChatbotService>();
builder.Services.AddSingleton<SpeechService>();
builder.Services.AddScoped<KeywordRecognitionService>();
builder.Services.AddSingleton<KeywordOrchestrator>();

// Agregar política CORS si es necesario
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Inicia el KeywordRecognitionService mediante el KeywordOrchestrator
var orchestrator = app.Services.GetRequiredService<KeywordOrchestrator>();
_ = Task.Run(() => orchestrator.StartRecognitionAsync());

// Manejar el cierre de la aplicación para liberar recursos
app.Lifetime.ApplicationStopping.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var keywordService = scope.ServiceProvider.GetRequiredService<KeywordRecognitionService>();
    keywordService.StopListeningAsync().Wait();
    keywordService.Dispose();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseCors("AllowAll");

// Mapear los controladores API
app.MapControllers();

// Mapear las páginas Razor
app.MapRazorPages();

// Redirigir la raíz a index.html
app.MapGet("/", context =>
{
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

app.Run();
