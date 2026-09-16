using System;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Views.Controls;

namespace MapWizard.Desktop.Services;

public enum ModalPresentation
{
    Dialog,
    MapPickerOverlay
}

public sealed record ModalRequest(
    object? Content,
    string? Title = null,
    object? FooterContent = null,
    bool ShowCloseButton = true,
    bool CloseOnBackdropClick = true,
    bool CloseOnEscape = true,
    ModalPresentation Presentation = ModalPresentation.Dialog);

public interface IModalService
{
    void RegisterHost(ModalHost host);

    bool IsOpen { get; }

    Task<object?> ShowAsync(ModalRequest request, CancellationToken cancellationToken = default);

    Task CloseAsync(object? result = null);

    /// <summary>
    /// Closes the open modal when the user presses Escape, honoring the
    /// current request's <c>CloseOnEscape</c> flag. Returns true when a modal was closed.
    /// </summary>
    Task<bool> CloseOnEscapeAsync();
}
