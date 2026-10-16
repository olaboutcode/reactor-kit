using Reactor.Kit.SheetTokens;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// Turns style, detents, and measurements into a card rectangle.
/// Lengths are device-independent units. The function does not read pixels or a platform.
/// </summary>
static class SheetGeometry
{
    public const double ReadableWidth = 672;
    public const double CompactWidthDip = 600;
    public const double CompactHeightDip = 480;
    public const double DimOpacity = 0.30;
    const double SideGap = 20;

    public static SheetLayout Layout(SheetProps props, SheetMetrics metrics, string? preferId = null)
    {
        var large = Large(metrics);
        var rests = Rests(props, metrics, large);
        var selected = Select(props, rests, large, preferId, out var raiseInactive);
        var frame = Frame(props, metrics, rests, selected, large);
        return new SheetLayout
        {
            Frame = frame,
            Rests = rests,
            RaiseInactive = raiseInactive,
            Large = large,
        };
    }

    public static double Large(SheetMetrics metrics)
    {
        var reserve = Math.Max(metrics.Safe.Top, 10);
        return Math.Max(0, metrics.Height - reserve);
    }

    public static double Target(SheetDetent detent, double large)
        => detent.Kind switch
        {
            SheetDetentKind.Medium => large / 2,
            SheetDetentKind.Large => large,
            SheetDetentKind.Fraction => Unit(detent.Value) * large,
            SheetDetentKind.Height => Math.Clamp(NonNegative(detent.Value), 0, large),
            _ => large,
        };

    /// <summary>
    /// iOS 15 has no custom detent. Each page detent maps to medium or large.
    /// A tie goes to large. The half-large stand-in can miss UIKit by a few points.
    /// </summary>
    public static string LegacyId(SheetDetent detent, double large)
    {
        if (detent.Kind is SheetDetentKind.Medium)
            return SheetIds.Medium;

        if (detent.Kind is SheetDetentKind.Large)
            return SheetIds.Large;

        var target = Target(detent, large);
        var medium = large / 2;
        var toMedium = Math.Abs(target - medium);
        var toLarge = Math.Abs(target - large);
        return toMedium < toLarge ? SheetIds.Medium : SheetIds.Large;
    }

    public static string? Snap(
        IReadOnlyList<SheetRest> rests,
        double height,
        double velocityY,
        bool dismissDisabled,
        out bool dismiss)
    {
        dismiss = false;
        if (rests.Count == 0)
            return null;

        var smallest = rests[0].Height;
        var belowHalf = height < smallest / 2;
        var flung = velocityY > 1000 && height < smallest;
        if (!dismissDisabled && (belowHalf || flung))
        {
            dismiss = true;
            return null;
        }

        var projected = height + (-velocityY * 0.1);
        var best = rests[0];
        var bestDiff = Math.Abs(best.Height - projected);
        for (var i = 1; i < rests.Count; i++)
        {
            var rest = rests[i];
            var diff = Math.Abs(rest.Height - projected);
            if (diff < bestDiff - 0.5)
            {
                best = rest;
                bestDiff = diff;
                continue;
            }

            if (Math.Abs(diff - bestDiff) > 0.5)
                continue;

            var upward = velocityY < -1;
            var downward = velocityY > 1;
            if (upward && rest.Height > best.Height)
                best = rest;
            else if (downward && rest.Height < best.Height)
                best = rest;
            else if (!upward && !downward && rest.Height > best.Height)
                best = rest;
        }

        return best.Id;
    }

    static List<SheetRest> Rests(SheetProps props, SheetMetrics metrics, double large)
    {
        if (props.Style == SheetStyle.Form && !metrics.CompactWidth)
            return [];

        var source = props.Detents.Count == 0
            ? [SheetDetent.Large]
            : props.Detents;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<SheetRest>();
        foreach (var detent in source)
        {
            if (detent is null)
                continue;

            if (!seen.Add(detent.Id))
                DuplicateDetent(detent);

            if (detent.Kind == SheetDetentKind.Medium && metrics.CompactHeight)
                continue;

            list.Add(new SheetRest(detent.Id, Target(detent, large)));
        }

        if (list.Count == 0)
            list.Add(new SheetRest(SheetIds.Large, large));

        list.Sort(static (left, right) => left.Height.CompareTo(right.Height));
        return list;
    }

    static void DuplicateDetent(SheetDetent detent)
        => throw new ArgumentException($"The detent id '{detent.Id}' is already in the list.", nameof(detent));

    static SheetRest? Select(SheetProps props, List<SheetRest> rests, double large, string? preferId, out bool raiseInactive)
    {
        raiseInactive = false;
        if (rests.Count == 0)
            return null;

        if (preferId != null)
        {
            foreach (var rest in rests)
            {
                if (string.Equals(rest.Id, preferId, StringComparison.Ordinal))
                    return rest;
            }
        }

        var requested = props.SelectedDetent;
        if (requested != null)
        {
            foreach (var rest in rests)
            {
                if (string.Equals(rest.Id, requested, StringComparison.Ordinal))
                    return rest;
            }
        }

        var stored = Find(props.Detents, requested);
        if (stored != null)
        {
            raiseInactive = true;
            var target = Target(stored, large);
            SheetRest? next = null;
            foreach (var rest in rests)
            {
                if (rest.Height + 0.5 < target)
                    continue;

                next = rest;
                break;
            }

            return next ?? rests[^1];
        }

        return rests[^1];
    }

    static SheetFrame Frame(
        SheetProps props,
        SheetMetrics metrics,
        List<SheetRest> rests,
        SheetRest? selected,
        double large)
    {
        var radius = props.CornerRadius ?? 10;
        var grabber = props.Grabber;
        if (props.Style == SheetStyle.Form && !metrics.CompactWidth)
            return Centered(props, metrics, radius, grabber);

        var height = selected?.Height ?? large;
        var full = !props.EdgeAttachedInCompactHeight && metrics.CompactHeight;
        Rect card;
        if (full)
        {
            card = new Rect(0, 0, Math.Max(0, metrics.Width), Math.Max(0, metrics.Height));
            radius = 0;
        }
        else
        {
            var width = PageWidth(props, metrics, out var x);
            var top = Math.Max(0, metrics.Height - height);
            card = new Rect(x, top, width, height);
        }

        card = Keyboard(props, metrics, card, full, centered: false);
        var (opacity, hits) = Dim(props, rests, selected, height);
        return new SheetFrame
        {
            Card = card,
            DimOpacity = opacity,
            DimHits = hits,
            CornerRadius = radius,
            Centered = false,
            FullScreen = full,
            Grabber = grabber,
            ActiveId = selected?.Id,
            RestHeight = height,
        };
    }

    static SheetFrame Centered(SheetProps props, SheetMetrics metrics, double radius, bool grabber)
    {
        var maxW = Math.Max(0, metrics.Width - metrics.Safe.Left - metrics.Safe.Right - (SideGap * 2));
        var maxH = Math.Max(0, metrics.Height - metrics.Safe.Top - metrics.Safe.Bottom - (SideGap * 2));
        var cardW = Math.Min(Math.Max(0, props.PreferredWidth), maxW);
        var cardH = Math.Min(Math.Max(0, props.PreferredHeight), maxH);
        var card = new Rect(
            (metrics.Width - cardW) / 2,
            (metrics.Height - cardH) / 2,
            cardW,
            cardH);
        card = Keyboard(props, metrics, card, full: false, centered: true);
        return new SheetFrame
        {
            Card = card,
            DimOpacity = DimOpacity,
            DimHits = true,
            CornerRadius = radius,
            Centered = true,
            FullScreen = false,
            Grabber = grabber,
            ActiveId = null,
            RestHeight = cardH,
        };
    }

    static Rect Keyboard(SheetProps props, SheetMetrics metrics, Rect card, bool full, bool centered)
    {
        var overlap = Math.Max(0, metrics.KeyboardOverlap);
        if (overlap <= 0 || full)
            return card;

        var reserve = Math.Max(metrics.Safe.Top, 10);
        var keyboardTop = metrics.Height - overlap;
        if (!centered)
        {
            var edgeTop = Math.Max(card.Y, reserve);
            var bottom = Math.Max(edgeTop, keyboardTop);
            return new Rect(card.X, edgeTop, card.Width, bottom - edgeTop);
        }

        var shifted = new Rect(card.X, card.Y - overlap, card.Width, card.Height);
        if (metrics.FocusedBottom is not double focus)
            return ClampTop(shifted, reserve);

        var cleared = focus - overlap;
        var limit = keyboardTop - 8;
        if (cleared <= limit)
            return ClampTop(shifted, reserve);

        var shrink = cleared - limit;
        var height = Math.Max(0, shifted.Height - shrink);
        var top = Math.Max(reserve, shifted.Bottom - height);
        return new Rect(shifted.X, top, shifted.Width, Math.Max(0, shifted.Bottom - top));
    }

    static Rect ClampTop(Rect card, double reserve)
    {
        if (card.Y >= reserve)
            return card;

        var bottom = card.Bottom;
        var top = reserve;
        return new Rect(card.X, top, card.Width, Math.Max(0, bottom - top));
    }

    static (double Opacity, bool Hits) Dim(
        SheetProps props,
        List<SheetRest> rests,
        SheetRest? selected,
        double height)
    {
        if (props.UndimmedDetent is null || rests.Count == 0 || selected is null)
            return (DimOpacity, true);

        double? undimmed = null;
        foreach (var rest in rests)
        {
            if (!string.Equals(rest.Id, props.UndimmedDetent, StringComparison.Ordinal))
                continue;

            undimmed = rest.Height;
            break;
        }

        if (undimmed is not double gate)
            return (DimOpacity, true);

        if (height <= gate + 0.5)
            return (0, false);

        double? next = null;
        foreach (var rest in rests)
        {
            if (rest.Height <= gate + 0.5)
                continue;

            next = rest.Height;
            break;
        }

        if (next is not double taller || height >= taller)
            return (DimOpacity, true);

        var span = taller - gate;
        var opacity = span <= 0 ? DimOpacity : DimOpacity * ((height - gate) / span);
        return (opacity, true);
    }

    static double PageWidth(SheetProps props, SheetMetrics metrics, out double x)
    {
        var safe = Math.Max(0, metrics.Width - metrics.Safe.Left - metrics.Safe.Right);
        if (metrics.CompactWidth)
        {
            x = metrics.Safe.Left;
            return safe;
        }

        double width;
        if (props.WidthFollowsPreferredSize)
            width = Math.Min(safe, Math.Max(0, props.PreferredWidth));
        else if (props.PageSizing)
            width = Math.Min(safe, ReadableWidth);
        else
            width = safe;

        x = metrics.Safe.Left + ((safe - width) / 2);
        return width;
    }

    static SheetDetent? Find(IReadOnlyList<SheetDetent> detents, string? id)
    {
        if (id is null)
            return null;

        foreach (var detent in detents)
        {
            if (detent != null && string.Equals(detent.Id, id, StringComparison.Ordinal))
                return detent;
        }

        return null;
    }

    static double Unit(double value)
        => Math.Clamp(NonNegative(value), 0, 1);

    static double NonNegative(double value)
        => double.IsFinite(value) && value > 0 ? value : 0;
}
