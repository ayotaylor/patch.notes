using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Models.Auth;
using Backend.Data;
using Microsoft.Extensions.Logging;

namespace Backend.Tests;

public class DatabaseFixture : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    public UserManager<User> UserManager { get; }
    public ApplicationDbContext Context { get; }

    public DatabaseFixture()
    {
        // Setup MySQL database and services
        var services = new ServiceCollection();
        
        // Use hardcoded connection string to avoid configuration conflicts
        var mySqlConnectionString = "Server=localhost;Port=3306;Database=patchnotesdb;Uid=root;Pwd=password;";
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(mySqlConnectionString, new MySqlServerVersion(new Version(8, 0, 21))));

        // Add Identity services
        services.AddIdentity<User, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add logging
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        
        // Create scope and get services
        _scope = _serviceProvider.CreateScope();
        UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    public async Task CleanupTestUsersAsync()
    {
        // Clean up any existing test users (emails ending with @patch.notes)
        var existingTestUsers = Context.Users.Where(u => u.Email!.EndsWith("@patch.notes")).ToList();
        if (existingTestUsers.Count > 0)
        {
            var existingTestUserIds = existingTestUsers.Select(u => u.Id).ToList();
            var existingTestProfiles = Context.UserProfiles.Where(p => existingTestUserIds.Contains(p.UserId)).ToList();
            
            Context.UserProfiles.RemoveRange(existingTestProfiles);
            Context.Users.RemoveRange(existingTestUsers);
            await Context.SaveChangesAsync();
        }
    }

    public async Task CleanupSpecificUsersAsync(List<string> userIds)
    {
        if (userIds.Count == 0) return;

        // Clean up specific users and their profiles
        var usersToDelete = Context.Users.Where(u => userIds.Contains(u.Id)).ToList();
        var profilesToDelete = Context.UserProfiles.Where(p => userIds.Contains(p.UserId)).ToList();
        
        if (usersToDelete.Count > 0 || profilesToDelete.Count > 0)
        {
            Context.UserProfiles.RemoveRange(profilesToDelete);
            Context.Users.RemoveRange(usersToDelete);
            await Context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _scope?.Dispose();
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }
}