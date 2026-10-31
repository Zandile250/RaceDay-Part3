using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC ----
builder.Services.AddControllersWithViews(options =>
{
    // Every POST must carry the anti-forgery token the forms include.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ApiExceptionFilter>();
});

// ---- Session management: remembers who is signed in (and their role) on every page ----
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);   // same as the API's session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---- The RaceDay API (Part 2): the only place this application gets or changes data ----
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
    throw new InvalidOperationException("Configuration 'Api:BaseUrl' must be the absolute URL of the RaceDay API, e.g. http://localhost:8080/");

builder.Services.AddHttpClient<IRaceDayApi, RaceDayApi>(client =>
    {
        client.BaseAddress = new Uri(apiUri.AbsoluteUri.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // Cookies are attached per user by RaceDayApi, never by the shared handler.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

UiHelpers.CurrencySymbol = builder.Configuration["Ui:CurrencySymbol"] ?? "R";

var app = builder.Build();

// One culture everywhere so numbers and dates behave the same on every machine and in Docker.
var culture = new RequestCulture(CultureInfo.InvariantCulture);
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = culture,
    SupportedCultures = new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { CultureInfo.InvariantCulture }
});

// Friendly pages instead of raw errors, in every environment.
app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/status/{0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllers();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
