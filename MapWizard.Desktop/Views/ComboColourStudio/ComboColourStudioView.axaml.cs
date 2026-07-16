using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Views.ComboColourStudio;

public partial class ComboColourStudioView : UserControl
{
    private const double DragStartThreshold = 4;

    private sealed record DragCandidate(
        PointerPressedEventArgs TriggerEvent,
        Point StartPosition,
        Border Host,
        AvaloniaComboColourToken Token,
        AvaloniaComboColourPoint Owner);

    private DragCandidate? _dragCandidate;
    private Border? _activeDropHost;
    private bool _dragInProgress;

    public ComboColourStudioView()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerPressedEvent, OnPointerPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(InputElement.PointerMovedEvent, OnPointerMovedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(InputElement.PointerReleasedEvent, OnPointerReleasedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (_dragInProgress)
        {
            return;
        }

        if (!TryGetTokenContextFromSource(e.Source, out var host, out var token, out var point) ||
            !e.GetCurrentPoint(host).Properties.IsLeftButtonPressed)
        {
            ResetDragState();
            return;
        }

        _dragCandidate = new DragCandidate(
            e,
            e.GetPosition(host),
            host,
            token,
            point);
    }

    private async void OnPointerMovedTunnel(object? sender, PointerEventArgs e)
    {
        var candidate = _dragCandidate;
        if (_dragInProgress || candidate is null)
        {
            return;
        }

        var current = e.GetCurrentPoint(this);
        if (!current.Properties.IsLeftButtonPressed)
        {
            ResetDragState();
            return;
        }

        var currentPosition = e.GetPosition(candidate.Host);
        var dragDistance = Math.Abs(currentPosition.X - candidate.StartPosition.X) +
                           Math.Abs(currentPosition.Y - candidate.StartPosition.Y);
        if (dragDistance < DragStartThreshold)
        {
            return;
        }

        var dragTransfer = new DataTransfer();
        dragTransfer.Add(DataTransferItem.CreateText("MapWizardComboColourTokenMove"));

        _dragInProgress = true;
        candidate.Host.Classes.Set("drag-source", true);
        try
        {
            e.Handled = true;
            await DragDrop.DoDragDropAsync(candidate.TriggerEvent, dragTransfer, DragDropEffects.Move);
        }
        finally
        {
            candidate.Host.Classes.Set("drag-source", false);
            _dragInProgress = false;
            ClearActiveDropHost();
            ResetDragState();
        }
    }

    private void OnPointerReleasedTunnel(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragInProgress)
        {
            return;
        }

        ResetDragState();
    }

    private void SequenceToken_OnDragOver(object? sender, DragEventArgs e)
    {
        UpdateDropVisualState(sender, e);
    }

    private void SequenceToken_OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is not Border host)
        {
            return;
        }

        ClearActiveDropHost();

        if (!TryGetDropContext(host, out var sourcePoint, out var sourceToken, out var targetToken))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var insertAfterTarget = e.GetPosition(host).X >= host.Bounds.Width / 2.0;
        MoveTokenWithinPoint(sourcePoint, sourceToken, targetToken, insertAfterTarget);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private static void MoveTokenWithinPoint(
        AvaloniaComboColourPoint point,
        AvaloniaComboColourToken sourceToken,
        AvaloniaComboColourToken targetToken,
        bool insertAfterTarget)
    {
        var sequence = point.ColourSequence;
        var sourceIndex = sequence.IndexOf(sourceToken);
        var targetIndex = sequence.IndexOf(targetToken);

        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        var destinationIndex = targetIndex + (insertAfterTarget ? 1 : 0);
        if (sourceIndex < destinationIndex)
        {
            destinationIndex--;
        }

        destinationIndex = Math.Clamp(destinationIndex, 0, sequence.Count - 1);
        if (destinationIndex == sourceIndex)
        {
            return;
        }

        sequence.Move(sourceIndex, destinationIndex);
    }

    private void UpdateDropVisualState(object? sender, DragEventArgs e)
    {
        if (sender is not Border host)
        {
            return;
        }

        if (!ReferenceEquals(_activeDropHost, host))
        {
            ClearActiveDropHost();
            _activeDropHost = host;
        }

        var canDrop = TryGetDropContext(host, out _, out _, out _);
        host.Classes.Set("drag-over", canDrop);
        e.DragEffects = canDrop ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private bool TryGetDropContext(
        Border host,
        out AvaloniaComboColourPoint sourcePoint,
        out AvaloniaComboColourToken sourceToken,
        out AvaloniaComboColourToken targetToken)
    {
        sourcePoint = null!;
        sourceToken = null!;
        targetToken = null!;

        if (host.DataContext is not AvaloniaComboColourToken target)
        {
            return false;
        }

        var candidate = _dragCandidate;
        var targetPoint = FindOwningColourPoint(host);
        if (targetPoint is null || candidate is null)
        {
            return false;
        }

        if (!ReferenceEquals(targetPoint, candidate.Owner) ||
            ReferenceEquals(target, candidate.Token))
        {
            return false;
        }

        sourcePoint = candidate.Owner;
        sourceToken = candidate.Token;
        targetToken = target;
        return true;
    }

    private static AvaloniaComboColourPoint? FindOwningColourPoint(Visual host)
    {
        foreach (var element in host.GetSelfAndVisualAncestors().OfType<StyledElement>())
        {
            if (element.DataContext is AvaloniaComboColourPoint point)
            {
                return point;
            }
        }

        return null;
    }

    private static bool TryGetTokenContextFromSource(
        object? source,
        out Border host,
        out AvaloniaComboColourToken token,
        out AvaloniaComboColourPoint point)
    {
        host = null!;
        token = null!;
        point = null!;

        if (source is not Visual visual)
        {
            return false;
        }

        foreach (var element in visual.GetSelfAndVisualAncestors())
        {
            if (element is Border border &&
                border.Classes.Contains("SequenceTokenDragHost") &&
                border.DataContext is AvaloniaComboColourToken borderToken)
            {
                host = border;
                token = borderToken;
                break;
            }
        }

        if (host is null)
        {
            return false;
        }

        var ownerPoint = FindOwningColourPoint(host);
        if (ownerPoint is null)
        {
            return false;
        }

        point = ownerPoint;
        return true;
    }

    private void ResetDragState()
    {
        _dragCandidate = null;
    }

    private void ClearActiveDropHost()
    {
        if (_activeDropHost is null)
        {
            return;
        }

        _activeDropHost.Classes.Set("drag-over", false);
        _activeDropHost = null;
    }

}
