using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WPF.Dialogs;

public partial class DatabaseConfigurationDialog : Window
{
    private readonly IDatabaseConfigurationService _databaseConfigService;

    // ── Public state ──────────────────────────────────────────────────────────

    /// <summary>Currently selected database provider.</summary>
    public DatabaseProvider SelectedProvider { get; private set; } = DatabaseProvider.Sqlite;

    /// <summary>
    /// The fully assembled configuration produced by <see cref="Save_Click"/>.
    /// Null until the dialog is saved successfully.
    /// </summary>
    public DatabaseConfiguration? Configuration { get; private set; }

    // Plaintext password strings — encrypted on save
    public string SqlServerPassword { get; private set; } = string.Empty;
    public string MySqlPassword    { get; private set; } = string.Empty;
    public string PostgreSqlPassword { get; private set; } = string.Empty;

    // ── Constructor ───────────────────────────────────────────────────────────

    public DatabaseConfigurationDialog(IDatabaseConfigurationService databaseConfigService)
    {
        _databaseConfigService = databaseConfigService;
        InitializeComponent();

        // Load any existing saved configuration into the form
        _ = LoadExistingConfigurationAsync();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task LoadExistingConfigurationAsync()
    {
        try
        {
            var config = await _databaseConfigService.GetConfigurationAsync();
            PopulateFormFromConfig(config);
        }
        catch
        {
            // If loading fails, the form defaults are already set in XAML
        }
    }

    private void PopulateFormFromConfig(DatabaseConfiguration config)
    {
        // Switch provider selector
        var providerIndex = config.Provider switch
        {
            DatabaseProvider.Sqlite    => 0,
            DatabaseProvider.SqlServer => 1,
            DatabaseProvider.MySql     => 2,
            DatabaseProvider.PostgreSql => 3,
            _ => 0
        };
        ProviderComboBox.SelectedIndex = providerIndex;
        SelectedProvider = config.Provider;
        OnProviderChanged();

        // SQLite
        if (config.Sqlite != null)
        {
            SqliteDatabasePathTextBox.Text = config.Sqlite.DatabasePath;
        }

        // SQL Server
        if (config.SqlServer != null)
        {
            var ss = config.SqlServer;
            SqlServerHostTextBox.Text        = ss.Host;
            SqlServerInstanceTextBox.Text    = ss.InstanceName ?? string.Empty;
            SqlServerPortTextBox.Text        = ss.Port.ToString();
            SqlServerDatabaseTextBox.Text    = ss.Database;
            SqlServerAuthModeComboBox.SelectedIndex = ss.AuthMode == SqlServerAuthMode.WindowsAuthentication ? 0 : 1;
            SqlServerUsernameTextBox.Text    = ss.Username ?? string.Empty;
            // Password left blank (cannot decrypt here without async; user must re-enter if needed)
            SqlServerEncryptionComboBox.SelectedIndex = ss.Encryption switch
            {
                SqlServerEncryptionMode.None      => 0,
                SqlServerEncryptionMode.Optional  => 1,
                SqlServerEncryptionMode.Mandatory => 2,
                SqlServerEncryptionMode.Strict    => 3,
                _ => 1
            };
            SqlServerTrustCertCheckBox.IsChecked     = ss.TrustServerCertificate;
            SqlServerCertificateTextBox.Text         = ss.ServerCertificate ?? string.Empty;
            SqlServerProtocolComboBox.SelectedIndex  = ss.NetworkProtocol switch
            {
                SqlServerNetworkProtocol.Default      => 0,
                SqlServerNetworkProtocol.TcpIp        => 1,
                SqlServerNetworkProtocol.NamedPipes   => 2,
                SqlServerNetworkProtocol.SharedMemory => 3,
                _ => 0
            };
            SqlServerPacketSizeTextBox.Text   = ss.PacketSize.ToString();
            SqlServerConnTimeoutTextBox.Text  = ss.ConnectionTimeout.ToString();
            SqlServerCmdTimeoutTextBox.Text   = ss.CommandTimeout.ToString();
            SqlServerAppNameTextBox.Text      = ss.ApplicationName;
            SqlServerWorkstationTextBox.Text  = ss.WorkstationId ?? string.Empty;
            SqlServerMarsCheckBox.IsChecked   = ss.MultipleActiveResultSets;
            SqlServerAppIntentComboBox.SelectedIndex = ss.ApplicationIntent == SqlServerApplicationIntent.ReadOnly ? 1 : 0;
            SqlServerMultiSubnetCheckBox.IsChecked   = ss.MultiSubnetFailover;
            SqlServerFailoverTextBox.Text     = ss.FailoverPartner ?? string.Empty;
            SqlServerPoolingCheckBox.IsChecked = ss.Pooling;
            SqlServerMinPoolTextBox.Text      = ss.MinPoolSize.ToString();
            SqlServerMaxPoolTextBox.Text      = ss.MaxPoolSize.ToString();
            SqlServerAdditionalParamsTextBox.Text = ss.AdditionalParameters ?? string.Empty;
            UpdateSqlServerAuthPanelVisibility();
            UpdateSqlServerCertPanelVisibility();
            UpdateSqlServerPoolPanelVisibility();
        }

        // MySQL
        if (config.MySql != null)
        {
            var my = config.MySql;
            MySqlHostTextBox.Text     = my.Host;
            MySqlPortTextBox.Text     = my.Port.ToString();
            MySqlDatabaseTextBox.Text = my.Database;
            MySqlUsernameTextBox.Text = my.Username;
            MySqlSslModeComboBox.SelectedIndex = my.SslMode switch
            {
                MySqlSslMode.None       => 0,
                MySqlSslMode.Preferred  => 1,
                MySqlSslMode.Required   => 2,
                MySqlSslMode.VerifyCA   => 3,
                MySqlSslMode.VerifyFull => 4,
                _ => 1
            };
            MySqlSslCaTextBox.Text    = my.SslCaPath ?? string.Empty;
            MySqlSslCertTextBox.Text  = my.SslCertPath ?? string.Empty;
            MySqlSslKeyTextBox.Text   = my.SslKeyPath ?? string.Empty;
            MySqlConnTimeoutTextBox.Text = my.ConnectionTimeout.ToString();
            MySqlCmdTimeoutTextBox.Text  = my.CommandTimeout.ToString();
            MySqlCharSetTextBox.Text     = my.CharacterSet;
            MySqlAllowZeroDateTimeCheckBox.IsChecked  = my.AllowZeroDateTime;
            MySqlAllowUserVariablesCheckBox.IsChecked = my.AllowUserVariables;
            MySqlPoolingCheckBox.IsChecked = my.Pooling;
            MySqlMinPoolTextBox.Text = my.MinPoolSize.ToString();
            MySqlMaxPoolTextBox.Text = my.MaxPoolSize.ToString();
            UpdateMySqlCertPanelVisibility();
            UpdateMySqlPoolPanelVisibility();
        }

        // PostgreSQL
        if (config.PostgreSql != null)
        {
            var pg = config.PostgreSql;
            PgHostTextBox.Text     = pg.Host;
            PgPortTextBox.Text     = pg.Port.ToString();
            PgDatabaseTextBox.Text = pg.Database;
            PgUsernameTextBox.Text = pg.Username;
            PgSslModeComboBox.SelectedIndex = pg.SslMode switch
            {
                PostgreSqlSslMode.Disable    => 0,
                PostgreSqlSslMode.Allow      => 1,
                PostgreSqlSslMode.Prefer     => 2,
                PostgreSqlSslMode.Require    => 3,
                PostgreSqlSslMode.VerifyCA   => 4,
                PostgreSqlSslMode.VerifyFull => 5,
                _ => 2
            };
            PgSslCertTextBox.Text     = pg.SslCertPath ?? string.Empty;
            PgSslKeyTextBox.Text      = pg.SslKeyPath ?? string.Empty;
            PgSslRootCertTextBox.Text = pg.SslRootCertPath ?? string.Empty;
            PgConnTimeoutTextBox.Text = pg.ConnectionTimeout.ToString();
            PgCmdTimeoutTextBox.Text  = pg.CommandTimeout.ToString();
            PgAppNameTextBox.Text     = pg.ApplicationName;
            PgSearchPathTextBox.Text  = pg.SearchPath ?? string.Empty;
            PgPoolingCheckBox.IsChecked = pg.Pooling;
            PgMinPoolTextBox.Text = pg.MinPoolSize.ToString();
            PgMaxPoolTextBox.Text = pg.MaxPoolSize.ToString();
            UpdatePgCertPanelVisibility();
            UpdatePgPoolPanelVisibility();
        }
    }

    // ── Provider switching ────────────────────────────────────────────────────

    private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedProvider = ProviderComboBox.SelectedIndex switch
        {
            1 => DatabaseProvider.SqlServer,
            2 => DatabaseProvider.MySql,
            3 => DatabaseProvider.PostgreSql,
            _ => DatabaseProvider.Sqlite
        };
        OnProviderChanged();
    }

    private void OnProviderChanged()
    {
        SqlitePanel.Visibility    = SelectedProvider == DatabaseProvider.Sqlite    ? Visibility.Visible : Visibility.Collapsed;
        SqlServerPanel.Visibility = SelectedProvider == DatabaseProvider.SqlServer ? Visibility.Visible : Visibility.Collapsed;
        MySqlPanel.Visibility     = SelectedProvider == DatabaseProvider.MySql     ? Visibility.Visible : Visibility.Collapsed;
        PostgreSqlPanel.Visibility = SelectedProvider == DatabaseProvider.PostgreSql ? Visibility.Visible : Visibility.Collapsed;

        EnsureProviderConfig();
    }

    private void EnsureProviderConfig()
    {
        // No-op for UI purposes — sub-objects are created in BuildCurrentConfig()
        // This method exists so callers have a named entry point.
    }

    // ── SQL Server conditional visibility ─────────────────────────────────────

    private void SqlServerAuthMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateSqlServerAuthPanelVisibility();

    private void UpdateSqlServerAuthPanelVisibility()
    {
        bool sqlAuth = SqlServerAuthModeComboBox.SelectedIndex == 1;
        SqlServerUsernamePanel.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
        SqlServerPasswordPanel.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SqlServerEncryption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateSqlServerCertPanelVisibility();

    private void UpdateSqlServerCertPanelVisibility()
    {
        int idx = SqlServerEncryptionComboBox.SelectedIndex;
        // Show cert panel for Mandatory (2) or Strict (3)
        SqlServerCertificatePanel.Visibility = idx >= 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SqlServerPooling_Changed(object sender, RoutedEventArgs e)
        => UpdateSqlServerPoolPanelVisibility();

    private void UpdateSqlServerPoolPanelVisibility()
    {
        SqlServerPoolSizePanel.Visibility = SqlServerPoolingCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── MySQL conditional visibility ──────────────────────────────────────────

    private void MySqlSslMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateMySqlCertPanelVisibility();

    private void UpdateMySqlCertPanelVisibility()
    {
        int idx = MySqlSslModeComboBox.SelectedIndex;
        // Show cert paths for VerifyCA (3) or VerifyFull (4)
        MySqlCertPathsPanel.Visibility = idx >= 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MySqlPooling_Changed(object sender, RoutedEventArgs e)
        => UpdateMySqlPoolPanelVisibility();

    private void UpdateMySqlPoolPanelVisibility()
    {
        MySqlPoolSizePanel.Visibility = MySqlPoolingCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── PostgreSQL conditional visibility ─────────────────────────────────────

    private void PgSslMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdatePgCertPanelVisibility();

    private void UpdatePgCertPanelVisibility()
    {
        int idx = PgSslModeComboBox.SelectedIndex;
        // Show cert paths for VerifyCA (4) or VerifyFull (5)
        PgCertPathsPanel.Visibility = idx >= 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PgPooling_Changed(object sender, RoutedEventArgs e)
        => UpdatePgPoolPanelVisibility();

    private void UpdatePgPoolPanelVisibility()
    {
        PgPoolSizePanel.Visibility = PgPoolingCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Browse helpers ────────────────────────────────────────────────────────

    /// <summary>Opens an OpenFileDialog and places the selected path into <paramref name="target"/>.</summary>
    private void BrowseFile(TextBox target, string filter)
    {
        var dlg = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = false
        };
        if (dlg.ShowDialog(this) == true)
        {
            target.Text = dlg.FileName;
        }
    }

    private void BrowseSqliteFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title  = "Choose SQLite database location",
            Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            DefaultExt = ".db",
            FileName = "passwordmanager.db"
        };
        if (dlg.ShowDialog(this) == true)
        {
            SqliteDatabasePathTextBox.Text = dlg.FileName;
        }
    }

    private void BrowseMySqlCa_Click(object sender, RoutedEventArgs e)
        => BrowseFile(MySqlSslCaTextBox, "Certificate Files (*.pem;*.crt;*.cer)|*.pem;*.crt;*.cer|All Files (*.*)|*.*");

    private void BrowseMySqlCert_Click(object sender, RoutedEventArgs e)
        => BrowseFile(MySqlSslCertTextBox, "Certificate Files (*.pem;*.crt;*.cer)|*.pem;*.crt;*.cer|All Files (*.*)|*.*");

    private void BrowseMySqlKey_Click(object sender, RoutedEventArgs e)
        => BrowseFile(MySqlSslKeyTextBox, "Key Files (*.pem;*.key)|*.pem;*.key|All Files (*.*)|*.*");

    private void BrowsePgSslCert_Click(object sender, RoutedEventArgs e)
        => BrowseFile(PgSslCertTextBox, "Certificate Files (*.pem;*.crt;*.cer)|*.pem;*.crt;*.cer|All Files (*.*)|*.*");

    private void BrowsePgSslKey_Click(object sender, RoutedEventArgs e)
        => BrowseFile(PgSslKeyTextBox, "Key Files (*.pem;*.key)|*.pem;*.key|All Files (*.*)|*.*");

    private void BrowsePgSslRootCert_Click(object sender, RoutedEventArgs e)
        => BrowseFile(PgSslRootCertTextBox, "Certificate Files (*.pem;*.crt;*.cer)|*.pem;*.crt;*.cer|All Files (*.*)|*.*");

    // ── Test Connection ───────────────────────────────────────────────────────

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        TestConnectionButton.IsEnabled = false;
        SetStatus("Testing connection...", neutral: true);

        try
        {
            var config = BuildCurrentConfig();
            var (success, error) = await _databaseConfigService.TestConnectionAsync(config);

            if (success)
                SetStatus("Connection successful.", success: true);
            else
                SetStatus($"Connection failed: {error}", success: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", success: false);
        }
        finally
        {
            TestConnectionButton.IsEnabled = true;
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateRequiredFields(out string validationError))
        {
            SetStatus(validationError, success: false);
            return;
        }

        SaveButton.IsEnabled = false;
        SetStatus("Saving...", neutral: true);

        try
        {
            // Capture plaintext passwords before clearing them
            SqlServerPassword   = SqlServerPasswordBox.Password;
            MySqlPassword       = MySqlPasswordBox.Password;
            PostgreSqlPassword  = PgPasswordBox.Password;

            var config = BuildCurrentConfig();
            await SetEncryptedPasswordsAsync(config);
            await _databaseConfigService.SaveConfigurationAsync(config);

            Configuration = config;
            DialogResult  = true;
            Close();
        }
        catch (Exception ex)
        {
            SetStatus($"Save failed: {ex.Message}", success: false);
            SaveButton.IsEnabled = true;
        }
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Build config from current form state ──────────────────────────────────

    private DatabaseConfiguration BuildCurrentConfig()
    {
        var config = new DatabaseConfiguration
        {
            Provider   = SelectedProvider,
            IsFirstRun = false
        };

        switch (SelectedProvider)
        {
            case DatabaseProvider.Sqlite:
                config.Sqlite = new SqliteConfig
                {
                    DatabasePath = SqliteDatabasePathTextBox.Text.Trim()
                };
                break;

            case DatabaseProvider.SqlServer:
                config.SqlServer = BuildSqlServerConfig();
                break;

            case DatabaseProvider.MySql:
                config.MySql = BuildMySqlConfig();
                break;

            case DatabaseProvider.PostgreSql:
                config.PostgreSql = BuildPostgreSqlConfig();
                break;
        }

        return config;
    }

    private SqlServerConfig BuildSqlServerConfig()
    {
        bool sqlAuth = SqlServerAuthModeComboBox.SelectedIndex == 1;

        return new SqlServerConfig
        {
            Host             = SqlServerHostTextBox.Text.Trim(),
            InstanceName     = NullIfEmpty(SqlServerInstanceTextBox.Text),
            Port             = ParseInt(SqlServerPortTextBox.Text, 1433),
            Database         = SqlServerDatabaseTextBox.Text.Trim(),
            AuthMode         = sqlAuth
                                 ? SqlServerAuthMode.SqlServerAuthentication
                                 : SqlServerAuthMode.WindowsAuthentication,
            Username         = sqlAuth ? NullIfEmpty(SqlServerUsernameTextBox.Text) : null,
            // EncryptedPassword populated later by SetEncryptedPasswordsAsync
            Encryption       = SqlServerEncryptionComboBox.SelectedIndex switch
            {
                0 => SqlServerEncryptionMode.None,
                2 => SqlServerEncryptionMode.Mandatory,
                3 => SqlServerEncryptionMode.Strict,
                _ => SqlServerEncryptionMode.Optional
            },
            TrustServerCertificate = SqlServerTrustCertCheckBox.IsChecked == true,
            ServerCertificate = NullIfEmpty(SqlServerCertificateTextBox.Text),
            NetworkProtocol  = SqlServerProtocolComboBox.SelectedIndex switch
            {
                1 => SqlServerNetworkProtocol.TcpIp,
                2 => SqlServerNetworkProtocol.NamedPipes,
                3 => SqlServerNetworkProtocol.SharedMemory,
                _ => SqlServerNetworkProtocol.Default
            },
            PacketSize            = ParseInt(SqlServerPacketSizeTextBox.Text, 4096),
            ConnectionTimeout     = ParseInt(SqlServerConnTimeoutTextBox.Text, 15),
            CommandTimeout        = ParseInt(SqlServerCmdTimeoutTextBox.Text, 30),
            ApplicationName       = SqlServerAppNameTextBox.Text.Trim(),
            WorkstationId         = NullIfEmpty(SqlServerWorkstationTextBox.Text),
            MultipleActiveResultSets = SqlServerMarsCheckBox.IsChecked == true,
            ApplicationIntent    = SqlServerAppIntentComboBox.SelectedIndex == 1
                                     ? SqlServerApplicationIntent.ReadOnly
                                     : SqlServerApplicationIntent.ReadWrite,
            MultiSubnetFailover  = SqlServerMultiSubnetCheckBox.IsChecked == true,
            FailoverPartner      = NullIfEmpty(SqlServerFailoverTextBox.Text),
            Pooling              = SqlServerPoolingCheckBox.IsChecked == true,
            MinPoolSize          = ParseInt(SqlServerMinPoolTextBox.Text, 0),
            MaxPoolSize          = ParseInt(SqlServerMaxPoolTextBox.Text, 100),
            AdditionalParameters = NullIfEmpty(SqlServerAdditionalParamsTextBox.Text)
        };
    }

    private MySqlConfig BuildMySqlConfig()
    {
        return new MySqlConfig
        {
            Host     = MySqlHostTextBox.Text.Trim(),
            Port     = ParseInt(MySqlPortTextBox.Text, 3306),
            Database = MySqlDatabaseTextBox.Text.Trim(),
            Username = MySqlUsernameTextBox.Text.Trim(),
            // EncryptedPassword populated later
            SslMode  = MySqlSslModeComboBox.SelectedIndex switch
            {
                0 => MySqlSslMode.None,
                2 => MySqlSslMode.Required,
                3 => MySqlSslMode.VerifyCA,
                4 => MySqlSslMode.VerifyFull,
                _ => MySqlSslMode.Preferred
            },
            SslCaPath   = NullIfEmpty(MySqlSslCaTextBox.Text),
            SslCertPath = NullIfEmpty(MySqlSslCertTextBox.Text),
            SslKeyPath  = NullIfEmpty(MySqlSslKeyTextBox.Text),
            ConnectionTimeout = ParseInt(MySqlConnTimeoutTextBox.Text, 30),
            CommandTimeout    = ParseInt(MySqlCmdTimeoutTextBox.Text, 30),
            CharacterSet      = MySqlCharSetTextBox.Text.Trim(),
            AllowZeroDateTime  = MySqlAllowZeroDateTimeCheckBox.IsChecked == true,
            AllowUserVariables = MySqlAllowUserVariablesCheckBox.IsChecked == true,
            Pooling    = MySqlPoolingCheckBox.IsChecked == true,
            MinPoolSize = ParseInt(MySqlMinPoolTextBox.Text, 0),
            MaxPoolSize = ParseInt(MySqlMaxPoolTextBox.Text, 100)
        };
    }

    private PostgreSqlConfig BuildPostgreSqlConfig()
    {
        return new PostgreSqlConfig
        {
            Host     = PgHostTextBox.Text.Trim(),
            Port     = ParseInt(PgPortTextBox.Text, 5432),
            Database = PgDatabaseTextBox.Text.Trim(),
            Username = PgUsernameTextBox.Text.Trim(),
            // EncryptedPassword populated later
            SslMode  = PgSslModeComboBox.SelectedIndex switch
            {
                0 => PostgreSqlSslMode.Disable,
                1 => PostgreSqlSslMode.Allow,
                3 => PostgreSqlSslMode.Require,
                4 => PostgreSqlSslMode.VerifyCA,
                5 => PostgreSqlSslMode.VerifyFull,
                _ => PostgreSqlSslMode.Prefer
            },
            SslCertPath     = NullIfEmpty(PgSslCertTextBox.Text),
            SslKeyPath      = NullIfEmpty(PgSslKeyTextBox.Text),
            SslRootCertPath = NullIfEmpty(PgSslRootCertTextBox.Text),
            ConnectionTimeout = ParseInt(PgConnTimeoutTextBox.Text, 30),
            CommandTimeout    = ParseInt(PgCmdTimeoutTextBox.Text, 30),
            ApplicationName   = PgAppNameTextBox.Text.Trim(),
            SearchPath        = NullIfEmpty(PgSearchPathTextBox.Text),
            Pooling    = PgPoolingCheckBox.IsChecked == true,
            MinPoolSize = ParseInt(PgMinPoolTextBox.Text, 1),
            MaxPoolSize = ParseInt(PgMaxPoolTextBox.Text, 100)
        };
    }

    // ── Password encryption ───────────────────────────────────────────────────

    private async System.Threading.Tasks.Task SetEncryptedPasswordsAsync(DatabaseConfiguration config)
    {
        if (config.SqlServer != null && !string.IsNullOrEmpty(SqlServerPassword))
        {
            config.SqlServer.EncryptedPassword = await _databaseConfigService.EncryptPasswordAsync(SqlServerPassword);
        }

        if (config.MySql != null && !string.IsNullOrEmpty(MySqlPassword))
        {
            config.MySql.EncryptedPassword = await _databaseConfigService.EncryptPasswordAsync(MySqlPassword);
        }

        if (config.PostgreSql != null && !string.IsNullOrEmpty(PostgreSqlPassword))
        {
            config.PostgreSql.EncryptedPassword = await _databaseConfigService.EncryptPasswordAsync(PostgreSqlPassword);
        }
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateRequiredFields(out string error)
    {
        error = string.Empty;

        switch (SelectedProvider)
        {
            case DatabaseProvider.SqlServer:
                if (string.IsNullOrWhiteSpace(SqlServerHostTextBox.Text))
                {
                    error = "SQL Server: Server is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(SqlServerDatabaseTextBox.Text))
                {
                    error = "SQL Server: Database is required.";
                    return false;
                }
                if (SqlServerAuthModeComboBox.SelectedIndex == 1 &&
                    string.IsNullOrWhiteSpace(SqlServerUsernameTextBox.Text))
                {
                    error = "SQL Server: Username is required for SQL Server Authentication.";
                    return false;
                }
                break;

            case DatabaseProvider.MySql:
                if (string.IsNullOrWhiteSpace(MySqlHostTextBox.Text))
                {
                    error = "MySQL: Server is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(MySqlDatabaseTextBox.Text))
                {
                    error = "MySQL: Database is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(MySqlUsernameTextBox.Text))
                {
                    error = "MySQL: Username is required.";
                    return false;
                }
                break;

            case DatabaseProvider.PostgreSql:
                if (string.IsNullOrWhiteSpace(PgHostTextBox.Text))
                {
                    error = "PostgreSQL: Server is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(PgDatabaseTextBox.Text))
                {
                    error = "PostgreSQL: Database is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(PgUsernameTextBox.Text))
                {
                    error = "PostgreSQL: Username is required.";
                    return false;
                }
                break;
        }

        return true;
    }

    // ── Status helpers ────────────────────────────────────────────────────────

    private void SetStatus(string message, bool? success = null, bool neutral = false)
    {
        StatusText.Text = message;

        if (neutral || success == null)
        {
            StatusText.Foreground = (Brush)FindResource("ModernTextSecondaryBrush");
        }
        else if (success == true)
        {
            StatusText.Foreground = (Brush)FindResource("ModernSuccessBrush");
        }
        else
        {
            StatusText.Foreground = (Brush)FindResource("ModernErrorBrush");
        }
    }

    // ── Small utilities ───────────────────────────────────────────────────────

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int ParseInt(string text, int fallback)
        => int.TryParse(text?.Trim(), out int result) ? result : fallback;
}
