using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.DTOs.FloorPlans;

public record LayoutDto(
    int Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive,
    IEnumerable<FloorPlanDto> FloorPlans
);

public record FloorPlanDto(
    int Id,
    string Name,
    int SortOrder,
    int Width,
    int Height,
    string? BackgroundColor,
    bool IsActive,
    IEnumerable<TableDto> Tables,
    IEnumerable<LandmarkDto> Landmarks
);

public record TableDto(
    int Id,
    string Name,
    string? Label,
    int MinCovers,
    int MaxCovers,
    TableShape Shape,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    bool IsCombinable,
    bool IsActive,
    int? TableTypeId,
    string? TableTypeName,
    string? CurrentStatus,
    string? CurrentReservationGuest
);

public record LandmarkDto(
    int Id,
    string Name,
    string Type,
    string? Icon,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation
);

public record CreateLayoutRequest(string Name, string? Description, bool IsDefault);
public record UpdateLayoutRequest(string Name, string? Description, bool IsDefault, bool IsActive);

public record CreateFloorPlanRequest(string Name, int SortOrder, int Width, int Height, string? BackgroundColor);

public record SaveTableRequest(
    int? Id,
    string Name,
    string? Label,
    int MinCovers,
    int MaxCovers,
    TableShape Shape,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    bool IsCombinable,
    int? TableTypeId
);

public record SaveLandmarkRequest(
    int? Id,
    string Name,
    string Type,
    string? Icon,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation
);

public record SaveFloorPlanLayoutRequest(
    IEnumerable<SaveTableRequest> Tables,
    IEnumerable<SaveLandmarkRequest> Landmarks
);
