using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

// South African Windows machines use a COMMA as the decimal separator
// (en-ZA), but browsers always post "10.5" with a dot - which would make
// every decimal field (distance, fee, latitude) fail to bind. Pinning the
// culture to invariant on every machine and inside Docker avoids that.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ---------------- MVC ----------------
builder.Services.AddControllersWithViews(options =>
{
    // Every POST form must carry an anti-forgery token (CSRF protection).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    // Optional fields are declared with "?"; don't treat other strings as [Required].
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ---------------- Session (the MVC app's own authenticated state) ----------------
// Holds the user's id, role and name, plus the cookie of their API session.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// ---------------- Typed HTTP client for the Part 2 API ----------------
// The ONLY way this project reads or writes data. The base address comes from
// configuration, so Docker can override it with:  -e ApiBaseUrl=http://host:5000/
builder.Services.AddTransient<ApiSessionHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    // IMPORTANT: turn the handler's built-in cookie jar OFF. The handler is
    // shared by every user, so a shared jar would leak one user's API session
    // to another. ApiSessionHandler forwards each user's own cookie instead.
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    .AddHttpMessageHandler<ApiSessionHandler>();

var app = builder.Build();

// Friendly error pages instead of raw stack traces, in every environment.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { CultureInfo.InvariantCulture }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
