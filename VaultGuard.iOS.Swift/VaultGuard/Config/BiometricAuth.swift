// BiometricAuth.swift — availability check only. The actual gated release of secrets happens via
// Keychain's kSecAttrAccessControl(.biometryCurrentSet), not here — this just decides whether to
// show quick-unlock UI at all.
import LocalAuthentication

enum BiometricAuth {
    static func isAvailable() -> Bool {
        var error: NSError?
        return LAContext().canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error)
    }

    /// "Face ID" / "Touch ID" / "biometrics" — for button and settings copy.
    static var displayName: String {
        let context = LAContext()
        _ = context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: nil)
        switch context.biometryType {
        case .faceID: return "Face ID"
        case .touchID: return "Touch ID"
        default: return "biometrics"
        }
    }
}
