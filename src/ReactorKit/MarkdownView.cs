using MauiReactor;
using SKCanvasView = SkiaSharp.Views.Maui.Controls.SKCanvasView;
using NativeMarkdown = Reactor.Kit.Internals.Markdown.MarkdownCanvas;

namespace Reactor.Kit;

/// <summary>
/// Skia markdown for CommonMark and the usual GitHub extensions.
/// A picture link keeps its alt text. The picture file is not loaded.
/// </summary>
public sealed class MarkdownView : View<SKCanvasView>
{
    internal MarkdownView(Action<SKCanvasView?>? componentRefAction = null)
        : base(componentRefAction)
    {
    }

    protected override void OnMount()
    {
        _nativeControl ??= new NativeMarkdown();
        base.OnMount();
    }

    public MarkdownView Markdown(string markdown)
        => this.Set(NativeMarkdown.MarkdownProperty, markdown);

    public MarkdownView Markdown(Func<string> markdown, IComponentWithState? state = null)
        => this.Set(NativeMarkdown.MarkdownProperty, new PropertyValue<string>(markdown, state));

    /// <summary>Reads a UTF-8 file. When the path is set, it replaces <see cref="Markdown(string)"/>.</summary>
    public MarkdownView File(string path)
        => this.Set(NativeMarkdown.FileProperty, path);

    public MarkdownView File(Func<string> path, IComponentWithState? state = null)
        => this.Set(NativeMarkdown.FileProperty, new PropertyValue<string>(path, state));

    public MarkdownView FontSize(double size)
        => this.Set(NativeMarkdown.FontSizeProperty, size);

    public MarkdownView FontSize(Func<double> size, IComponentWithState? state = null)
        => this.Set(NativeMarkdown.FontSizeProperty, new PropertyValue<double>(size, state));

    public MarkdownView FontFamily(string family)
        => this.Set(NativeMarkdown.FontFamilyProperty, family);

    public MarkdownView FontFamily(Func<string> family, IComponentWithState? state = null)
        => this.Set(NativeMarkdown.FontFamilyProperty, new PropertyValue<string>(family, state));

    public MarkdownView TextColor(Color color)
        => this.Set(NativeMarkdown.TextColorProperty, color);

    public MarkdownView TextColor(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeMarkdown.TextColorProperty, new PropertyValue<Color>(color, state));
}
