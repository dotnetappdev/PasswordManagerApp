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

/// 1Password-style quick-access picker: category filter + search, results grouped by the month
/// the item was added (newest first) — mirrors QuickAccessView in the main app.
private struct CredentialListView: View {
    let filter: String?
    let onPick: (AutoFillCred) -> Void
    let onCancel: () -> Void

    @State private var query = ""
    @State private var categoryFilter: String?

    private var all: [AutoFillCred] { AutoFillCredentialStore.all() }

    private var categories: [String] {
        Set(all.compactMap { $0.categoryName }).sorted()
    }

    private var results: [AutoFillCred] {
        let base = filter.map { f in
            all.filter { $0.identifier.localizedCaseInsensitiveContains(hostPart(f)) }
        } ?? all
        let scoped = base.isEmpty ? all : base
        return scoped
            .filter { categoryFilter == nil || $0.categoryName == categoryFilter }
            .filter {
                query.isEmpty ||
                $0.identifier.localizedCaseInsensitiveContains(query) ||
                $0.username.localizedCaseInsensitiveContains(query) ||
                $0.title.localizedCaseInsensitiveContains(query)
            }
    }

    private static let monthFormat: DateFormatter = {
        let f = DateFormatter()
        f.dateFormat = "MMMM yyyy"
        return f
    }()

    private func monthKey(_ cred: AutoFillCred) -> String {
        cred.createdAt > 0
            ? Self.monthFormat.string(from: Date(timeIntervalSince1970: cred.createdAt)).uppercased()
            : "UNDATED"
    }

    private var groupedKeys: [String] {
        var seen: [String] = []
        for cred in results.sorted(by: { $0.createdAt > $1.createdAt }) {
            let key = monthKey(cred)
            if !seen.contains(key) { seen.append(key) }
        }
        return seen
    }

    private var grouped: [String: [AutoFillCred]] {
        Dictionary(grouping: results.sorted { $0.createdAt > $1.createdAt }, by: monthKey)
    }

    var body: some View {
        NavigationView {
            List {
                ForEach(groupedKeys, id: \.self) { month in
                    Section(header: Text(month)) {
                        ForEach(grouped[month] ?? [], id: \.username) { cred in
                            Button { onPick(cred) } label: {
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(cred.title.isEmpty ? cred.identifier : cred.title).font(.headline)
                                    Text(cred.username).font(.subheadline).foregroundStyle(.secondary)
                                }
                            }
                        }
                    }
                }
            }
            .searchable(text: $query, prompt: "Search logins")
            .navigationTitle("VaultGuard")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Cancel", action: onCancel) }
                if !categories.isEmpty {
                    ToolbarItem(placement: .navigationBarTrailing) {
                        Menu {
                            Button {
                                categoryFilter = nil
                            } label: {
                                if categoryFilter == nil { Label("All Categories", systemImage: "checkmark") }
                                else { Text("All Categories") }
                            }
                            ForEach(categories, id: \.self) { cat in
                                Button {
                                    categoryFilter = cat
                                } label: {
                                    if categoryFilter == cat { Label(cat, systemImage: "checkmark") }
                                    else { Text(cat) }
                                }
                            }
                        } label: {
                            Image(systemName: "line.3.horizontal.decrease.circle")
                        }
                    }
                }
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
