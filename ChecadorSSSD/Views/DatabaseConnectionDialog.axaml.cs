using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.Views;

public partial class DatabaseConnectionDialog : Window
{
    private readonly DatabaseConnectionService _connectionService;

    private TextBlock? _connectionErrorText;
    private TextBlock? _testStatusText;
    private TextBox? _serverTextBox;
    private TextBox? _portTextBox;
    private TextBox? _databaseTextBox;
    private TextBox? _userTextBox;
    private TextBox? _passwordTextBox;
    private Button? _continueOfflineButton;
    private Button? _testButton;
    private Button? _saveButton;
    private bool _isClosed;

    public DatabaseConnectionDialog()
        : this(new DatabaseConnectionService(new SettingsService()), string.Empty)
    {
    }

    public DatabaseConnectionDialog(
        DatabaseConnectionService connectionService,
        string initialError)
    {
        _connectionService = connectionService;
        InitializeComponent();
        LoadCurrentSettings();

        if (_connectionErrorText != null)
        {
            _connectionErrorText.Text = string.IsNullOrWhiteSpace(initialError)
                ? "No hay una conexión activa con MySQL."
                : initialError;
        }
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        _connectionErrorText = this.FindControl<TextBlock>("ConnectionErrorText");
        _testStatusText = this.FindControl<TextBlock>("TestStatusText");
        _serverTextBox = this.FindControl<TextBox>("ServerTextBox");
        _portTextBox = this.FindControl<TextBox>("PortTextBox");
        _databaseTextBox = this.FindControl<TextBox>("DatabaseTextBox");
        _userTextBox = this.FindControl<TextBox>("UserTextBox");
        _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
        _continueOfflineButton = this.FindControl<Button>("ContinueOfflineButton");
        _testButton = this.FindControl<Button>("TestButton");
        _saveButton = this.FindControl<Button>("SaveButton");
    }

    private void LoadCurrentSettings()
    {
        var settings = _connectionService.GetCurrentSettings();
        if (_serverTextBox != null) _serverTextBox.Text = settings.Server;
        if (_portTextBox != null) _portTextBox.Text = settings.Port.ToString();
        if (_databaseTextBox != null) _databaseTextBox.Text = settings.Database;
        if (_userTextBox != null) _userTextBox.Text = settings.User;
        if (_passwordTextBox != null) _passwordTextBox.Text = settings.Password;
    }

    private async void TestConnection_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await TestConnectionAsync();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error inesperado al probar MySQL: {ex}");
            if (!_isClosed)
                ShowStatus(DatabaseConnectionResult.Fail($"No se pudo completar la prueba.\n\nDetalle: {ex.Message}"));
        }
    }

    private async void SaveAndRestart_Click(object? sender, RoutedEventArgs e)
    {
        var connectionString = TryBuildConnectionString();
        if (connectionString == null)
            return;

        DatabaseConnectionResult result;
        SetBusy(true);
        try
        {
            result = await DatabaseConnectionService.TestAsync(connectionString);
        }
        finally
        {
            if (!_isClosed)
                SetBusy(false);
        }

        if (_isClosed)
            return;

        ShowStatus(result);

        if (!result.Success)
            return;

        try
        {
            _connectionService.Save(connectionString);
            Close(true);
        }
        catch (Exception ex)
        {
            ShowStatus(DatabaseConnectionResult.Fail(
                $"La conexión funciona, pero no se pudo guardar la configuración.\n\nDetalle: {ex.Message}"));
        }
    }

    private void ContinueOffline_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async Task<bool> TestConnectionAsync()
    {
        var connectionString = TryBuildConnectionString();
        if (connectionString == null)
            return false;

        DatabaseConnectionResult result;
        SetBusy(true);
        try
        {
            result = await DatabaseConnectionService.TestAsync(connectionString);
        }
        finally
        {
            if (!_isClosed)
                SetBusy(false);
        }

        if (_isClosed)
            return false;

        ShowStatus(result);
        return result.Success;
    }

    private string? TryBuildConnectionString()
    {
        try
        {
            if (!uint.TryParse(_portTextBox?.Text, out var port))
                throw new ArgumentException("El puerto debe ser un número válido.");

            var settings = new DatabaseConnectionSettings(
                _serverTextBox?.Text ?? string.Empty,
                port,
                _databaseTextBox?.Text ?? string.Empty,
                _userTextBox?.Text ?? string.Empty,
                _passwordTextBox?.Text ?? string.Empty);

            return DatabaseConnectionService.BuildConnectionString(settings);
        }
        catch (Exception ex)
        {
            ShowStatus(DatabaseConnectionResult.Fail(ex.Message));
            return null;
        }
    }

    private void SetBusy(bool busy)
    {
        if (_continueOfflineButton != null) _continueOfflineButton.IsEnabled = !busy;
        if (_testButton != null) _testButton.IsEnabled = !busy;
        if (_saveButton != null) _saveButton.IsEnabled = !busy;
        if (_testStatusText != null && busy)
        {
            _testStatusText.Text = "Probando conexión...";
            _testStatusText.Foreground = Brush.Parse("#A5B2C2");
        }
    }

    private void ShowStatus(DatabaseConnectionResult result)
    {
        if (_testStatusText == null)
            return;

        _testStatusText.Text = result.Message;
        _testStatusText.Foreground = Brush.Parse(result.Success ? "#42A77A" : "#D36262");
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        base.OnClosed(e);
    }
}
