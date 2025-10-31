using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SubspaceStats.Areas.Identity.Data;
using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Filters;
using SubspaceStats.Options;
using SubspaceStats.Services;

namespace SubspaceStats
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            const string identityConnectionStringName = "AspNetCoreIdentity";
            var connectionString = builder.Configuration.GetConnectionString(identityConnectionStringName) ?? throw new InvalidOperationException($"Connection string '{identityConnectionStringName}' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            builder.Services.AddDefaultIdentity<SubspaceStatsUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.Configure<GeneralOptions>(builder.Configuration.GetSection(GeneralOptions.GeneralSectionKey));
            builder.Services.Configure<StatOptions>(builder.Configuration.GetSection(StatOptions.StatsSectionKey));
            builder.Services.Configure<LeagueOptions>(builder.Configuration.GetSection(LeagueOptions.LeagueSectionKey));

            // The connection string the services use is named: "SubspaceStats"
            // In Development use user-secrets.
            // In Production use Environment Variables or an online secrets store, depending on your host.

            builder.Services.AddHybridCache();

            builder.Services.AddSingleton<ILeagueRepository, LeagueRepository>();
            builder.Services.AddSingleton<IStatsRepository, StatsRepository>();

            // Note: Scoped because it uses ASP.NET Identity
            builder.Services.AddScoped<IAuthorizationHandler, ManageLeagueAuthorizationHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, ManageSeasonAuthorizationHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, ManageSeasonDetailsAuthorizationHandler>();

            if (!builder.Environment.IsDevelopment())
            {
                // Email Sender Service for ASP.NET Core Identity
                // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm
                // To use this, configure the AuthenticationEmail section in appsettings.json
                builder.Services.AddTransient<IEmailSender<SubspaceStatsUser>, EmailSender>();
                builder.Services.Configure<AuthenticationEmailOptions>(builder.Configuration.GetSection(AuthenticationEmailOptions.AuthenticationEmailSectionKey));
            }

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(PolicyNames.Manager, policy => policy.AddRequirements(new ManagerRequirement()));

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<OperationCanceledExceptionFilter>();
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();

                await SeedAuth.Initialize(serviceProvider);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.MapStaticAssets();

            IOptions<LeagueOptions> leagueOptions = app.Services.GetRequiredService<IOptions<LeagueOptions>>();

            if(!string.IsNullOrWhiteSpace(leagueOptions.Value.ImagePhysicalPath) && !string.IsNullOrWhiteSpace(leagueOptions.Value.ImageUrlPath))
            app.UseStaticFiles(
                new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(leagueOptions.Value.ImagePhysicalPath),
                    RequestPath = leagueOptions.Value.ImageUrlPath
                });

            app.UseRouting();

            app.UseAuthorization();

            app.MapAreaControllerRoute(
                name: "LeagueSeasonPlayers",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/Players/{action}",
                defaults: new { controller = "SeasonPlayer" });

            app.MapAreaControllerRoute(
                name: "LeagueSeasonTeamCreate",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/Teams/Create",
                defaults: new { controller = "Team", action = "Create" });

            app.MapAreaControllerRoute(
                name: "LeagueSeasonGames",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/Games/{action}",
                defaults: new { controller = "SeasonGame" });

            app.MapAreaControllerRoute(
                name: "LeagueSeasonRound",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/Rounds/{roundNumber:int}/{action=Detail}",
                defaults: new { controller = "SeasonRound" });

            app.MapAreaControllerRoute(
                name: "LeagueSeasonRoundCreate",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/Rounds/Create",
                defaults: new { controller = "SeasonRound", action = "Create" });

            app.MapAreaControllerRoute(
                name: "LeagueSeasonRound",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/Roles/{action=Index}",
                defaults: new { controller = "SeasonRoles" });

            app.MapAreaControllerRoute(
                name: "LeagueSeason",
                areaName: "League",
                pattern: "League/Season/{seasonId:long}/{action=Index}",
                defaults: new { controller = "Season" });

            app.MapAreaControllerRoute(
                name: "LeagueCreateSeason",
                areaName: "League",
                pattern: "League/{leagueId:long}/CreateSeason",
                defaults: new { controller = "Season", action = "Create" });

            app.MapAreaControllerRoute(
                name: "LeagueManage",
                areaName: "League",
                pattern: "League/Manage",
                defaults: new { controller = "League", action = "Index" });

            app.MapAreaControllerRoute(
                name: "LeagueNav",
                areaName: "League",
                pattern: "League/Nav",
                defaults: new { controller = "Home", action = "Nav" });

            app.MapAreaControllerRoute(
                name: "LeagueCreate",
                areaName: "League",
                pattern: "League/Create",
                defaults: new { controller = "League", action = "Create" });

            app.MapAreaControllerRoute(
                name: "LeagueFranchise",
                areaName: "League",
                pattern: "League/Franchise",
                defaults: new { controller = "Franchise", action = "Index" });

            app.MapAreaControllerRoute(
                name: "LeagueFranchise",
                areaName: "League",
                pattern: "League/Franchise/{franchiseId:long}/{action=Details}",
                defaults: new { controller = "Franchise" });

            app.MapAreaControllerRoute(
                name: "LeagueTeam",
                areaName: "League",
                pattern: "League/Team/{teamId:long}/{action=Index}",
                defaults: new { controller = "Team" });

            app.MapAreaControllerRoute(
                name: "LeagueLeagueRoles",
                areaName: "League",
                pattern: "League/{leagueId:long}/Roles/{action=Index}",
                defaults: new { controller = "LeagueRoles" });

            app.MapAreaControllerRoute(
                name: "LeagueLeague",
                areaName: "League",
                pattern: "League/{leagueId:long}/{action=Details}",
                defaults: new { controller = "League" });

            app.MapAreaControllerRoute(
                name: "LeagueAreaDefault",
                areaName: "League",
                pattern: "League/{controller=Home}/{action=Index}/{id?}");

            app.MapAreaControllerRoute(
                name: "AdminArea",
                areaName: "Admin",
                pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "Game",
                pattern: "Game/{id:long}",
                defaults: new { controller = "Game", action = "Index" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // For using the ASP.NET Core Identity RCL
            app.MapRazorPages();

            app.Run();
        }
    }
}