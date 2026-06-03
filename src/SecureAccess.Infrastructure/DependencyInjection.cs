using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Features.AccessRequests;
using SecureAccess.Application.Features.Audit;
using SecureAccess.Application.Features.Auth;
using SecureAccess.Application.Features.Bulk;
using SecureAccess.Application.Features.Clients;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Application.Features.Dashboard;
using SecureAccess.Application.Features.ImportExport;
using SecureAccess.Application.Features.Machines;
using SecureAccess.Application.Features.Reports;
using SecureAccess.Application.Features.Search;
using SecureAccess.Application.Features.Templates;
using SecureAccess.Application.Features.Users;
using SecureAccess.Infrastructure.Persistence;
using SecureAccess.Infrastructure.Security;
using SecureAccess.Infrastructure.Services;

namespace SecureAccess.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core, Data Protection, the encryption subsystem and all
    /// application services. Shared by both the API and Blazor hosts.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        SqlClientWindowsBootstrap.Initialize();

        // QuestPDF community license (free, no ongoing fees).
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = SqlConnectionStringHelper.Resolve(
            config.GetConnectionString("DefaultConnection"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.Configure<EncryptionOptions>(config.GetSection(EncryptionOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        var dpPath = config[$"{EncryptionOptions.SectionName}:DataProtectionKeysPath"] ?? "App_Data/keys/dp";
        var dp = services.AddDataProtection().SetApplicationName("SecureAccess");
        dp.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(dpPath)));
        if (OperatingSystem.IsWindows())
            dp.ProtectKeysWithDpapi();

        services.AddHttpContextAccessor();

        services.AddScoped<IEncryptionService, AesGcmEncryptionService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<CredentialDataProtector>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IMachineService, MachineService>();
        services.AddScoped<ICredentialTypeService, CredentialTypeService>();
        services.AddScoped<ICredentialService, CredentialService>();
        services.AddScoped<IAccessRequestService, AccessRequestService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IBulkService, BulkService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
