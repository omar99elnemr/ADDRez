using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.DTOs.Settings;

// Company
public record CompanySettingsDto(
    int Id, string Name, string? Email, string? Phone, string? Website,
    string? LogoUrl, string Timezone, string DefaultCurrency, string DefaultLocale
);
public record UpdateCompanyRequest(
    string Name, string? Email, string? Phone, string? Website,
    string Timezone, string DefaultCurrency, string DefaultLocale
);

// Branches
public record OutletDto(
    int Id, string Name, string? Address, string? Phone, string? Email,
    int DefaultGracePeriodMinutes, int DefaultTurnTimeMinutes,
    bool AutoConfirmOnline, bool IsActive,
    IEnumerable<OutletAreaDto> Areas
);
public record OutletAreaDto(int Id, string Name, int SortOrder);
public record CreateOutletRequest(
    string Name, string? Address, string? Phone, string? Email,
    int DefaultGracePeriodMinutes, int DefaultTurnTimeMinutes, bool AutoConfirmOnline,
    string[]? AreaNames
);
public record UpdateOutletRequest(
    string Name, string? Address, string? Phone, string? Email,
    int DefaultGracePeriodMinutes, int DefaultTurnTimeMinutes,
    bool AutoConfirmOnline, bool IsActive,
    string[]? AreaNames
);

// Users
public record UserListDto(
    int Id, string Username, string Email, string FirstName, string LastName,
    string FullName, string? Phone, bool IsActive, DateTime? LastLoginAt,
    IEnumerable<string> Roles, IEnumerable<string> Outlets
);
public record CreateUserRequest(
    string Username, string Email, string Password, string FirstName,
    string LastName, string? Phone, int[] RoleIds, int[] BranchIds
);
public record UpdateUserRequest(
    string Email, string FirstName, string LastName, string? Phone,
    bool IsActive, int[] RoleIds, int[] BranchIds
);
public record ResetPasswordRequest(string NewPassword);

// Roles
public record RoleDetailDto(
    int Id, string Name, string Slug, string? Description, bool IsSystem,
    IEnumerable<int> PermissionIds
);
public record PermissionDto(int Id, string Key, string Name, string Group, string? Description);
public record CreateRoleRequest(string Name, string Slug, string? Description, int[] PermissionIds);
public record UpdateRoleRequest(string Name, string? Description, int[] PermissionIds);

// Tag Categories
public record TagCategoryDto(int Id, string Name, string? Icon, TagType Type, int SortOrder, IEnumerable<TagSettingsDto> Tags);
public record CreateTagCategoryRequest(string Name, string? Icon, TagType Type);
public record UpdateTagCategoryRequest(string Name, string? Icon, int SortOrder);

// Tags
public record TagSettingsDto(int Id, string Name, string Color, TagType Type, int? TagCategoryId);
public record CreateTagRequest(string Name, string Color, TagType Type, int? TagCategoryId);
public record UpdateTagRequest(string Name, string Color, int? TagCategoryId);

// Client Categories
public record CategoryDto(int Id, string Name, string? Description, string Color, int Priority, bool IsActive);
public record CreateCategoryRequest(string Name, string? Description, string Color, int Priority);
public record UpdateCategoryRequest(string Name, string? Description, string Color, int Priority, bool IsActive);

// Notification Templates
public record TemplateDto(
    int Id, string Name, CommunicationType Type, CommunicationChannel Channel,
    string? Subject, string Body, bool IsActive
);
public record CreateTemplateRequest(
    string Name, CommunicationType Type, CommunicationChannel Channel,
    string? Subject, string Body
);
public record UpdateTemplateRequest(string Name, string? Subject, string Body, bool IsActive);

// Terms & Conditions
public record TermsDto(int Id, string Title, string Content, int SortOrder, bool IsActive);
public record CreateTermsRequest(string Title, string Content, int SortOrder);
public record UpdateTermsRequest(string Title, string Content, int SortOrder, bool IsActive);

// Time Slots
public record TimeSlotDto(
    int Id, string Name, string StartTime, string EndTime, int? LayoutId, string? LayoutName,
    bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday,
    string? StartDate, string? EndDate,
    int MaxCovers, int MaxReservations, int TurnTimeMinutes, int GracePeriodMinutes,
    bool RequireDeposit, decimal DepositAmountPerPerson, bool IsActive,
    IEnumerable<int> ExcludedCategoryIds
);
public record TimeSlotFullDto(
    int Id, string Name, string StartTime, string EndTime, int? LayoutId, string? LayoutName,
    bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday,
    string? StartDate, string? EndDate,
    int MaxCovers, int MaxReservations, int TurnTimeMinutes, int GracePeriodMinutes,
    bool RequireDeposit, decimal DepositAmountPerPerson, bool IsActive,
    IEnumerable<int> ExcludedCategoryIds,
    int OutletId, string OutletName
);
public record CreateTimeSlotRequest(
    string Name, string StartTime, string EndTime, int? LayoutId,
    bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday,
    string? StartDate, string? EndDate,
    int MaxCovers, int MaxReservations, int TurnTimeMinutes, int GracePeriodMinutes,
    bool RequireDeposit, decimal DepositAmountPerPerson, int[]? ExcludedCategoryIds,
    int[]? OutletIds = null
);
public record UpdateTimeSlotRequest(
    string Name, string StartTime, string EndTime, int? LayoutId,
    bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday,
    string? StartDate, string? EndDate,
    int MaxCovers, int MaxReservations, int TurnTimeMinutes, int GracePeriodMinutes,
    bool RequireDeposit, decimal DepositAmountPerPerson, int[]? ExcludedCategoryIds,
    bool IsActive = true
);

// Guest Lists
public record GuestListDto(
    int Id, int ReservationId, string? Name, int MaxCapacity,
    int TotalGuests, int CheckedInCount,
    IEnumerable<GuestListItemDto> Items
);
public record GuestListItemDto(
    int Id, string Name, string? Email, string? Phone, int Covers,
    GuestListItemStatus Status, string? Notes, DateTime? CheckedInAt
);
public record CreateGuestListItemRequest(string Name, string? Email, string? Phone, int Covers, string? Notes);
public record UpdateGuestListItemRequest(string Name, string? Email, string? Phone, int Covers, string? Notes);

// Campaigns
public record CampaignListDto(
    int Id, string Name, string? Subject, CommunicationChannel Channel,
    CampaignStatus Status, string TargetAudience,
    int TotalRecipients, int SentCount, int OpenCount,
    DateTime? ScheduledAt, DateTime? SentAt, DateTime CreatedAt
);
public record CreateCampaignRequest(
    string Name, string? Subject, string Body, CommunicationChannel Channel,
    string TargetAudience, int? TargetCategoryId, int? TargetTagId,
    DateTime? ScheduledAt
);
public record UpdateCampaignRequest(
    string Name, string? Subject, string Body, CommunicationChannel Channel,
    string TargetAudience, int? TargetCategoryId, int? TargetTagId,
    DateTime? ScheduledAt
);

// General Configurations (key-value per outlet)
public record ConfigurationDto(string Key, string Value, string? Description);
public record ConfigurationBulkRequest(Dictionary<string, string> Configs);
public record OutletConfigDto(int OutletId, string OutletName, Dictionary<string, string> Configs);

// Logs
public record OperationsLogDto(
    int Id, string Action, string EntityType, int? EntityId,
    string? Description, string? UserName, DateTime CreatedAt
);
public record ChangesLogDto(
    int Id, int ReservationId, string FieldName, string? OldValue,
    string? NewValue, string? UserName, DateTime CreatedAt
);
