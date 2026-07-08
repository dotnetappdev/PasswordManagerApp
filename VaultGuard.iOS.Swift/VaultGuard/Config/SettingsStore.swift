// SettingsStore.swift — full mirror of the WPF Settings surface, persisted in UserDefaults.
import Foundation
import SwiftUI

enum AppTheme: String, Codable, CaseIterable { case system, light, dark, highContrast }
enum DefaultView: String, Codable, CaseIterable { case dashboard, allItems, favorites }

struct AppSettings: Codable {
    // Appearance
    var theme: AppTheme = .system
    var defaultView: DefaultView = .allItems
    var showItemCount = true
    var animateTransitions = true
    // Accessibility
    var uiZoom: Double = 1.0
    var fontSizePt: Int = 14
    var scaleMenu: Double = 1.0
    var scaleQuickActions: Double = 1.0
    var scaleDetails: Double = 1.0
    var scaleDialogs: Double = 1.0
    var scaleGlobal: Double = 1.0
    var scaleCardIcons: Double = 1.0
    var reduceMotion = false
    var highContrast = false
    // Security
    var requirePasscodeOnLaunch = true
    var biometricUnlock = false
    var autoLockMinutes: Int = 5
    var clipboardClearSeconds: Int = 30
    var confirmOnDelete = true
    // Password generator
    var pwLength: Int = 20
    var pwUpper = true
    var pwLower = true
    var pwDigits = true
    var pwSymbols = true
    var pwAvoidAmbiguous = false

    var colorScheme: ColorScheme? {
        switch theme {
        case .system: return nil
        case .light: return .light
        case .dark, .highContrast: return .dark
        }
    }
}

@MainActor
final class SettingsStore: ObservableObject {
    @Published var settings: AppSettings { didSet { persist() } }

    private let key = "app_settings"
    private let defaults = UserDefaults.standard

    init() {
        if let data = defaults.data(forKey: key),
           let decoded = try? JSONDecoder().decode(AppSettings.self, from: data) {
            settings = decoded
        } else {
            settings = AppSettings()
        }
    }

    private func persist() {
        if let data = try? JSONEncoder().encode(settings) {
            defaults.set(data, forKey: key)
        }
    }

    func update(_ mutate: (inout AppSettings) -> Void) {
        var copy = settings
        mutate(&copy)
        settings = copy
    }
}
