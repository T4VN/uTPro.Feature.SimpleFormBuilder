using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace uTPro.Feature.SimpleFormBuilder.TestSite;

/// <summary>
/// Seeds the backoffice test accounts documented in the package README so that, after the
/// SQLite database is deleted and recreated, the role/permission matrix can be exercised
/// again without recreating users by hand.
///
/// All accounts share the unattended admin password (<c>Admin1234!</c>).
///
/// | Email                   | Group(s)                         | Purpose                                              |
/// |-------------------------|----------------------------------|------------------------------------------------------|
/// | admin@example.com       | Administrators (unattended)      | Everything                                           |
/// | editor@example.com      | Editor (+ uTPro Form section)    | View/export only; sensitive masked                   |
/// | editorSD@example.com    | Editor + sensitiveData           | As editor + can view decrypted sensitive values      |
/// | adminCustom@example.com | Admin Custom (Settings + section)| Can design/edit forms (canEdit) but sensitive masked |
///
/// This is TEST-SITE ONLY scaffolding and is not part of the shipped package.
/// </summary>
public sealed class TestSiteUserSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TestUserSeeder>();
}

public sealed class TestUserSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // The custom backoffice section registered by the package (umbraco-package.json).
    private const string FormSectionAlias = "uTPro.Section.Form";
    private const string TestPassword = "Admin1234!";

    private const string SensitiveDataAlias = "sensitiveData";
    private const string AdminCustomAlias = "adminCustom";
    private const string EditorGroupAlias = "editor";

    private readonly IUserService _userService;
    private readonly IUserGroupService _userGroupService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<TestUserSeeder> _logger;

    public TestUserSeeder(
        IUserService userService,
        IUserGroupService userGroupService,
        IShortStringHelper shortStringHelper,
        ILogger<TestUserSeeder> logger)
    {
        _userService = userService;
        _userGroupService = userGroupService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync();
        }
        catch (Exception ex)
        {
            // Never let test-data seeding take down the site boot.
            _logger.LogWarning(ex, "Test user seeding failed.");
        }
    }

    private async Task SeedAsync()
    {
        Guid performingUserKey = Constants.Security.SuperUserKey;

        // 1. Make sure the built-in Editor group can see the uTPro Form section.
        IUserGroup? editor = await _userGroupService.GetAsync(EditorGroupAlias);
        if (editor is not null && editor.AllowedSections.Contains(FormSectionAlias) == false)
        {
            editor.AddAllowedSection(FormSectionAlias);
            await _userGroupService.UpdateAsync(editor, performingUserKey);
        }

        // 2. Ensure the "sensitiveData" group exists (grants canViewSensitive).
        IUserGroup? sensitive = await _userGroupService.GetAsync(SensitiveDataAlias);
        if (sensitive is null)
        {
            var group = new UserGroup(_shortStringHelper)
            {
                Alias = SensitiveDataAlias,
                Name = "Sensitive Data",
                Icon = "icon-lock",
            };
            group.AddAllowedSection(FormSectionAlias);
            await _userGroupService.CreateAsync(group, performingUserKey);
        }

        // 3. Ensure the "adminCustom" group exists: an admin-like clone that includes the
        //    Settings section (=> canEdit) and the uTPro Form section, but is neither the
        //    built-in Administrators group nor sensitiveData (=> sensitive values stay masked).
        IUserGroup? adminCustom = await _userGroupService.GetAsync(AdminCustomAlias);
        if (adminCustom is null)
        {
            var group = new UserGroup(_shortStringHelper)
            {
                Alias = AdminCustomAlias,
                Name = "Admin Custom",
                Icon = "icon-users-alt",
                HasAccessToAllLanguages = true,
            };

            // Clone sections/permissions from the built-in Administrators group when available.
            IUserGroup? admin = await _userGroupService.GetAsync(Constants.Security.AdminGroupAlias);
            if (admin is not null)
            {
                foreach (var section in admin.AllowedSections)
                {
                    group.AddAllowedSection(section);
                }

                group.Permissions = new HashSet<string>(admin.Permissions);
            }

            // Guarantee the two sections that matter for this feature.
            group.AddAllowedSection(Constants.Applications.Settings);
            group.AddAllowedSection(FormSectionAlias);

            await _userGroupService.CreateAsync(group, performingUserKey);
        }

        // 3b. TEST-SITE ONLY: grant the uTPro Form section to EVERY user group so the
        //     section is visible regardless of which group a test account belongs to.
        await EnsureAllGroupsHaveFormSectionAsync(performingUserKey);

        // 4. Create the three non-admin test users (admin@example.com is created unattended).
        await EnsureUserAsync("editor@example.com", "Editor", performingUserKey,
            EditorGroupAlias);

        await EnsureUserAsync("editorSD@example.com", "Editor SensitiveData", performingUserKey,
            EditorGroupAlias, SensitiveDataAlias);

        await EnsureUserAsync("adminCustom@example.com", "Admin Custom", performingUserKey,
            AdminCustomAlias);
    }

    private async Task EnsureAllGroupsHaveFormSectionAsync(Guid performingUserKey)
    {
        // Page through every user group and add the uTPro Form section where missing.
        PagedModel<IUserGroup> page = await _userGroupService.GetAllAsync(0, int.MaxValue);
        foreach (IUserGroup group in page.Items)
        {
            if (group.AllowedSections.Contains(FormSectionAlias))
            {
                continue;
            }

            group.AddAllowedSection(FormSectionAlias);
            Attempt<IUserGroup, UserGroupOperationStatus> result =
                await _userGroupService.UpdateAsync(group, performingUserKey);

            if (result.Success == false)
            {
                _logger.LogWarning("Could not add uTPro Form section to group {Alias}: {Status}.",
                    group.Alias, result.Status);
            }
        }
    }

    private async Task EnsureUserAsync(string email, string name, Guid performingUserKey, params string[] groupAliases)
    {
        // Skip if the user already exists (e.g. on a subsequent boot).
        if (_userService.GetByEmail(email) is not null)
        {
            return;
        }

        // Resolve the group keys for the requested aliases.
        var groupKeys = new HashSet<Guid>();
        foreach (var alias in groupAliases)
        {
            IUserGroup? group = await _userGroupService.GetAsync(alias);
            if (group is not null)
            {
                groupKeys.Add(group.Key);
            }
        }

        if (groupKeys.Count == 0)
        {
            _logger.LogWarning("No user groups resolved for {Email}; skipping.", email);
            return;
        }

        var createModel = new UserCreateModel
        {
            Email = email,
            UserName = email,
            Name = name,
            Kind = UserKind.Default,
            UserGroupKeys = groupKeys,
        };

        Attempt<UserCreationResult, UserOperationStatus> createResult =
            await _userService.CreateAsync(performingUserKey, createModel, approveUser: true);

        if (createResult.Success == false)
        {
            _logger.LogWarning("Could not create test user {Email}: {Status}.", email, createResult.Status);
            return;
        }

        // The created user starts with a random password; set the known test password.
        IUser? created = _userService.GetByEmail(email);
        if (created is null)
        {
            return;
        }

        Attempt<PasswordChangedModel, UserOperationStatus> passwordResult =
            await _userService.ChangePasswordAsync(performingUserKey, new ChangeUserPasswordModel
            {
                UserKey = created.Key,
                NewPassword = TestPassword,
            });

        if (passwordResult.Success == false)
        {
            _logger.LogWarning("Created {Email} but could not set its password: {Status}.", email, passwordResult.Status);
        }
        else
        {
            _logger.LogInformation("Seeded test user {Email}.", email);
        }
    }
}
