namespace SensorWidget.Core;

/// <summary>One row of the widget, ready to display.</summary>
public sealed record Reading(HardwareKind Hardware, string Label, string DisplayValue, bool IsTooHot);

/// <summary>The rows shown together under one heading.</summary>
public sealed record ReadingGroup(string Name, IReadOnlyList<Reading> Rows);
