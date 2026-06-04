using Microsoft.AspNetCore.Components;
using SecureAccess.Application.Features.AccessRequests;
using SecureAccess.Application.Features.Clients;
using SecureAccess.Application.Features.Machines;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Web.Components.Pages;

public partial class RequestAccess
{
    [Inject] private IAccessRequestService AccessRequests { get; set; } = default!;
    [Inject] private IMachineService Machines { get; set; } = default!;
    [Inject] private IClientService Clients { get; set; } = default!;

    [SupplyParameterFromQuery]
    public int? MachineId { get; set; }

    private List<ClientDto> _clients = new();
    private List<MachineDto> _machines = new();
    private int _clientId;
    private CredentialAccessRequest _request = new();
    private AccessRequestResult? _result;
    private readonly HashSet<string> _shown = new();
    private string? _error;
    private bool _busy;
    private bool _showNote = true;

    protected override async Task OnInitializedAsync()
    {
        _clients = (await Clients.GetClientsAsync(null, 1, 500)).Items.ToList();
        if (MachineId is { } mid)
        {
            _request.MachineId = mid;
            var machine = await Machines.GetMachineAsync(mid);
            if (machine is not null)
            {
                _clientId = machine.ClientId;
                await OnClientChangedAsync();
                _request.MachineId = mid;
            }
        }
    }

    private async Task OnClientChangedAsync()
    {
        _machines = _clientId == 0
            ? new List<MachineDto>()
            : (await Machines.GetMachinesAsync(_clientId, null, 1, 500)).Items.ToList();

        if (_request.MachineId != 0 && _machines.All(m => m.Id != _request.MachineId))
            _request.MachineId = 0;
    }

    private async Task SubmitAsync()
    {
        _error = null;
        _result = null;
        _busy = true;

        try
        {
            if (_request.MachineId == 0)
            {
                _error = "Please select a machine.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_request.Reason))
            {
                _error = "Please enter a reason for this access request.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_request.InternalTicket))
            {
                _error = "Please enter an internal ticket reference.";
                return;
            }

            var result = await AccessRequests.RequestCredentialsAsync(_request);
            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            _result = result.Value;
            _shown.Clear();
        }
        finally
        {
            _busy = false;
        }
    }

    private void ClearForm()
    {
        _error = null;
        _result = null;
        _shown.Clear();
        _clientId = 0;
        _machines = new List<MachineDto>();
        _request = new CredentialAccessRequest();
        _showNote = true;
    }

    private bool IsShown(int credId, string key) => _shown.Contains($"{credId}:{key}");

    private void ToggleSecret(int credId, string key)
    {
        var composite = $"{credId}:{key}";
        if (!_shown.Add(composite))
            _shown.Remove(composite);
    }

    private static string FormatReasonCategory(AccessReasonCategory category) =>
        category switch
        {
            AccessReasonCategory.SoftwareUpdate => "Software Update",
            AccessReasonCategory.PreventiveMaintenance => "Preventive Maintenance",
            AccessReasonCategory.CorrectiveMaintenance => "Corrective Maintenance",
            AccessReasonCategory.Installation => "Installation",
            AccessReasonCategory.Training => "Training",
            AccessReasonCategory.Diagnostics => "Diagnostics",
            AccessReasonCategory.EmergencySupport => "Emergency Support",
            AccessReasonCategory.Other => "Other",
            _ => category.ToString()
        };
}
