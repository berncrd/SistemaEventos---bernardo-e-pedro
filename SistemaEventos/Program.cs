using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Banco de dados (Entity Framework Core + SQL Server)
// ---------------------------------------------------------------------------
var stringConexao = builder.Configuration.GetConnectionString("ConexaoSqlServer")
    ?? throw new InvalidOperationException("A string de conexão 'ConexaoSqlServer' não foi encontrada no appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(stringConexao)
           // Evita que pequenas diferenças de metadados entre versões do EF Core
           // impeçam a aplicação da migração inicial.
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// ---------------------------------------------------------------------------
// Autenticação e autorização (ASP.NET Core Identity com perfis)
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<IdentityErrorDescriberPtBr>()
    .AddClaimsPrincipalFactory<UsuarioClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Login";
    options.LogoutPath = "/Conta/Logout";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ---------------------------------------------------------------------------
// MVC com mensagens de validação em português
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    var mensagens = options.ModelBindingMessageProvider;
    mensagens.SetValueIsInvalidAccessor(valor => $"O valor '{valor}' é inválido.");
    mensagens.SetValueMustBeANumberAccessor(campo => $"O campo {campo} deve ser um número.");
    mensagens.SetValueMustNotBeNullAccessor(valor => "Este campo é obrigatório.");
    mensagens.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"O valor '{valor}' não é válido para {campo}.");
    mensagens.SetMissingBindRequiredValueAccessor(campo => $"O campo {campo} é obrigatório.");
    mensagens.SetMissingKeyOrValueAccessor(() => "Este campo é obrigatório.");
    mensagens.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisição é obrigatório.");
    mensagens.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"O valor '{valor}' é inválido.");
    mensagens.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado é inválido.");
    mensagens.SetNonPropertyValueMustBeANumberAccessor(() => "O campo deve ser um número.");
    mensagens.SetUnknownValueIsInvalidAccessor(campo => $"O valor informado para {campo} é inválido.");
});

builder.Services.AddScoped<IImagemService, ImagemService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Cultura pt-BR (datas dd/MM/yyyy, moeda R$ e vírgula decimal)
// ---------------------------------------------------------------------------
var culturaBrasil = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaBrasil;
CultureInfo.DefaultThreadCurrentUICulture = culturaBrasil;

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaBrasil),
    SupportedCultures = new List<CultureInfo> { culturaBrasil },
    SupportedUICultures = new List<CultureInfo> { culturaBrasil }
});

// ---------------------------------------------------------------------------
// Pipeline HTTP
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/erro/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Painel}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Aplica as migrações, cria os perfis e o administrador padrão
await DbInitializer.InicializarAsync(app.Services);

app.Run();
