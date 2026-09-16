using System;
using System.Threading;
using System.Threading.Tasks;
using MapWizard.Desktop.Views.Controls;

namespace MapWizard.Desktop.Services;

public sealed class ModalService : IModalService
{
    private ModalHost? _host;
    private TaskCompletionSource<object?>? _completion;
    private ModalRequest? _currentRequest;

    public bool IsOpen => _host?.IsOpen == true;

    public void RegisterHost(ModalHost host)
    {
        if (_host is not null)
        {
            _host.CloseRequested -= OnHostCloseRequested;
        }

        _host = host;
        _host.CloseRequested += OnHostCloseRequested;
    }

    public async Task<object?> ShowAsync(ModalRequest request, CancellationToken cancellationToken = default)
    {
        if (_host is null)
        {
            throw new InvalidOperationException("No modal host is registered.");
        }

        if (_host.IsOpen)
        {
            throw new InvalidOperationException("Another modal is already open.");
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completion = completion;
        _currentRequest = request;

        _host.Title = request.Title;
        _host.DialogContent = request.Content;
        _host.FooterContent = request.FooterContent;
        _host.ShowCloseButton = request.ShowCloseButton;
        _host.CloseOnBackdropClick = request.CloseOnBackdropClick;
        _host.CloseOnEscape = request.CloseOnEscape;
        _host.Presentation = request.Presentation;

        using var registration = cancellationToken.Register(() => _ = CloseAsync(null));

        try
        {
            await _host.OpenAsync(cancellationToken);
            return await completion.Task;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            if (ReferenceEquals(_completion, completion))
            {
                _completion = null;
                _currentRequest = null;
            }
        }
    }

    public async Task CloseAsync(object? result = null)
    {
        var completion = _completion;
        if (completion is null || completion.Task.IsCompleted)
        {
            if (_host?.IsOpen == true)
            {
                await _host.CloseAsync();
            }

            return;
        }

        try
        {
            if (_host?.IsOpen == true)
            {
                _host.IsHitTestVisible = false;
                await _host.CloseAsync();
            }
        }
        finally
        {
            _host?.Clear();
            if (_host is not null)
            {
                _host.IsHitTestVisible = true;
            }

            if (ReferenceEquals(_completion, completion))
            {
                _completion = null;
                _currentRequest = null;
            }

            completion.TrySetResult(result);
        }
    }

    public async Task<bool> CloseOnEscapeAsync()
    {
        if (!IsOpen || _currentRequest?.CloseOnEscape != true)
        {
            return false;
        }

        await CloseAsync(null);
        return true;
    }

    private void OnHostCloseRequested(object? sender, EventArgs e)
    {
        _ = CloseAsync(null);
    }
}
