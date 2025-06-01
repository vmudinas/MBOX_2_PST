using MboxToPstBlazorApp.Components;
using MboxToPstBlazorApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure form options for large file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue; // Remove limit
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure server options for large uploads
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue; // Remove limit
});

// Register custom services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<GmailService>();

// Add minimal authentication for Gmail OAuth
builder.Services.AddAuthentication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add download endpoint for converted files
app.MapGet("/download", async (HttpContext context, string file) =>
{
    if (string.IsNullOrEmpty(file) || !File.Exists(file))
    {
        return Results.NotFound("File not found");
    }

    // Security check: only allow downloads from the uploads directory
    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    var fullPath = Path.GetFullPath(file);
    var uploadsFullPath = Path.GetFullPath(uploadsDir);
    
    if (!fullPath.StartsWith(uploadsFullPath))
    {
        return Results.Forbid();
    }

    var fileName = Path.GetFileName(file);
    var fileBytes = await File.ReadAllBytesAsync(file);
    var contentType = Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".pst" => "application/vnd.ms-outlook",
        ".mbox" => "application/mbox",
        _ => "application/octet-stream"
    };

    return Results.File(fileBytes, contentType, fileName);
});

app.Run();
