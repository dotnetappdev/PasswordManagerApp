// CredentialProviderViewController.swift — VaultGuard's iOS AutoFill Credential Provider.
//
// Lets VaultGuard fill saved logins in Safari and other apps (Settings › General › AutoFill Passwords ›
// VaultGuard). Reads credentials the main app published via AutoFillCredentialStore.
import AuthenticationServices
import SwiftUI

class CredentialProviderViewController: ASCredentialProviderViewController {

    /// QuickType path: iOS asks us to return the tapped credential with no UI. We can, since the credential
    /// set is already in the shared keychain.
    override func provideCredentialWithoutUserInteraction(for credentialIdentity: ASPasswordCredentialIdentity) {
        if let password = AutoFillCredentialStore.password(
            for: credentialIdentity.serviceIdentifier.identifier,
            username: credentialIdentity.user
        ) {
            let credential = ASPasswordCredential(user: credentialIdentity.user, password: password)
            extensionContext.completeRequest(withSelectedCredential: credential, completionHandler: nil)
        } else {
            extensionContext.cancelRequest(
                withError: NSError(domain: ASExtensionErrorDomain, code: ASExtensionError.credentialIdentityNotFound.rawValue)
            )
        }
    }

    /// Fallback when QuickType needs UI (e.g. a specific identity tapped) — show the picker filtered to it.
    override func prepareInterfaceToProvideCredential(for credentialIdentity: ASPasswordCredentialIdentity) {
        showList(filter: credentialIdentity.serviceIdentifier.identifier)
    }

    /// The user opened VaultGuard from the AutoFill menu — show the full, searchable credential list.
    override func prepareCredentialList(for serviceIdentifiers: [ASCredentialServiceIdentifier]) {
        showList(filter: serviceIdentifiers.first?.identifier)
    }

    private func showList(filter: String?) {
        let view = CredentialListView(
            filter: filter,
            onPick: { [weak self] cred in
                let credential = ASPasswordCredential(user: cred.username, password: cred.password)
                self?.extensionContext.completeRequest(withSelectedCredential: credential, completionHandler: nil)
            },
            onCancel: { [weak self] in
                self?.extensionContext.cancelRequest(
                    withError: NSError(domain: ASExtensionErrorDomain, code: ASExtensionError.userCanceled.rawValue)
                )
            }
        )
        let host = UIHostingController(rootView: view)
        addChild(host)
        host.view.frame = self.view.bounds
        host.view.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        self.view.addSubview(host.view)
        host.didMove(toParent: self)
    }
}

private struct CredentialListView: View {
    let filter: String?
    let onPick: (AutoFillCred) -> Void
    let onCancel: () -> Void

    @State private var query = ""

    private var all: [AutoFillCred] { AutoFillCredentialStore.all() }

    private var results: [AutoFillCred] {
        let base = filter.map { f in
            all.filter { $0.identifier.localizedCaseInsensitiveContains(hostPart(f)) }
        } ?? all
        let list = base.isEmpty ? all : base
        guard !query.isEmpty else { return list.sorted { $0.identifier < $1.identifier } }
        return list.filter {
            $0.identifier.localizedCaseInsensitiveContains(query) ||
            $0.username.localizedCaseInsensitiveContains(query)
        }
    }

    var body: some View {
        NavigationView {
            List(results, id: \.username) { cred in
                Button { onPick(cred) } label: {
                    VStack(alignment: .leading, spacing: 2) {
                        Text(cred.identifier).font(.headline)
                        Text(cred.username).font(.subheadline).foregroundStyle(.secondary)
                    }
                }
            }
            .searchable(text: $query, prompt: "Search logins")
            .navigationTitle("VaultGuard")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Cancel", action: onCancel) }
            }
            .overlay {
                if results.isEmpty {
                    ContentUnavailableCompatAF(title: all.isEmpty ? "Unlock VaultGuard to sync logins" : "No matching logins")
                }
            }
        }
    }

    private func hostPart(_ s: String) -> String {
        s.lowercased().replacingOccurrences(of: "https://", with: "")
            .replacingOccurrences(of: "http://", with: "").replacingOccurrences(of: "www.", with: "")
            .split(separator: "/").first.map(String.init) ?? s
    }
}

private struct ContentUnavailableCompatAF: View {
    let title: String
    var body: some View {
        VStack(spacing: 10) {
            Image(systemName: "lock.shield").font(.system(size: 40)).foregroundStyle(.secondary)
            Text(title).foregroundStyle(.secondary).multilineTextAlignment(.center).padding(.horizontal)
        }
    }
}
