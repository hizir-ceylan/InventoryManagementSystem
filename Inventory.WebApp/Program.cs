var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Remove HTTPS redirection for development and flexible deployment
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

// Default route to devices page
app.MapGet("/", () => Results.Redirect("/Devices"));

app.Run();

// Configuration class for API settings
public class ApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5093";
}
