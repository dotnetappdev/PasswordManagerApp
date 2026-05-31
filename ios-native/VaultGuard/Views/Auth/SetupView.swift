import SwiftUI

struct SetupView: View {
    @EnvironmentObject var appState: AppState
    @State private var masterPassword: String = ""
    @State private var confirmPassword: String = ""
    @State private var showPassword: Bool = false
    @State private var showConfirm: Bool = false
    @State private var isLoading: Bool = false
    @State private var errorMessage: String?
    @State private var currentStep: SetupStep = .welcome

    enum SetupStep {
        case welcome, createPassword, biometrics
    }

    var passwordStrength: PasswordStrength {
        EncryptionService.shared.passwordStrength(masterPassword)
    }

    var passwordsMatch: Bool {
        !confirmPassword.isEmpty && masterPassword == confirmPassword
    }

    var canProceed: Bool {
        masterPassword.count >= 8 && passwordsMatch
    }

    var body: some View {
        ZStack {
            // Background gradient
            LinearGradient(
                colors: [Color(hex: "7C3AED"), Color(hex: "4C1D95")],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

            VStack(spacing: 0) {
                switch currentStep {
                case .welcome:
                    welcomeStep
                case .createPassword:
                    createPasswordStep
                case .biometrics:
                    biometricsStep
                }
            }
        }
    }

    // MARK: - Welcome Step

    private var welcomeStep: some View {
        VStack(spacing: 32) {
            Spacer()

            // App icon
            ZStack {
                RoundedRectangle(cornerRadius: 28)
                    .fill(.white.opacity(0.15))
                    .frame(width: 120, height: 120)
                    .overlay(
                        RoundedRectangle(cornerRadius: 28)
                            .stroke(.white.opacity(0.3), lineWidth: 1)
                    )
                Image(systemName: "lock.shield.fill")
                    .font(.system(size: 56))
                    .foregroundColor(.white)
            }

            VStack(spacing: 12) {
                Text("Welcome to VaultGuard")
                    .font(.system(size: 28, weight: .bold))
                    .foregroundColor(.white)
                    .multilineTextAlignment(.center)

                Text("Your secure, encrypted password manager.\nKeep all your credentials safe in one place.")
                    .font(.system(size: 16))
                    .foregroundColor(.white.opacity(0.8))
                    .multilineTextAlignment(.center)
                    .lineSpacing(4)
            }
            .padding(.horizontal, 32)

            // Feature bullets
            VStack(alignment: .leading, spacing: 16) {
                featureBullet(icon: "lock.fill", text: "AES-256-GCM encryption")
                featureBullet(icon: "faceid", text: "Face ID / Touch ID support")
                featureBullet(icon: "icloud.fill", text: "Optional self-hosted sync")
                featureBullet(icon: "key.fill", text: "Password generator built-in")
            }
            .padding(.horizontal, 40)

            Spacer()

            Button {
                withAnimation(.spring(response: 0.4, dampingFraction: 0.8)) {
                    currentStep = .createPassword
                }
            } label: {
                Text("Get Started")
                    .font(.system(size: 17, weight: .semibold))
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(maxWidth: .infinity)
                    .frame(height: 56)
                    .background(.white)
                    .cornerRadius(16)
            }
            .padding(.horizontal, 24)
            .padding(.bottom, 48)
        }
        .transition(.asymmetric(insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
    }

    private func featureBullet(icon: String, text: String) -> some View {
        HStack(spacing: 12) {
            Image(systemName: icon)
                .font(.system(size: 16))
                .foregroundColor(.white.opacity(0.9))
                .frame(width: 24)
            Text(text)
                .font(.system(size: 15))
                .foregroundColor(.white.opacity(0.9))
        }
    }

    // MARK: - Create Password Step

    private var createPasswordStep: some View {
        ScrollView {
            VStack(spacing: 28) {
                // Header
                VStack(spacing: 8) {
                    Image(systemName: "lock.fill")
                        .font(.system(size: 40))
                        .foregroundColor(.white)
                        .padding(.top, 60)

                    Text("Create Master Password")
                        .font(.system(size: 24, weight: .bold))
                        .foregroundColor(.white)

                    Text("This password encrypts all your data.\nMake it strong and memorable.")
                        .font(.system(size: 14))
                        .foregroundColor(.white.opacity(0.75))
                        .multilineTextAlignment(.center)
                }

                // Form card
                VStack(spacing: 20) {
                    // Password field
                    VStack(alignment: .leading, spacing: 8) {
                        Text("MASTER PASSWORD")
                            .font(.system(size: 11, weight: .semibold))
                            .foregroundColor(Color(hex: "7C3AED"))
                            .tracking(0.5)

                        HStack {
                            Group {
                                if showPassword {
                                    TextField("Enter master password", text: $masterPassword)
                                } else {
                                    SecureField("Enter master password", text: $masterPassword)
                                }
                            }
                            .font(.system(size: 16))
                            .textContentType(.newPassword)
                            .autocapitalization(.none)
                            .autocorrectionDisabled()

                            Button {
                                showPassword.toggle()
                            } label: {
                                Image(systemName: showPassword ? "eye.slash" : "eye")
                                    .foregroundColor(.secondary)
                            }
                        }
                        .padding(14)
                        .background(Color(UIColor.systemBackground))
                        .cornerRadius(12)

                        // Strength indicator
                        if !masterPassword.isEmpty {
                            VStack(alignment: .leading, spacing: 4) {
                                GeometryReader { geo in
                                    ZStack(alignment: .leading) {
                                        RoundedRectangle(cornerRadius: 2)
                                            .fill(Color(UIColor.systemGray5))
                                            .frame(height: 4)
                                        RoundedRectangle(cornerRadius: 2)
                                            .fill(passwordStrength.color)
                                            .frame(width: geo.size.width * passwordStrength.fraction, height: 4)
                                            .animation(.spring(response: 0.3), value: passwordStrength.fraction)
                                    }
                                }
                                .frame(height: 4)

                                Text("Strength: \(passwordStrength.label)")
                                    .font(.system(size: 12))
                                    .foregroundColor(passwordStrength.color)
                            }
                        }
                    }

                    // Confirm password
                    VStack(alignment: .leading, spacing: 8) {
                        Text("CONFIRM PASSWORD")
                            .font(.system(size: 11, weight: .semibold))
                            .foregroundColor(Color(hex: "7C3AED"))
                            .tracking(0.5)

                        HStack {
                            Group {
                                if showConfirm {
                                    TextField("Confirm master password", text: $confirmPassword)
                                } else {
                                    SecureField("Confirm master password", text: $confirmPassword)
                                }
                            }
                            .font(.system(size: 16))
                            .textContentType(.newPassword)
                            .autocapitalization(.none)
                            .autocorrectionDisabled()

                            Button {
                                showConfirm.toggle()
                            } label: {
                                Image(systemName: showConfirm ? "eye.slash" : "eye")
                                    .foregroundColor(.secondary)
                            }
                        }
                        .padding(14)
                        .background(Color(UIColor.systemBackground))
                        .cornerRadius(12)
                        .overlay(
                            RoundedRectangle(cornerRadius: 12)
                                .stroke(
                                    !confirmPassword.isEmpty && !passwordsMatch ? Color.red.opacity(0.5) : Color.clear,
                                    lineWidth: 1
                                )
                        )

                        if !confirmPassword.isEmpty && !passwordsMatch {
                            Text("Passwords do not match")
                                .font(.system(size: 12))
                                .foregroundColor(.red)
                        } else if passwordsMatch {
                            HStack(spacing: 4) {
                                Image(systemName: "checkmark.circle.fill")
                                    .font(.system(size: 12))
                                Text("Passwords match")
                                    .font(.system(size: 12))
                            }
                            .foregroundColor(.green)
                        }
                    }

                    // Warning
                    HStack(alignment: .top, spacing: 8) {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .font(.system(size: 14))
                            .foregroundColor(.orange)
                        Text("There's no way to recover this password if you forget it. Consider writing it down and storing it safely.")
                            .font(.system(size: 12))
                            .foregroundColor(.secondary)
                    }
                    .padding(12)
                    .background(Color.orange.opacity(0.08))
                    .cornerRadius(10)

                    if let error = errorMessage {
                        Text(error)
                            .font(.system(size: 13))
                            .foregroundColor(.red)
                            .padding(10)
                            .background(Color.red.opacity(0.08))
                            .cornerRadius(8)
                    }
                }
                .padding(20)
                .background(Color(UIColor.secondarySystemBackground).opacity(0.5))
                .cornerRadius(20)
                .padding(.horizontal, 20)

                // Create button
                Button {
                    createMasterPassword()
                } label: {
                    Group {
                        if isLoading {
                            ProgressView()
                                .tint(.white)
                        } else {
                            Text("Create Vault")
                                .font(.system(size: 17, weight: .semibold))
                        }
                    }
                    .foregroundColor(.white)
                    .frame(maxWidth: .infinity)
                    .frame(height: 56)
                    .background(canProceed ? Color.white.opacity(0.25) : Color.white.opacity(0.1))
                    .cornerRadius(16)
                    .overlay(
                        RoundedRectangle(cornerRadius: 16)
                            .stroke(.white.opacity(canProceed ? 0.5 : 0.2), lineWidth: 1)
                    )
                }
                .disabled(!canProceed || isLoading)
                .padding(.horizontal, 24)
                .padding(.bottom, 48)
            }
        }
        .transition(.asymmetric(insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
    }

    // MARK: - Biometrics Step

    private var biometricsStep: some View {
        VStack(spacing: 32) {
            Spacer()

            let biometricType = AuthService.shared.biometricType()

            VStack(spacing: 16) {
                Image(systemName: biometricType == .faceID ? "faceid" : "touchid")
                    .font(.system(size: 64))
                    .foregroundColor(.white)
                    .symbolEffect(.pulse)

                Text(biometricType == .faceID ? "Enable Face ID?" : "Enable Touch ID?")
                    .font(.system(size: 24, weight: .bold))
                    .foregroundColor(.white)

                Text("Use \(biometricType == .faceID ? "Face ID" : "Touch ID") to quickly unlock VaultGuard without typing your master password.")
                    .font(.system(size: 15))
                    .foregroundColor(.white.opacity(0.8))
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 32)
            }

            Spacer()

            VStack(spacing: 12) {
                Button {
                    AuthService.shared.isBiometricsEnabled = true
                    finishSetup()
                } label: {
                    Text("Enable Biometrics")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundColor(Color(hex: "7C3AED"))
                        .frame(maxWidth: .infinity)
                        .frame(height: 56)
                        .background(.white)
                        .cornerRadius(16)
                }

                Button {
                    finishSetup()
                } label: {
                    Text("Skip for Now")
                        .font(.system(size: 17))
                        .foregroundColor(.white.opacity(0.8))
                        .frame(height: 44)
                }
            }
            .padding(.horizontal, 24)
            .padding(.bottom, 48)
        }
        .transition(.asymmetric(insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
    }

    // MARK: - Actions

    private func createMasterPassword() {
        isLoading = true
        errorMessage = nil

        Task {
            do {
                try AuthService.shared.setupMasterPassword(masterPassword)
                await MainActor.run {
                    isLoading = false
                    withAnimation {
                        currentStep = .biometrics
                    }
                }
            } catch {
                await MainActor.run {
                    isLoading = false
                    errorMessage = error.localizedDescription
                }
            }
        }
    }

    private func finishSetup() {
        withAnimation {
            appState.isFirstLaunch = false
            appState.unlockApp()
        }
    }
}

#Preview {
    SetupView()
        .environmentObject(AppState())
}
