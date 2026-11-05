using SensorWidget.Core;

namespace SensorWidget.Tests;

public class ReadingBuilderTests
{
    private static RawSensor CpuTemp(string name, float? value) =>
        new(HardwareKind.Cpu, SensorKind.Temperature, name, value);

    private static RawSensor GpuTemp(string name, float? value) =>
        new(HardwareKind.Gpu, SensorKind.Temperature, name, value);

    private static RawSensor GpuFan(string name, float? value) =>
        new(HardwareKind.Gpu, SensorKind.Fan, name, value);

    private static RawSensor BoardTemp(string name, float? value) =>
        new(HardwareKind.Motherboard, SensorKind.Temperature, name, value);

    private static RawSensor BoardFan(string name, float? value) =>
        new(HardwareKind.Motherboard, SensorKind.Fan, name, value);

    private static Reading Row(IEnumerable<Reading> readings, string label) =>
        Assert.Single(readings, reading => reading.Label == label);

    // FR1.1 to FR1.5: every group is shown, labelled, in °C and RPM.
    [Fact]
    public void Build_ShowsCpuGpuAndEveryMotherboardSensor()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            CpuTemp("Core (Tctl/Tdie)", 45.5f),
            GpuTemp("GPU Core", 52f),
            GpuFan("GPU Fan", 1200f),
            BoardTemp("System", 38f),
            BoardTemp("Chipset", 55f),
            BoardFan("CPU Fan", 900f),
            BoardFan("System Fan #1", 750f),
        });

        Assert.Equal("45.5 °C", Row(readings, "CPU Temperature").DisplayValue);
        Assert.Equal("52.0 °C", Row(readings, "GPU Temperature").DisplayValue);
        Assert.DoesNotContain(readings, reading => reading.Label == "GPU Fan");
        Assert.Equal("38.0 °C", Row(readings, "System").DisplayValue);
        Assert.Equal("55.0 °C", Row(readings, "Chipset").DisplayValue);
        Assert.Equal("900 RPM", Row(readings, "CPU Fan").DisplayValue);
        Assert.Equal("750 RPM", Row(readings, "System Fan #1").DisplayValue);
        Assert.Equal(6, readings.Count);
    }

    [Fact]
    public void Build_PrefersTheNamedCpuAndGpuSensors()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            CpuTemp("CCD1 (Tdie)", 70f),
            CpuTemp("Core (Tctl/Tdie)", 50f),
            GpuTemp("GPU Hot Spot", 80f),
            GpuTemp("GPU Core", 60f),
        });

        Assert.Equal("50.0 °C", Row(readings, "CPU Temperature").DisplayValue);
        Assert.Equal("60.0 °C", Row(readings, "GPU Temperature").DisplayValue);
    }

    [Fact]
    public void Build_FallsBackToAnotherSensorWhenThePreferredOneHasNoValue()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            CpuTemp("Core (Tctl/Tdie)", null),
            CpuTemp("CCD1 (Tdie)", 70f),
        });

        Assert.Equal("70.0 °C", Row(readings, "CPU Temperature").DisplayValue);
    }

    // FR4.1, FR4.3: too hot exactly at the limit, not 0.1 °C below it.
    [Theory]
    [InlineData(HardwareKind.Cpu, "CPU Temperature", 85f, true)]
    [InlineData(HardwareKind.Cpu, "CPU Temperature", 84.9f, false)]
    [InlineData(HardwareKind.Gpu, "GPU Temperature", 90f, true)]
    [InlineData(HardwareKind.Gpu, "GPU Temperature", 89.9f, false)]
    [InlineData(HardwareKind.Motherboard, "System", 70f, true)]
    [InlineData(HardwareKind.Motherboard, "System", 69.9f, false)]
    public void Build_FlagsTemperatureAtOrAboveItsLimit(HardwareKind hardware, string label, float value, bool tooHot)
    {
        var sensorName = hardware == HardwareKind.Motherboard ? label : "Any sensor";

        var readings = ReadingBuilder.Build(new[]
        {
            new RawSensor(hardware, SensorKind.Temperature, sensorName, value),
        });

        Assert.Equal(tooHot, Row(readings, label).IsTooHot);
    }

    // FR4: fan speeds are never highlighted, however high.
    [Fact]
    public void Build_NeverFlagsFans()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            BoardFan("Fan #2", 5000f),
            BoardFan("Fan #3", 5000f),
        });

        Assert.Equal("5000 RPM", Row(readings, "CPU Fan").DisplayValue);
        Assert.Equal("5000 RPM", Row(readings, "Fan #3").DisplayValue);
        Assert.All(readings, reading => Assert.False(reading.IsTooHot));
    }

    // FR5.1: a sensor with no value is N/A while the others keep showing values.
    [Fact]
    public void Build_ShowsNotAvailableForMissingValueAndKeepsOtherRows()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            CpuTemp("Core (Tctl/Tdie)", null),
            GpuTemp("GPU Core", 60f),
            BoardFan("Fan #3", float.NaN),
            BoardTemp("System", null),
            BoardFan("CPU Fan", 900f),
        });

        Assert.Equal("N/A", Row(readings, "CPU Temperature").DisplayValue);
        Assert.Equal("N/A", Row(readings, "Fan #3").DisplayValue);
        Assert.Equal("N/A", Row(readings, "System").DisplayValue);
        Assert.False(Row(readings, "CPU Temperature").IsTooHot);
        Assert.Equal("60.0 °C", Row(readings, "GPU Temperature").DisplayValue);
        Assert.Equal("900 RPM", Row(readings, "CPU Fan").DisplayValue);
    }

    [Fact]
    public void Build_StillShowsTheCpuAndGpuRowsWhenThereAreNoSensors()
    {
        var readings = ReadingBuilder.Build(Array.Empty<RawSensor>());

        Assert.Equal(new[] { "CPU Temperature", "GPU Temperature" }, readings.Select(r => r.Label));
        Assert.All(readings, reading => Assert.Equal("N/A", reading.DisplayValue));
    }

    // FR1.4, FR1.5: the board's generic names become what each sensor measures.
    [Theory]
    [InlineData(SensorKind.Temperature, "Temperature #2", 38f, "Motherboard", "38.0 °C")]
    [InlineData(SensorKind.Temperature, "Temperature #3", 44f, "VRM MOSFETs", "44.0 °C")]
    [InlineData(SensorKind.Temperature, "Temperature #5", 29f, "AUXTIN2", "29.0 °C")]
    [InlineData(SensorKind.Fan, "Fan #2", 900f, "CPU Fan", "900 RPM")]
    public void Build_ShowsTheRealNameOfEachMappedSensor(
        SensorKind kind, string libraryName, float value, string label, string displayValue)
    {
        var readings = ReadingBuilder.Build(new[] { new RawSensor(HardwareKind.Motherboard, kind, libraryName, value) });

        Assert.Equal(displayValue, Row(readings, label).DisplayValue);
        Assert.DoesNotContain(readings, reading => reading.Label == libraryName);
    }

    [Fact]
    public void Build_HidesTheUnusedMotherboardTemperature()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            BoardTemp("Temperature #1", 25f),
            BoardTemp("Temperature #2", 38f),
        });

        Assert.DoesNotContain(readings, reading => reading.Label == "Temperature #1");
        Assert.Equal("38.0 °C", Row(readings, "Motherboard").DisplayValue);
    }

    [Fact]
    public void Build_HidesAnIdleMotherboardFanThenShowsItAgainWhenItSpins()
    {
        var idle = ReadingBuilder.Build(new[] { BoardFan("Fan #3", 0f) });
        var spinning = ReadingBuilder.Build(new[] { BoardFan("Fan #3", 650f) });

        Assert.DoesNotContain(idle, reading => reading.Label == "Fan #3");
        Assert.Equal("650 RPM", Row(spinning, "Fan #3").DisplayValue);
    }

    // A value below 1 RPM rounds to "0 RPM" but is not exactly zero, so it still shows.
    [Fact]
    public void Build_OnlyHidesAFanThatReadsExactlyZero()
    {
        var readings = ReadingBuilder.Build(new[] { BoardFan("Fan #3", 0.4f) });

        Assert.Equal("0 RPM", Row(readings, "Fan #3").DisplayValue);
    }

    // A stopped CPU fan is the one fan that must not disappear.
    [Fact]
    public void Build_KeepsTheCpuFanVisibleAtZeroRpm()
    {
        var readings = ReadingBuilder.Build(new[] { BoardFan("Fan #2", 0f) });

        Assert.Equal("0 RPM", Row(readings, "CPU Fan").DisplayValue);
    }

    // FR5.1: no value is not the same as 0 RPM, so the row stays and shows N/A.
    [Theory]
    [InlineData(null)]
    [InlineData(float.NaN)]
    public void Build_KeepsAMotherboardFanWithNoValueAsNotAvailable(float? value)
    {
        var readings = ReadingBuilder.Build(new[] { BoardFan("Fan #3", value) });

        Assert.Equal("N/A", Row(readings, "Fan #3").DisplayValue);
    }

    // FR1.3 by the person's decision: the system has no GPU fan, so the GPU group never shows one,
    // and a GPU sensor that shares a motherboard fan's library name does not become the CPU fan.
    [Theory]
    [InlineData("GPU Fan", 1200f)]
    [InlineData("Fan #2", 1200f)]
    [InlineData("Fan #2", 0f)]
    [InlineData("GPU Fan", null)]
    [InlineData("GPU Fan", float.NaN)]
    public void Build_ShowsNoGpuFanRowWhateverTheGpuReports(string sensorName, float? value)
    {
        var readings = ReadingBuilder.Build(new[] { GpuTemp("GPU Core", 60f), GpuFan(sensorName, value) });

        var gpuGroup = Assert.Single(ReadingBuilder.Group(readings), group => group.Name == "GPU");
        Assert.Equal(new[] { "GPU Temperature" }, gpuGroup.Rows.Select(row => row.Label));
        Assert.Equal(new[] { "CPU Temperature", "GPU Temperature" }, readings.Select(reading => reading.Label));
    }

    // FR1.5: the CPU fan is grouped with the CPU, straight after its temperature.
    [Fact]
    public void Group_ShowsTheCpuFanInTheCpuGroupAndNotTheMotherboardGroup()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            BoardFan("Fan #2", 900f),
            CpuTemp("Core (Tctl/Tdie)", 45f),
            BoardTemp("Temperature #2", 38f),
            BoardFan("Fan #3", 650f),
        });

        var groups = ReadingBuilder.Group(readings);

        var cpuRows = Assert.Single(groups, group => group.Name == "CPU").Rows;
        Assert.Equal(new[] { "CPU Temperature", "CPU Fan" }, cpuRows.Select(row => row.Label));
        Assert.Equal("900 RPM", cpuRows[1].DisplayValue);

        var boardRows = Assert.Single(groups, group => group.Name == "Motherboard").Rows;
        Assert.Equal(new[] { "Motherboard", "Fan #3" }, boardRows.Select(row => row.Label));
    }

    // The label decides the placement, so a library that already says "CPU Fan" is treated the same.
    [Fact]
    public void Group_MovesASensorTheLibraryAlreadyCallsCpuFan()
    {
        var groups = ReadingBuilder.Group(ReadingBuilder.Build(new[] { BoardFan("CPU Fan", 900f) }));

        Assert.Contains(Assert.Single(groups, group => group.Name == "CPU").Rows, row => row.Label == "CPU Fan");
        Assert.DoesNotContain(groups, group => group.Name == "Motherboard");
    }

    // A stopped CPU fan must stay visible, and in its new place.
    [Theory]
    [InlineData(0f, "0 RPM")]
    [InlineData(null, "N/A")]
    public void Group_KeepsAStoppedOrUnreadableCpuFanInTheCpuGroup(float? value, string displayValue)
    {
        var groups = ReadingBuilder.Group(ReadingBuilder.Build(new[] { BoardFan("Fan #2", value) }));

        var cpuRows = Assert.Single(groups, group => group.Name == "CPU").Rows;
        Assert.Equal(displayValue, Assert.Single(cpuRows, row => row.Label == "CPU Fan").DisplayValue);
    }

    // Only the CPU fan moved: the heading stays, with its other rows and hiding rules.
    [Fact]
    public void Group_KeepsTheMotherboardHeadingAndItsOtherRows()
    {
        var groups = ReadingBuilder.Group(ReadingBuilder.Build(new[]
        {
            BoardTemp("Temperature #1", 25f),
            BoardTemp("Temperature #2", 38f),
            BoardTemp("Temperature #3", 44f),
            BoardTemp("Temperature #5", 29f),
            BoardFan("Fan #2", 900f),
            BoardFan("Fan #3", 0f),
            BoardFan("Fan #6", 800f),
        }));

        var boardRows = Assert.Single(groups, group => group.Name == "Motherboard").Rows;
        Assert.Equal(new[] { "Motherboard", "VRM MOSFETs", "AUXTIN2", "Fan #6" }, boardRows.Select(row => row.Label));
    }

    // A value in the CPU group is still a fan speed, so the CPU temperature limit never applies to it.
    [Fact]
    public void Build_DoesNotFlagTheCpuFanAboveTheCpuTemperatureLimit()
    {
        var readings = ReadingBuilder.Build(new[] { BoardFan("Fan #2", TemperatureLimits.Cpu + 1000f) });

        var cpuFan = Row(readings, "CPU Fan");
        Assert.Equal(HardwareKind.Cpu, cpuFan.Hardware);
        Assert.False(cpuFan.IsTooHot);
    }

    [Fact]
    public void Build_KeepsTheLibraryNameOfAnUnmappedSensor()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            BoardTemp("Temperature #4", 33f),
            BoardFan("Fan #6", 800f),
        });

        Assert.Equal("33.0 °C", Row(readings, "Temperature #4").DisplayValue);
        Assert.Equal("800 RPM", Row(readings, "Fan #6").DisplayValue);
    }

    // Mapping and hiding match the library's names exactly, so a near match is left alone.
    [Fact]
    public void Build_DoesNotTreatANearMatchAsAMappedOrHiddenSensor()
    {
        var readings = ReadingBuilder.Build(new[]
        {
            BoardTemp("temperature #1", 25f),
            BoardTemp("temperature #2", 38f),
        });

        Assert.Equal("25.0 °C", Row(readings, "temperature #1").DisplayValue);
        Assert.Equal("38.0 °C", Row(readings, "temperature #2").DisplayValue);
    }

    // FR4.3: a new name does not change the motherboard limit.
    [Theory]
    [InlineData("Temperature #2", "Motherboard")]
    [InlineData("Temperature #3", "VRM MOSFETs")]
    [InlineData("Temperature #5", "AUXTIN2")]
    public void Build_KeepsTheMotherboardLimitOnRenamedRows(string libraryName, string label)
    {
        var atLimit = ReadingBuilder.Build(new[] { BoardTemp(libraryName, 70f) });
        var belowLimit = ReadingBuilder.Build(new[] { BoardTemp(libraryName, 69.9f) });

        Assert.True(Row(atLimit, label).IsTooHot);
        Assert.False(Row(belowLimit, label).IsTooHot);
    }

    [Fact]
    public void Group_KeepsCpuGpuMotherboardOrder()
    {
        var readings = ReadingBuilder.Build(new[] { BoardTemp("System", 38f) });

        var groups = ReadingBuilder.Group(readings);

        Assert.Equal(new[] { "CPU", "GPU", "Motherboard" }, groups.Select(group => group.Name));
        Assert.Equal(new[] { "GPU Temperature" }, groups[1].Rows.Select(row => row.Label));
    }
}
