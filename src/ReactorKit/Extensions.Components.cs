using MauiReactor;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using SKCanvasView = SkiaSharp.Views.Maui.Controls.SKCanvasView;

namespace Reactor.Kit;

/// <summary>
/// Registers ReactorKit handlers and the control factories.
/// </summary>
#pragma warning disable MA0048 // Ignore file name & type mismatch
public static partial class Extensions
#pragma warning restore MA0048
{
    /// <summary>
    /// Adds every child. ContentView keeps a single Content, so the last child is the one that stays.
    /// </summary>
    public static GlazeView GlazeView(params VisualNode?[] children)
        => Create(refAction: null, children);

    /// <summary>
    /// Same children as <see cref="GlazeView(VisualNode?[])"/>.
    /// The callback receives the mounted element, and null when it unmounts.
    /// </summary>
    public static GlazeView GlazeView(Action<MauiContentView?> refAction, params VisualNode?[] children)
        => Create(refAction, children);

    static GlazeView Create(Action<MauiContentView?>? refAction, VisualNode?[]? children)
    {
        var glaze = new GlazeView(refAction);
        if (children is not { Length: > 0 })
            return glaze;
        glaze.AddChildren(children);
        return glaze;
    }

    /// <summary>
    /// Skia text with word and sentence highlights.
    /// </summary>
    public static RichTextView RichTextView()
        => new(componentRefAction: null);

    /// <summary>
    /// Same view as <see cref="RichTextView()"/>, with the text set.
    /// </summary>
    public static RichTextView RichTextView(string text)
        => new RichTextView(componentRefAction: null).Text(text);

    /// <summary>
    /// Same view as <see cref="RichTextView()"/>.
    /// The callback receives the mounted canvas, and null when it unmounts.
    /// </summary>
    public static RichTextView RichTextView(Action<SKCanvasView?> refAction)
        => new(refAction);

    /// <summary>
    /// Same view as <see cref="RichTextView()"/>.
    /// The callback receives the mounted canvas, and null when it unmounts. The text is set.
    /// </summary>
    public static RichTextView RichTextView(Action<SKCanvasView?> refAction, string text)
        => new RichTextView(refAction).Text(text);

    /// <summary>
    /// Skia markdown, including pipe tables and flowchart diagrams.
    /// </summary>
    public static MarkdownView MarkdownView()
        => new(componentRefAction: null);

    /// <summary>
    /// Same view as <see cref="MarkdownView()"/>, with the document set.
    /// </summary>
    public static MarkdownView MarkdownView(string markdown)
        => new MarkdownView(componentRefAction: null).Markdown(markdown);

    /// <summary>
    /// Same view as <see cref="MarkdownView()"/>.
    /// The callback receives the mounted canvas, and null when it unmounts.
    /// </summary>
    public static MarkdownView MarkdownView(Action<SKCanvasView?> refAction)
        => new(refAction);

    /// <summary>
    /// Same view as <see cref="MarkdownView()"/>.
    /// The callback receives the mounted canvas, and null when it unmounts. The document is set.
    /// </summary>
    public static MarkdownView MarkdownView(Action<SKCanvasView?> refAction, string markdown)
        => new MarkdownView(refAction).Markdown(markdown);

    /// <summary>
    /// One rigid mesh. Drag to orbit it.
    /// </summary>
    public static Object3DView Object3DView()
        => new(componentRefAction: null);

    /// <summary>
    /// Same view as <see cref="Object3DView()"/>.
    /// The callback receives the mounted element, and null when it unmounts.
    /// </summary>
    public static Object3DView Object3DView(Action<MauiContentView?> refAction)
        => new(refAction);
}
