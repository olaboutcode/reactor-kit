namespace KitTests.Components;

/// <summary>
/// One timeline for each variation: placement, span, groups, rails, and markers.
/// </summary>
sealed class TimelineTestPage : Component<TimelineTestPage.PageState>
{
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Muted = Color.FromArgb("#8E8E93");
    static readonly Color Ink = Color.FromArgb("#1C1C1E");

    internal sealed class PageState
    {
        public bool Wireframe { get; set; }
    }

    public override VisualNode Render()
        => ContentPage("Timeline",
            Grid("auto,*", "*",
                NavigationBar.Create("Timeline"),
                ScrollView(
                    VStack(
                        Example("Start and end", StartEnd()),
                        Example("Journal", Journal()),
                        Example("Grouped days", GroupedDays()),
                        Example("Spanning block", Spanning()),
                        Example("Plain", Plain()),
                        Example("Labels trailing", Trailing()),
                        Example("Dashed segment and a gap", MixedRail()),
                        Example("Custom marker", CustomMarker()),
                        Example("Group header and a clamped span", Clamped()),
                        Example("Fixed label width", FixedWidth()),
                        Example("Content only", ContentOnly()),
                        Example("Labels only", LabelsOnly()),
                        Example("Rail break", RailBreak()),
                        Example("Type and spacing", TypeAndSpacing()),
                        Example("Right to left", RightToLeft())
                    )
                    .Spacing(28)
                    .Padding(0, 8, 0, 24)
                )
                .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                .GridRow(1)
            )
            .Padding(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    static VStack Example(string title, VisualNode timeline)
        => VStack(
            Label(title).FontSize(13).TextColor(Muted),
            timeline
        ).Spacing(8);

    static TimelineView StartEnd()
        => TimelineView(steps => steps
            .Step(step => step
                .Label("Start")
                .Content("Thu, May 25th at 6:00AM"))
            .Step(step => step
                .Label("End")
                .Content("Thu, May 25th at 7:00AM")
                .MarkerRing()))
            .LabelsInside();

    TimelineView Journal()
        => TimelineView(steps => steps
            .Step(step => step
                .Label(Time("10.30 AM"))
                .Content("Had a great day at work today. Made progress on the project and received positive feedback from the team."))
            .Step(step => step
                .Label(Time("12.30 PM"))
                .Content(Tasks()))
            .Step(step => step
                .Label(Time("13.30 PM"))
                .Content("Lorem ipsum dolor sit amet consectetur. Nunc nulla a blandit nunc ullamcorper tellus morbi nisl.")))
            .DashedLine()
            .StepSpacing(44)
            .NoMarker();

    static TimelineView GroupedDays()
        => TimelineView(steps => steps
            .Group(day => day
                .Header("Wednesday 13")
                .Step(step => step
                    .Label(Hours("8:00", "17:00"))
                    .Content("Assignment Deadline")
                    .MarkerColor(Color.FromArgb("#E35D6A")))
                .Step(step => step
                    .Label(Hours("17:00", "19:30"))
                    .Content("Class Cancelled")
                    .MarkerColor(Color.FromArgb("#2BB673"))))
            .Group(day => day
                .Header("Thursday 14")
                .Step(step => step
                    .Label(Hours("19:00", "21:00"))
                    .Content("Basketball")
                    .MarkerColor(Color.FromArgb("#7B61FF"))
                    .LineColor(Color.FromArgb("#7B61FF"))
                    .LineThickness(4))
                .Step(step => step
                    .Label("22:00")
                    .Content("Dinner")
                    .MarkerColor(Color.FromArgb("#F5C451")))));

    static TimelineView Spanning()
        => TimelineView(steps => steps
            .Step(step => step
                .Label("8:00")
                .Content(Card("Zoom meet with client", "Thursday, from New York"))
                .Span(3))
            .Step(step => step.Label("9:00"))
            .Step(step => step.Label("10:00"))
            .Step(step => step
                .Label("11:00")
                .Content(Card("Explore Design App", "11:00 to 12:00"))));

    static TimelineView Plain()
        => TimelineView(steps => steps
            .Step(step => step.Label("One").Content("Start"))
            .Step(step => step.Content("No label"))
            .Step(step => step.Label("Three").Content("End")));

    static TimelineView Trailing()
        => TimelineView(steps => steps
            .Step(step => step.Label("Start").Content("The content leads"))
            .Step(step => step.Label("Middle").Content("The rail stays between"))
            .Step(step => step.Label("End").Content("The label trails")))
            .LabelsTrailing();

    static TimelineView MixedRail()
        => TimelineView(steps => steps
            .Step(step => step
                .Label("One")
                .Content("Dashed below")
                .DashedLine())
            .Step(step => step
                .Label("Two")
                .Content("Gap below")
                .NoLine())
            .Step(step => step
                .Label("Three")
                .Content("Solid above")));

    static TimelineView CustomMarker()
        => TimelineView(steps => steps
            .Step(step => step
                .Label("Flag")
                .Content("Custom marker")
                .Marker(Flag()))
            .Step(step => step
                .Label("Ring")
                .Content("The timeline default")))
            .MarkerRing();

    static TimelineView Clamped()
        => TimelineView(steps => steps
            .Group(day => day
                .Header(DayHeader())
                .Step(step => step
                    .Label("A")
                    .Content("Span asks for four rows and stops at the group")
                    .Span(4))
                .Step(step => step.Label("B")))
            .Step(step => step
                .Label("Next")
                .Content("Its own rail")));

    static TimelineView FixedWidth()
        => TimelineView(steps => steps
            .Step(step => step
                .Label("A long morning label")
                .Content("The column is 72 points"))
            .Step(step => step
                .Label("Short")
                .Content("The long label wraps")))
            .LabelWidth(72);

    static TimelineView ContentOnly()
        => TimelineView(steps => steps
            .Step(step => step.Content("First"))
            .Step(step => step.Content("Second"))
            .Step(step => step.Content("Third")));

    static TimelineView LabelsOnly()
        => TimelineView(steps => steps
            .Step(step => step.Label("Alpha"))
            .Step(step => step.Label("Beta"))
            .Step(step => step.Label("Gamma")));

    static TimelineView TypeAndSpacing()
        => TimelineView(steps => steps
            .Step(step => step
                .Label("Near")
                .Content("Step spacing is 20"))
            .Step(step => step
                .Label("Near")
                .Content("This content is smaller")
                .ContentFontSize(13))
            .Group(day => day
                .Header("Further")
                .HeaderFontSize(20)
                .Step(step => step
                    .Label("After")
                    .Content("Group spacing is 56"))))
            .LabelFontSize(20)
            .ContentFontSize(16)
            .HeaderFontSize(12)
            .StepSpacing(20)
            .GroupSpacing(56);

    static TimelineView RailBreak()
        => TimelineView(steps => steps
            .Step(step => step.Label("Before").Content("First rail"))
            .Group(day => day
                .Step(step => step.Label("Inside").Content("The rail starts again"))
                .Step(step => step.Label("Still").Content("Same group"))));

    static MauiReactor.Grid RightToLeft()
        => Grid(Plain())
            .FlowDirection(FlowDirection.RightToLeft);

    VStack Tasks()
        => VStack(
            CheckboxView()
                .Label("Finish the wireframe")
                .IsChecked(State.Wireframe)
                .OnChanged(on => SetState(s => s.Wireframe = on)),
            Label("Email Jonelle about cashback"),
            Label("Go and check mail box")
        ).Spacing(16);

    static MauiReactor.Border Time(string text)
        => Border(
            Label(text).FontSize(12).TextColor(Ink)
        )
        .Padding(10, 4)
        .BackgroundColor(Color.FromArgb("#F2F2F7"))
        .Stroke(Colors.Transparent)
        .StrokeCornerRadius(12);

    static VStack Hours(string start, string end)
        => VStack(
            Label(start).FontSize(15),
            Label(end).FontSize(15).TextColor(Muted)
        ).Spacing(0);

    static MauiReactor.Border Card(string title, string detail)
        => Border(
            VStack(
                Label(title).FontSize(16),
                Label(detail).FontSize(13).TextColor(Muted)
            ).Spacing(4)
        )
        .Padding(14)
        .BackgroundColor(Color.FromArgb("#FFF6E8"))
        .Stroke(Colors.Transparent)
        .StrokeCornerRadius(16)
        .VStart();

    static MauiReactor.Border Flag()
        => Border()
            .WidthRequest(14)
            .HeightRequest(14)
            .BackgroundColor(Color.FromArgb("#E35D6A"))
            .Stroke(Colors.Transparent)
            .StrokeCornerRadius(3);

    static HStack DayHeader()
        => HStack(
            Label("Friday").FontSize(13).FontAttributes(FontAttributes.Bold),
            Label("2").FontSize(13).TextColor(Muted)
        ).Spacing(8);
}
