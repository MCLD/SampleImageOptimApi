using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add our ImageOptimApi client
builder.Services.AddHttpClient<ImageOptimApi.Client>()
        .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            AllowAutoRedirect = true
        })
        .ConfigureHttpClient(_ =>
        {
            _.Timeout = TimeSpan.FromSeconds(30);
            _.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TestApp", "1.0.0"));
        });

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();