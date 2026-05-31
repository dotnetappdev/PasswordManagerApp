import SwiftUI
import LocalAuthentication

struct LoginView: View {
    @EnvironmentObject var appState: AppState
    @State private var masterPassword: String = ""
    @State private var showPassword: Bool = false
    @State private var isLoading: Bool = false
    @State private var errorMessage: String?
    @State private var attempts: Int = 0
    @State private var isShaking: Bool = false

    private let maxAttempts = 5

    var biometricType: LABiometryType {
        AuthService.shared.biometricType()
    }

    var showBiometricButton: Bool {
        AuthService.shared.isBiometricsEnabled && biometricType != .none
    }

    var body: some View {
        ZStack {
            // Background gradient
            LinearGradient(
                colors: [Color(hex: "4C1D95"), Color(hex: "7C3AED"), Color(hex: "9333EA")],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

            // Decorative circles
            ZStack {
                Circle()
                    .fill(.white.opacity(0.04))
                    .frame(width: 400, height: 400)
                    .offset(x: -100, y: -200)

                Circle()
                    .fill(.white.opacity(0.04))
                    .frame(width: 300, height: 300)
                    .offset(x: 150, y: 300)
            }

            VStack(spacing: 0) {
                Spacer()

                // Logo
                VStack(spacing: 16) {
                    ZStack {
                        RoundedRectangle(cornerRadius: 24)
                            .fill(.white.opacity(0.15))
                            .frame(width: 96, height: 96)
                            .overlay(
                                RoundedRectangle(cornerRadius: 24)
                                    .stroke(.white.opacity(0.25), lineWidth: 1)
                            )
                        Image(systemName: "lock.shield.fill")
                            .font(.system(size: 44))
                            .foregroundColor(.white)
                    }

                    Text("VaultGuard")
                        .font(.system(size: 32, weight: .bold))
                        .foregroundColor(.white)

                    Text("Enter your master password to unlock")
                        .font(.system(size: 14))
                        .foregroundColor(.white.opacity(0.7))
                }

                Spacer()
                    .frame(height: 48)

                // Login form
                VStack(spacing: 20) {
                    // Password field
                    VStack(alignment: .leading, spacing: 8) {
                        HStack {
                            Group {
                                if showPassword {
                                    TextField("Master password", text: $masterPassword)
                                } else {
                                    SecureField("Master password", text: $masterPassword)
                                }
                            }
                            .font(.system(size: 17))
                            .textContentType(.password)
                            .autocapitalization(.none)
                            .autocorrectionDisabled()
                            .submitLabel(.go)
                            .onSubmit { authenticate() }

                            Button {
                                showPassword.toggle()
                            } label: {
                                Image(systemName: showPassword ? "eye.slash.fill" : "eye.fill")
                                    .foregroundColor(.white.opacity(0.6))
                                    .font(.system(size: 16))
                            }
                        }
                        .padding(16)
                        .background(.white.opacity(0.15))
                        .cornerRadius(14)
                        .overlay(
                            RoundedRectangle(cornerRadius: 14)
                                .stroke(.white.opacity(0.25), lineWidth: 1)
                        )
                        .offset(x: isShaking ? 6 : 0)
                        .animation(
                            isShaking ? .linear(duration: 0.05).repeatCount(6, autoreverses: true) : .default,
                            value: isShaking
                        )
                    }

                    // Error message
                    if let error = errorMessage {
                        HStack(spacing: 6) {
                            Image(systemName: "exclamationmark.circle.fill")
                                .font(.system(size: 13))
                            Text(error)
                                .font(.system(size: 13))
                        }
                        .foregroundColor(.red.opacity(0.9))
                        .padding(10)
                        .background(Color.red.opacity(0.15))
                        .cornerRadius(10)
                        .overlay(
                            RoundedRectangle(cornerRadius: 10)
                                .stroke(Color.red.opacity(0.3), lineWidth: 1)
                        )
                    }

                    // Attempts warning
                    if attempts > 0 {
                        Text("\(maxAttempts - attempts) attempt\(maxAttempts - attempts == 1 ? "" : "s") remaining")
                            .font(.system(size: 12))
                            .foregroundColor(.orange)
                    }

                    // Unlock button
                    Button {
                        authenticate()
                    } label: {
                        Group {
                            if isLoading {
                                ProgressView()
                                    .tint(Color(hex: "7C3AED"))
                            } else {
                                HStack(spacing: 8) {
                                    Image(systemName: "lock.open.fill")
                                        .font(.system(size: 16))
                                    Text("Unlock")
                                        .font(.system(size: 17, weight: .semibold))
                                }
                            }
                        }
                        .foregroundColor(Color(hex: "7C3AED"))
                        .frame(maxWidth: .infinity)
                        .frame(height: 56)
                        .background(.white)
                        .cornerRadius(14)
                    }
                    .disabled(masterPassword.isEmpty || isLoading)
                    .opacity(masterPassword.isEmpty ? 0.7 : 1.0)
                }
                .padding(.horizontal, 28)

                Spacer()
                    .frame(height: 32)

                // Biometrics button
                if showBiometricButton {
                    Button {
                        authenticateWithBiometrics()
                    } label: {
                        VStack(spacing: 8) {
                            Image(systemName: biometricType == .faceID ? "faceid" : "touchid")
                                .font(.system(size: 36))
                                .foregroundColor(.white.opacity(0.9))

                            Text("Use \(biometricType == .faceID ? "Face ID" : "Touch ID")")
                                .font(.system(size: 14))
                                .foregroundColor(.white.opacity(0.7))
                        }
                        .padding(.vertical, 16)
                        .frame(maxWidth: .infinity)
                    }
                    .padding(.horizontal, 28)
                }

                Spacer()
                    .frame(height: 48)
            }
        }
        .onAppear {
            if showBiometricButton {
                authenticateWithBiometrics()
            }
        }
    }

    // MARK: - Actions

    private func authenticate() {
        guard !masterPassword.isEmpty else { return }
        guard attempts < maxAttempts else {
            errorMessage = "Too many failed attempts. Please wait."
            return
        }

        isLoading = true
        errorMessage = nil

        Task {
            do {
                let success = try await Task.detached(priority: .userInitiated) {
                    try AuthService.shared.verifyMasterPassword(self.masterPassword)
                }.value

                await MainActor.run {
                    isLoading = false
                    if success {
                        appState.unlockApp()
                    } else {
                        attempts += 1
                        errorMessage = "Incorrect password. Please try again."
                        triggerShake()
                        masterPassword = ""
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

    private func authenticateWithBiometrics() {
        Task {
            do {
                let success = try await AuthService.shared.authenticateWithBiometrics()
                await MainActor.run {
                    if success {
                        appState.unlockApp()
                    }
                }
            } catch AuthError.userCancelled {
                // User cancelled — no error
                break
            } catch {
                await MainActor.run {
                    errorMessage = error.localizedDescription
                }
            }
        }
    }

    private func triggerShake() {
        isShaking = true
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) {
            isShaking = false
        }
    }
}

#Preview {
    LoginView()
        .environmentObject(AppState())
}
