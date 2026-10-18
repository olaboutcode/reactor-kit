namespace Reactor.Kit.TimelineViewTokens;

/// <summary>
/// The ordered blocks of one <see cref="Reactor.Kit.TimelineView"/>. Each call returns this list.
/// </summary>
public sealed class TimelineSteps
{
    readonly List<Block> _blocks = [];
    Run? _run;

    internal IReadOnlyList<Block> Blocks => _blocks;

    /// <summary>
    /// Appends a step to the current run. A group before a later step starts a new run.
    /// A null action appends a default step.
    /// </summary>
    public TimelineSteps Step(Action<TimelineStep>? configure = null)
    {
        _run ??= Open();
        var step = new TimelineStep();
        configure?.Invoke(step);
        _run.Steps.Add(step);
        return this;
    }

    /// <summary>
    /// Appends a group. The group's steps use their own rail.
    /// A null action appends a group with no header and no steps.
    /// </summary>
    public TimelineSteps Group(Action<TimelineGroup>? configure = null)
    {
        _run = null;
        var group = new TimelineGroup();
        configure?.Invoke(group);
        _blocks.Add(new Grouped(group));
        return this;
    }

    Run Open()
    {
        var run = new Run();
        _blocks.Add(run);
        _run = run;
        return run;
    }

    internal abstract class Block;

    internal sealed class Run : Block
    {
        public List<TimelineStep> Steps { get; } = [];
    }

    internal sealed class Grouped : Block
    {
        public Grouped(TimelineGroup group)
        {
            Group = group;
        }

        public TimelineGroup Group { get; }
    }
}
