using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using VaultGuard.Models.Configuration;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Dialogs;

public partial class DatabaseConfigurationDialog : Window
{
    private readonly IDatabaseConfigurationService _databaseConfigService;

    // ── Public state ──────────────────────────────────────────────────────────

    public DatabaseProvider SelectedProvider { get; private set; } = DatabaseProvider.Sqlite;

    /// <summary>The fully assembled configuration produced by Save. Null until saved successfully.</summary>
    public DatabaseConfiguration? Configuration { get; private set; }

    // Plaintext passwords captured from PasswordBox on save — encrypted by SetEncryptedPasswordsAsync
    public string SqlServerPassword  { get; private set; } = string.Empty;
    public string MySqlPassword      { get; private set; } = string.Empty;
    public string PostgreSqlPassword { get; private set; } = string.Empty;

    // ── Internal state ────────────────────────────────────────────────────────

    // Prevents cascading UpdateXxx calls while PopulateFormFromConfig is running.
    private bool _isPopulating;

    // Encrypted passwords from the previously-saved config. Carried over on save when
    // the user leaves the PasswordBox blank (meaning "keep existing password").
    private string? _existingSqlServerEncryptedPwd;
    private string? _existingMySqlEncryptedPwd;
    private string? _existingPgEncryptedPwd;

    // ── Constructor ───────────────────────────────────────────────────────────

    public DatabaseConfigurationDialog(IDatabaseConfigurationService databaseConfigService)
    {
        _databaseConfigService = databaseConfigService;
        InitializeComponent();
        Helpers.Win11Chrome.Apply(this);

        // Wait for Loaded so every named control is fully materialised before
        // async population runs — avoids NullReferenceException on TabControl items
        // that are in non-selected tabs during InitializeComponent.
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnWindowLoaded;
        _ = LoadExistingConfigurationAsync();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task LoadExistingConfigurationAsync()
    {
        try
        {
            var config = await _databaseConfigService.GetConfigurationAsync();
            if (config != null)
            {
                // Ensure we're on the UI thread — GetConfigurationAsync uses Task.Run
                // internally, so the continuation context is not guaranteed.
                await Dispatcher.InvokeAsync(() => PopulateFormFromConfig(config));
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to load configuration", ex);
        }
    }

    private void PopulateFormFromConfig(DatabaseConfiguration config)
    {
        if (ProviderComboBox == null) return;

        // Suppress all SelectionChanged / Checked event handlers while we programmatically
        // set values so they don't fire UpdateXxx mid-population.
        _isPopulating = true;
        try
        {
            // Store existing encrypted passwords so they can be carried over on save
            // when the user leaves the PasswordBox blank.
            _existingSqlServerEncryptedPwd = config.SqlServer?.EncryptedPassword;
            _existingMySqlEncryptedPwd     = config.MySql?.EncryptedPassword;
            _existingPgEncryptedPwd        = config.PostgreSql?.EncryptedPassword;

            // Provider selector
            SelectedProvider = config.Provider;
            ProviderComboBox.SelectedIndex = config.Provider switch
            {
                DatabaseProvider.SqlServer  => 1,
                DatabaseProvider.MySql      => 2,
                DatabaseProvider.PostgreSql => 3,
                _                           => 0
            };
            OnProviderChanged();

            // SQLite
            if (config.Sqlite != null && SqliteDatabasePathTextBox != null)
            {
                SqliteDatabasePathTextBox.Text = config.Sqlite.DatabasePath ?? string.Empty;
            }

            // SQL Server
            if (config.SqlServer != null)
            {
                var ss = config.SqlServer;
                if (SqlServerHostTextBox      != null) SqlServerHostTextBox.Text       = ss.Host ?? string.Empty;
                if (SqlServerInstanceTextBox  != null) SqlServerInstanceTextBox.Text   = ss.InstanceName ?? string.Empty;
                if (SqlServerPortTextBox      != null) SqlServerPortTextBox.Text       = ss.Port.ToString();
                if (SqlServerDatabaseTextBox  != null) SqlServerDatabaseTextBox.Text   = ss.Database ?? string.Empty;
                if (SqlServerAuthModeComboBox != null)
                    SqlServerAuthModeComboBox.SelectedIndex = ss.AuthMode == SqlServerAuthMode.WindowsAuthentication ? 0 : 1;
                if (SqlServerUsernameTextBox  != null) SqlServerUsernameTextBox.Text   = ss.Username ?? string.Empty;
                // Password left blank — user must re-enter if changing; existing value preserved via _existingSqlServerEncryptedPwd
                if (SqlServerEncryptionComboBox != null)
                    SqlServerEncryptionComboBox.SelectedIndex = ss.Encryption switch
                    {
                        SqlServerEncryptionMode.None      => 0,
                        SqlServerEncryptionMode.Mandatory => 2,
                        SqlServerEncryptionMode.Strict    => 3,
                        _                                 => 1
                    };
                if (SqlServerTrustCertCheckBox    != null) SqlServerTrustCertCheckBox.IsChecked   = ss.TrustServerCertificate;
                if (SqlServerCertificateTextBox   != null) SqlServerCertificateTextBox.Text        = ss.ServerCertificate ?? string.Empty;
                if (SqlServerProtocolComboBox     != null)
                    SqlServerProtocolComboBox.SelectedIndex = ss.NetworkProtocol switch
                    {
                        SqlServerNetworkProtocol.TcpIp        => 1,
                        SqlServerNetworkProtocol.NamedPipes   => 2,
                        SqlServerNetworkProtocol.SharedMemory => 3,
                        _                                     => 0
                    };
                if (SqlServerPacketSizeTextBox    != null) SqlServerPacketSizeTextBox.Text  = ss.PacketSize.ToString();
                if (SqlServerConnTimeoutTextBox   != null) SqlServerConnTimeoutTextBox.Text  = ss.ConnectionTimeout.ToString();
                if (SqlServerCmdTimeoutTextBox    != null) SqlServerCmdTimeoutTextBox.Text   = ss.CommandTimeout.ToString();
                if (SqlServerAppNameTextBox       != null) SqlServerAppNameTextBox.Text      = ss.ApplicationName ?? "VaultGuard";
                if (SqlServerWorkstationTextBox   != null) SqlServerWorkstationTextBox.Text  = ss.WorkstationId ?? string.Empty;
                if (SqlServerMarsCheckBox         != null) SqlServerMarsCheckBox.IsChecked   = ss.MultipleActiveResultSets;
                if (SqlServerAppIntentComboBox    != null)
                    SqlServerAppIntentComboBox.SelectedIndex = ss.ApplicationIntent == SqlServerApplicationIntent.ReadOnly ? 1 : 0;
                if (SqlServerMultiSubnetCheckBox  != null) SqlServerMultiSubnetCheckBox.IsChecked = ss.MultiSubnetFailover;
                if (SqlServerFailoverTextBox      != null) SqlServerFailoverTextBox.Text     = ss.FailoverPartner ?? string.Empty;
                if (SqlServerPoolingCheckBox      != null) SqlServerPoolingCheckBox.IsChecked = ss.Pooling;
                if (SqlServerMinPoolTextBox       != null) SqlServerMinPoolTextBox.Text      = ss.MinPoolSize.ToString();
                if (SqlServerMaxPoolTextBox       != null) SqlServerMaxPoolTextBox.Text      = ss.MaxPoolSize.ToString();
                if (SqlServerAdditionalParamsTextBox != null) SqlServerAdditionalParamsTextBox.Text = ss.AdditionalParameters ?? string.Empty;
            }

            // MySQL
            if (config.MySql != null)
            {
                var my = config.MySql;
                if (MySqlHostTextBox     != null) MySqlHostTextBox.Text     = my.Host ?? string.Empty;
                if (MySqlPortTextBox     != null) MySqlPortTextBox.Text     = my.Port.ToString();
                if (MySqlDatabaseTextBox != null) MySqlDatabaseTextBox.Text = my.Database ?? string.Empty;
                if (MySqlUsernameTextBox != null) MySqlUsernameTextBox.Text = my.Username ?? string.Empty;
                if (MySqlSslModeComboBox != null)
                    MySqlSslModeComboBox.SelectedIndex = my.SslMode switch
                    {
                        MySqlSslMode.None       => 0,
                        MySqlSslMode.Required   => 2,
                        MySqlSslMode.VerifyCA   => 3,
                        MySqlSslMode.VerifyFull => 4,
                        _                       => 1
                    };
                if (MySqlSslCaTextBox    != null) MySqlSslCaTextBox.Text    = my.SslCaPath ?? string.Empty;
                if (MySqlSslCertTextBox  != null) MySqlSslCertTextBox.Text  = my.SslCertPath ?? string.Empty;
                if (MySqlSslKeyTextBox   != null) MySqlSslKeyTextBox.Text   = my.SslKeyPath ?? string.Empty;
                if (MySqlConnTimeoutTextBox != null) MySqlConnTimeoutTextBox.Text = my.ConnectionTimeout.ToString();
                if (MySqlCmdTimeoutTextBox  != null) MySqlCmdTimeoutTextBox.Text  = my.CommandTimeout.ToString();
                if (MySqlCharSetTextBox     != null) MySqlCharSetTextBox.Text     = my.CharacterSet ?? "utf8mb4";
                if (MySqlAllowZeroDateTimeCheckBox  != null) MySqlAllowZeroDateTimeCheckBox.IsChecked  = my.AllowZeroDateTime;
                if (MySqlAllowUserVariablesCheckBox != null) MySqlAllowUserVariablesCheckBox.IsChecked = my.AllowUserVariables;
                if (MySqlPoolingCheckBox != null) MySqlPoolingCheckBox.IsChecked = my.Pooling;
                if (MySqlMinPoolTextBox  != null) MySqlMinPoolTextBox.Text = my.MinPoolSize.ToString();
                if (MySqlMaxPoolTextBox  != null) MySqlMaxPoolTextBox.Text = my.MaxPoolSize.ToString();
            }

            // PostgreSQL
            if (config.PostgreSql != null)
            {
                var pg = config.PostgreSql;
                if (PgHostTextBox     != null) PgHostTextBox.Text     = pg.Host ?? string.Empty;
                if (PgPortTextBox     != null) PgPortTextBox.Text     = pg.Port.ToString();
                if (PgDatabaseTextBox != null) PgDatabaseTextBox.Text = pg.Database ?? string.Empty;
                if (PgUsernameTextBox != null) PgUsernameTextBox.Text = pg.Username ?? string.Empty;
                if (PgSslModeComboBox != null)
                    PgSslModeComboBox.SelectedIndex = pg.SslMode switch
                    {
                        PostgreSqlSslMode.Disable    => 0,
                        PostgreSqlSslMode.Allow      => 1,
                        PostgreSqlSslMode.Require    => 3,
                        PostgreSqlSslMode.VerifyCA   => 4,
                        PostgreSqlSslMode.VerifyFull => 5,
                        _                            => 2
                    };
                if (PgSslCertTextBox     != null) PgSslCertTextBox.Text     = pg.SslCertPath ?? string.Empty;
                if (PgSslKeyTextBox      != null) PgSslKeyTextBox.Text      = pg.SslKeyPath ?? string.Empty;
                if (PgSslRootCertTextBox != null) PgSslRootCertTextBox.Text = pg.SslRootCertPath ?? string.Empty;
                if (PgConnTimeoutTextBox != null) PgConnTimeoutTextBox.Text = pg.ConnectionTimeout.ToString();
                if (PgCmdTimeoutTextBox  != null) PgCmdTimeoutTextBox.Text  = pg.CommandTimeout.ToString();
                if (PgAppNameTextBox     != null) PgAppNameTextBox.Text     = pg.ApplicationName ?? "VaultGuard";
                if (PgSearchPathTextBox  != null) PgSearchPathTextBox.Text  = pg.SearchPath ?? string.Empty;
                if (PgPoolingCheckBox    != null) PgPoolingCheckBox.IsChecked = pg.Pooling;
                if (PgMinPoolTextBox     != null) PgMinPoolTextBox.Text = pg.MinPoolSize.ToString();
                if (PgMaxPoolTextBox     != null) PgMaxPoolTextBox.Text = pg.MaxPoolSize.ToString();
            }
        }
        finally
        {
            _isPopulating = false;
        }

        // Now that all values are set, sync all conditional visibility panels once.
        UpdateSqlServerAuthPanelVisibility();
        UpdateSqlServerCertPanelVisibility();
        UpdateSqlServerPoolPanelVisibility();
        UpdateMySqlCertPanelVisibility();
        UpdateMySqlPoolPanelVisibility();
        UpdatePgCertPanelVisibility();
        UpdatePgPoolPanelVisibility();
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
        if (SqlitePanel == null) return;

        SqlitePanel.Visibility     = SelectedProvider == DatabaseProvider.Sqlite     ? Visibility.Visible : Visibility.Collapsed;
        SqlServerPanel.Visibility  = SelectedProvider == DatabaseProvider.SqlServer  ? Visibility.Visible : Visibility.Collapsed;
        MySqlPanel.Visibility      = SelectedProvider == DatabaseProvider.MySql      ? Visibility.Visible : Visibility.Collapsed;
        PostgreSqlPanel.Visibility = SelectedProvider == DatabaseProvider.PostgreSql ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── SQL Server conditional visibility ─────────────────────────────────────

    private void SqlServerAuthMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateSqlServerAuthPanelVisibility();

    private void UpdateSqlServerAuthPanelVisibility()
    {
        if (_isPopulating) return;
        if (SqlServerAuthModeComboBox == null || SqlServerUsernamePanel == null || SqlServerPasswordPanel == null) return;

        bool sqlAuth = SqlServerAuthModeComboBox.SelectedIndex == 1;
        SqlServerUsernamePanel.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
        SqlServerPasswordPanel.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SqlServerEncryption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateSqlServerCertPanelVisibility();

    private void UpdateSqlServerCertPanelVisibility()
    {
        if (_isPopulating) return;
        if (SqlServerEncryptionComboBox == null || SqlServerCertificatePanel == null) return;

        // Show certificate panel for Mandatory (2) or Strict (3)
        SqlServerCertificatePanel.Visibility = SqlServerEncryptionComboBox.SelectedIndex >= 2
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SqlServerPooling_Changed(object sender, RoutedEventArgs e)
        => UpdateSqlServerPoolPanelVisibility();

    private void UpdateSqlServerPoolPanelVisibility()
    {
        if (_isPopulating) return;
        if (SqlServerPoolingCheckBox == null || SqlServerPoolSizePanel == null) return;

        SqlServerPoolSizePanel.Visibility = SqlServerPoolingCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── MySQL conditional visibility ──────────────────────────────────────────

    private void MySqlSslMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateMySqlCertPanelVisibility();

    private void UpdateMySqlCertPanelVisibility()
    {
        if (_isPopulating) return;
        if (MySqlSslModeComboBox == null || MySqlCertPathsPanel == null) return;

        // Show cert paths for VerifyCA (3) or VerifyFull (4)
        MySqlCertPathsPanel.Visibility = MySqlSslModeComboBox.SelectedIndex >= 3
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MySqlPooling_Changed(object sender, RoutedEventArgs e)
        => UpdateMySqlPoolPanelVisibility();

    private void UpdateMySqlPoolPanelVisibility()
    {
        if (_isPopulating) return;
        if (MySqlPoolingCheckBox == null || MySqlPoolSizePanel == null) return;

        MySqlPoolSizePanel.Visibility = MySqlPoolingCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── PostgreSQL conditional visibility ─────────────────────────────────────

    private void PgSslMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdatePgCertPanelVisibility();

    private void UpdatePgCertPanelVisibility()
    {
        if (_isPopulating) return;
        if (PgSslModeComboBox == null || PgCertPathsPanel == null) return;

        // Show cert paths for VerifyCA (4) or VerifyFull (5)
        PgCertPathsPanel.Visibility = PgSslModeComboBox.SelectedIndex >= 4
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PgPooling_Changed(object sender, RoutedEventArgs e)
        => UpdatePgPoolPanelVisibility();

    private void UpdatePgPoolPanelVisibility()
    {
        if (_isPopulating) return;
        if (PgPoolingCheckBox == null || PgPoolSizePanel == null) return;

        PgPoolSizePanel.Visibility = PgPoolingCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Browse helpers ────────────────────────────────────────────────────────

    private void BrowseFile(TextBox target, string filter)
    {
        var dlg = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = false
        };
        if (dlg.ShowDialog(this) == true)
            target.Text = dlg.FileName;
    }

    private void BrowseSqliteFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title      = "Choose SQLite database location",
            Filter     = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            DefaultExt = ".db",
            FileName   = "passwordmanager.db"
        };
        if (dlg.ShowDialog(this) == true)
            SqliteDatabasePathTextBox.Text = dlg.FileName;
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
        if (TestConnectionButton.IsEnabled == false) return;

        TestConnectionButton.IsEnabled = false;
        SetStatus("Testing connection...", neutral: true);

        try
        {
            if (!ValidateRequiredFields(out string validationError))
            {
                SetStatus(validationError, success: false);
                return;
            }

            var config = BuildCurrentConfig();

            // Carry over existing encrypted passwords so the test works when the user
            // hasn't re-entered a password for an already-saved config.
            ApplyExistingPasswords(config);

            var timeoutTask = System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(30));
            var testTask    = _databaseConfigService.TestConnectionAsync(config);

            if (await System.Threading.Tasks.Task.WhenAny(testTask, timeoutTask) == timeoutTask)
            {
                SetStatus("Connection test timed out after 30 seconds.", success: false);
            }
            else
            {
                var (success, error) = await testTask;
                SetStatus(success ? "Connection successful." : $"Connection failed: {error}", success: success);
            }
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
        if (SaveButton.IsEnabled == false) return;

        if (!ValidateRequiredFields(out string validationError))
        {
            SetStatus(validationError, success: false);
            return;
        }

        SaveButton.IsEnabled   = false;
        CancelButton.IsEnabled = false;
        SetStatus("Saving...", neutral: true);

        try
        {
            // Capture plaintext passwords from PasswordBoxes before building config
            SqlServerPassword  = SqlServerPasswordBox?.Password ?? string.Empty;
            MySqlPassword      = MySqlPasswordBox?.Password ?? string.Empty;
            PostgreSqlPassword = PgPasswordBox?.Password ?? string.Empty;

            var config = BuildCurrentConfig();

            // Carry over existing encrypted passwords for fields the user left blank,
            // so re-saving without re-typing a password doesn't wipe credentials.
            ApplyExistingPasswords(config);

            // Encrypt any newly-entered plaintext passwords
            await SetEncryptedPasswordsAsync(config);

            await _databaseConfigService.SaveConfigurationAsync(config);

            Configuration = config;
            DialogResult  = true;
            // DialogResult setter closes the window automatically — no explicit Close() needed.
        }
        catch (Exception ex)
        {
            SetStatus($"Save failed: {ex.Message}", success: false);
            SaveButton.IsEnabled   = true;
            CancelButton.IsEnabled = true;
        }
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        // DialogResult setter closes the window automatically — no explicit Close() needed.
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
                    DatabasePath = SqliteDatabasePathTextBox?.Text?.Trim() ?? string.Empty
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
        bool sqlAuth = SqlServerAuthModeComboBox?.SelectedIndex == 1;

        return new SqlServerConfig
        {
            Host         = SqlServerHostTextBox?.Text?.Trim() ?? string.Empty,
            InstanceName = NullIfEmpty(SqlServerInstanceTextBox?.Text),
            Port         = ParseInt(SqlServerPortTextBox?.Text, 1433),
            Database     = SqlServerDatabaseTextBox?.Text?.Trim() ?? string.Empty,
            AuthMode     = sqlAuth
                             ? SqlServerAuthMode.SqlServerAuthentication
                             : SqlServerAuthMode.WindowsAuthentication,
            Username         = sqlAuth ? NullIfEmpty(SqlServerUsernameTextBox?.Text) : null,
            // EncryptedPassword populated later by ApplyExistingPasswords + SetEncryptedPasswordsAsync
            Encryption       = SqlServerEncryptionComboBox?.SelectedIndex switch
            {
                0 => SqlServerEncryptionMode.None,
                2 => SqlServerEncryptionMode.Mandatory,
                3 => SqlServerEncryptionMode.Strict,
                _ => SqlServerEncryptionMode.Optional
            },
            TrustServerCertificate   = SqlServerTrustCertCheckBox?.IsChecked == true,
            ServerCertificate        = NullIfEmpty(SqlServerCertificateTextBox?.Text),
            NetworkProtocol          = SqlServerProtocolComboBox?.SelectedIndex switch
            {
                1 => SqlServerNetworkProtocol.TcpIp,
                2 => SqlServerNetworkProtocol.NamedPipes,
                3 => SqlServerNetworkProtocol.SharedMemory,
                _ => SqlServerNetworkProtocol.Default
            },
            PacketSize               = ParseInt(SqlServerPacketSizeTextBox?.Text, 4096),
            ConnectionTimeout        = ParseInt(SqlServerConnTimeoutTextBox?.Text, 15),
            CommandTimeout           = ParseInt(SqlServerCmdTimeoutTextBox?.Text, 30),
            ApplicationName          = SqlServerAppNameTextBox?.Text?.Trim() ?? "VaultGuard",
            WorkstationId            = NullIfEmpty(SqlServerWorkstationTextBox?.Text),
            MultipleActiveResultSets = SqlServerMarsCheckBox?.IsChecked == true,
            ApplicationIntent        = SqlServerAppIntentComboBox?.SelectedIndex == 1
                                         ? SqlServerApplicationIntent.ReadOnly
                                         : SqlServerApplicationIntent.ReadWrite,
            MultiSubnetFailover      = SqlServerMultiSubnetCheckBox?.IsChecked == true,
            FailoverPartner          = NullIfEmpty(SqlServerFailoverTextBox?.Text),
            Pooling                  = SqlServerPoolingCheckBox?.IsChecked == true,
            MinPoolSize              = ParseInt(SqlServerMinPoolTextBox?.Text, 0),
            MaxPoolSize              = ParseInt(SqlServerMaxPoolTextBox?.Text, 100),
            AdditionalParameters     = NullIfEmpty(SqlServerAdditionalParamsTextBox?.Text)
        };
    }

    private MySqlConfig BuildMySqlConfig()
    {
        return new MySqlConfig
        {
            Host     = MySqlHostTextBox?.Text?.Trim() ?? string.Empty,
            Port     = ParseInt(MySqlPortTextBox?.Text, 3306),
            Database = MySqlDatabaseTextBox?.Text?.Trim() ?? string.Empty,
            Username = MySqlUsernameTextBox?.Text?.Trim() ?? string.Empty,
            // EncryptedPassword populated later
            SslMode  = MySqlSslModeComboBox?.SelectedIndex switch
            {
                0 => MySqlSslMode.None,
                2 => MySqlSslMode.Required,
                3 => MySqlSslMode.VerifyCA,
                4 => MySqlSslMode.VerifyFull,
                _ => MySqlSslMode.Preferred
            },
            SslCaPath         = NullIfEmpty(MySqlSslCaTextBox?.Text),
            SslCertPath       = NullIfEmpty(MySqlSslCertTextBox?.Text),
            SslKeyPath        = NullIfEmpty(MySqlSslKeyTextBox?.Text),
            ConnectionTimeout = ParseInt(MySqlConnTimeoutTextBox?.Text, 30),
            CommandTimeout    = ParseInt(MySqlCmdTimeoutTextBox?.Text, 30),
            CharacterSet      = MySqlCharSetTextBox?.Text?.Trim() ?? "utf8mb4",
            AllowZeroDateTime  = MySqlAllowZeroDateTimeCheckBox?.IsChecked == true,
            AllowUserVariables = MySqlAllowUserVariablesCheckBox?.IsChecked == true,
            Pooling     = MySqlPoolingCheckBox?.IsChecked == true,
            MinPoolSize = ParseInt(MySqlMinPoolTextBox?.Text, 0),
            MaxPoolSize = ParseInt(MySqlMaxPoolTextBox?.Text, 100)
        };
    }

    private PostgreSqlConfig BuildPostgreSqlConfig()
    {
        return new PostgreSqlConfig
        {
            Host     = PgHostTextBox?.Text?.Trim() ?? string.Empty,
            Port     = ParseInt(PgPortTextBox?.Text, 5432),
            Database = PgDatabaseTextBox?.Text?.Trim() ?? string.Empty,
            Username = PgUsernameTextBox?.Text?.Trim() ?? string.Empty,
            // EncryptedPassword populated later
            SslMode  = PgSslModeComboBox?.SelectedIndex switch
            {
                0 => PostgreSqlSslMode.Disable,
                1 => PostgreSqlSslMode.Allow,
                3 => PostgreSqlSslMode.Require,
                4 => PostgreSqlSslMode.VerifyCA,
                5 => PostgreSqlSslMode.VerifyFull,
                _ => PostgreSqlSslMode.Prefer
            },
            SslCertPath       = NullIfEmpty(PgSslCertTextBox?.Text),
            SslKeyPath        = NullIfEmpty(PgSslKeyTextBox?.Text),
            SslRootCertPath   = NullIfEmpty(PgSslRootCertTextBox?.Text),
            ConnectionTimeout = ParseInt(PgConnTimeoutTextBox?.Text, 30),
            CommandTimeout    = ParseInt(PgCmdTimeoutTextBox?.Text, 30),
            ApplicationName   = PgAppNameTextBox?.Text?.Trim() ?? "VaultGuard",
            SearchPath        = NullIfEmpty(PgSearchPathTextBox?.Text),
            Pooling     = PgPoolingCheckBox?.IsChecked == true,
            MinPoolSize = ParseInt(PgMinPoolTextBox?.Text, 1),
            MaxPoolSize = ParseInt(PgMaxPoolTextBox?.Text, 100)
        };
    }

    // ── Password helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Copies existing encrypted passwords into <paramref name="config"/> for any provider
    /// whose plaintext PasswordBox was left blank, so re-saving without re-typing a password
    /// doesn't discard the stored credential.
    /// </summary>
    private void ApplyExistingPasswords(DatabaseConfiguration config)
    {
        if (config.SqlServer != null && string.IsNullOrEmpty(SqlServerPassword))
            config.SqlServer.EncryptedPassword = _existingSqlServerEncryptedPwd;

        if (config.MySql != null && string.IsNullOrEmpty(MySqlPassword))
            config.MySql.EncryptedPassword = _existingMySqlEncryptedPwd;

        if (config.PostgreSql != null && string.IsNullOrEmpty(PostgreSqlPassword))
            config.PostgreSql.EncryptedPassword = _existingPgEncryptedPwd;
    }

    private async System.Threading.Tasks.Task SetEncryptedPasswordsAsync(DatabaseConfiguration config)
    {
        if (config.SqlServer != null && !string.IsNullOrEmpty(SqlServerPassword))
            config.SqlServer.EncryptedPassword = await _databaseConfigService.EncryptPasswordAsync(SqlServerPassword);

        if (config.MySql != null && !string.IsNullOrEmpty(MySqlPassword))
            config.MySql.EncryptedPassword = await _databaseConfigService.EncryptPasswordAsync(MySqlPassword);

        if (config.PostgreSql != null && !string.IsNullOrEmpty(PostgreSqlPassword))
            config.PostgreSql.EncryptedPassword = await _databaseConfigService.EncryptPasswordAsync(PostgreSqlPassword);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateRequiredFields(out string error)
    {
        error = string.Empty;

        switch (SelectedProvider)
        {
            case DatabaseProvider.SqlServer:
                if (string.IsNullOrWhiteSpace(SqlServerHostTextBox?.Text))
                {
                    error = "SQL Server: Server is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(SqlServerDatabaseTextBox?.Text))
                {
                    error = "SQL Server: Database is required.";
                    return false;
                }
                if (SqlServerAuthModeComboBox?.SelectedIndex == 1 &&
                    string.IsNullOrWhiteSpace(SqlServerUsernameTextBox?.Text))
                {
                    error = "SQL Server: Username is required for SQL Server Authentication.";
                    return false;
                }
                break;

            case DatabaseProvider.MySql:
                if (string.IsNullOrWhiteSpace(MySqlHostTextBox?.Text))
                {
                    error = "MySQL: Server is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(MySqlDatabaseTextBox?.Text))
                {
                    error = "MySQL: Database is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(MySqlUsernameTextBox?.Text))
                {
                    error = "MySQL: Username is required.";
                    return false;
                }
                break;

            case DatabaseProvider.PostgreSql:
                if (string.IsNullOrWhiteSpace(PgHostTextBox?.Text))
                {
                    error = "PostgreSQL: Server is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(PgDatabaseTextBox?.Text))
                {
                    error = "PostgreSQL: Database is required.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(PgUsernameTextBox?.Text))
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
        if (StatusText == null) return;

        try
        {
            StatusText.Text = message ?? string.Empty;

            if (neutral || success == null)
                StatusText.Foreground = TryFindResource("ModernTextSecondaryBrush") as Brush ?? SystemColors.GrayTextBrush;
            else if (success == true)
                StatusText.Foreground = TryFindResource("ModernSuccessBrush") as Brush ?? Brushes.Green;
            else
                StatusText.Foreground = TryFindResource("ModernErrorBrush") as Brush ?? Brushes.Red;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"SetStatus error", ex);
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int ParseInt(string? text, int fallback)
        => int.TryParse(text?.Trim(), out int result) ? result : fallback;
}
