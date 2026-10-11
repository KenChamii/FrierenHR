namespace FrierenHR.Application.Common.DTOs;

/// <param name="Key">Stable identifier, so the client can style a step without matching on its title.</param>
/// <param name="Route">Where the step is done, used to point at it.</param>
/// <param name="Why">
/// What goes wrong if the step is skipped. Setup order is load-bearing here and the
/// consequences are not obvious, so a list of tasks alone gets ignored.
/// </param>
/// <param name="Optional">Not needed for the application to behave correctly.</param>
public record SetupStepDto(
    string Key, string Title, string Why, string Route, bool Done, bool Optional);

/// <param name="NextRoute">The first unfinished required step, or null when there is none.</param>
public record SetupProgressDto(
    int Completed, int Required, bool IsComplete, string? NextRoute, List<SetupStepDto> Steps);
