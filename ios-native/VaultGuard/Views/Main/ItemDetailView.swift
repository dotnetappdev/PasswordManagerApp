import SwiftUI

struct ItemDetailView: View {
    let item: PasswordItem
    @Binding var selectedItem: PasswordItem?

    @State private var showEdit: Bool = false
    @State private var copiedField: String?
    @State private var currentItem: PasswordItem

    init(item: PasswordItem, selectedItem: Binding<PasswordItem?>) {
        self.item = item
        self._selectedItem = selectedItem
        self._currentItem = State(initialValue: item)
    }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // MARK: - Header

                itemHeader

                // MARK: - Fields Card

                VStack(spacing: 0) {
                    switch currentItem.type {
                    case .login:
                        loginFields
                    case .creditCard:
                        creditCardFields
                    case .secureNote:
                        secureNoteFields
                    case .wifi:
                        wifiFields
                    case .passkey:
                        passkeyFields
                    case .identity:
                        identityFields
                    case .bankAccount:
                        bankAccountFields
                    }

                    // Common fields
                    if let notes = currentItem.notes, !notes.isEmpty {
                        Divider().padding(.horizontal, 20)
                        FieldRowView(label: "NOTES", value: notes, isCopyable: true) { copy(notes, field: "NOTES") }
                    }

                    // Custom fields
                    if !currentItem.parsedCustomFields.isEmpty {
                        Divider().padding(.horizontal, 20)
                        ForEach(currentItem.parsedCustomFields) { field in
                            if field.isSecure {
                                PasswordFieldRowView(
                                    label: field.label.uppercased(),
                                    value: field.value
                                ) { copy(field.value, field: field.label) }
                            } else {
                                FieldRowView(
                                    label: field.label.uppercased(),
                                    value: field.value,
                                    isCopyable: true
                                ) { copy(field.value, field: field.label) }
                            }
                            if field.id != currentItem.parsedCustomFields.last?.id {
                                Divider().padding(.horizontal, 20)
                            }
                        }
                    }
                }
                .background(Color(UIColor.secondarySystemBackground))
                .cornerRadius(16)
                .padding(.horizontal, 20)
                .padding(.top, 20)

                // MARK: - Metadata

                metadataSection
                    .padding(.horizontal, 20)
                    .padding(.top, 20)

                // MARK: - Danger Zone

                dangerZone
                    .padding(.horizontal, 20)
                    .padding(.top, 20)
                    .padding(.bottom, 40)
            }
        }
        .background(Color(UIColor.systemBackground))
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItemGroup(placement: .navigationBarTrailing) {
                Button {
                    toggleFavorite()
                } label: {
                    Image(systemName: currentItem.isFavorite ? "star.fill" : "star")
                        .foregroundColor(currentItem.isFavorite ? .yellow : Color(hex: "7C3AED"))
                }

                Button("Edit") {
                    showEdit = true
                }
                .fontWeight(.semibold)
                .foregroundColor(Color(hex: "7C3AED"))
            }
        }
        .sheet(isPresented: $showEdit) {
            AddItemView(editingItem: currentItem) { updated in
                currentItem = updated
                selectedItem = updated
            }
        }
        .onChange(of: item) { _, newItem in
            currentItem = newItem
        }
        // Copied toast overlay
        .overlay(alignment: .bottom) {
            if let field = copiedField {
                HStack(spacing: 8) {
                    Image(systemName: "checkmark.circle.fill")
                        .foregroundColor(.green)
                    Text("\(field) copied")
                        .font(.system(size: 14, weight: .medium))
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 10)
                .background(.ultraThinMaterial)
                .cornerRadius(20)
                .shadow(color: .black.opacity(0.15), radius: 8, y: 4)
                .padding(.bottom, 24)
                .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .animation(.spring(response: 0.3), value: copiedField)
    }

    // MARK: - Header

    private var itemHeader: some View {
        HStack(spacing: 16) {
            ZStack {
                RoundedRectangle(cornerRadius: 18)
                    .fill(currentItem.type.color.opacity(0.15))
                    .frame(width: 72, height: 72)
                    .overlay(
                        RoundedRectangle(cornerRadius: 18)
                            .stroke(currentItem.type.color.opacity(0.3), lineWidth: 1)
                    )
                Image(systemName: currentItem.type.systemImage)
                    .font(.system(size: 32))
                    .foregroundColor(currentItem.type.color)
            }

            VStack(alignment: .leading, spacing: 4) {
                Text(currentItem.title)
                    .font(.system(size: 22, weight: .bold))
                    .lineLimit(2)

                Text(currentItem.type.displayName)
                    .font(.system(size: 14))
                    .foregroundColor(.secondary)

                if currentItem.isFavorite {
                    HStack(spacing: 4) {
                        Image(systemName: "star.fill")
                            .font(.system(size: 10))
                        Text("Favorite")
                            .font(.system(size: 12))
                    }
                    .foregroundColor(.yellow)
                }
            }

            Spacer()
        }
        .padding(20)
    }

    // MARK: - Login Fields

    private var loginFields: some View {
        VStack(spacing: 0) {
            if let username = currentItem.username {
                FieldRowView(label: "USERNAME", value: username, isCopyable: true) {
                    copy(username, field: "Username")
                }
                Divider().padding(.horizontal, 20)
            }

            if let password = currentItem.decryptedPassword {
                PasswordFieldRowView(label: "PASSWORD", value: password) {
                    copy(password, field: "Password")
                }
                Divider().padding(.horizontal, 20)
            }

            if let website = currentItem.website {
                FieldRowView(label: "WEBSITE", value: website, isURL: true, isCopyable: true) {
                    copy(website, field: "Website")
                }
            }
        }
    }

    // MARK: - Credit Card Fields

    private var creditCardFields: some View {
        VStack(spacing: 0) {
            if let holder = currentItem.cardholderName {
                FieldRowView(label: "CARDHOLDER NAME", value: holder, isCopyable: true) {
                    copy(holder, field: "Cardholder Name")
                }
                Divider().padding(.horizontal, 20)
            }

            if let cardNum = currentItem.cardNumber {
                let display = (try? EncryptionService.shared.decrypt(cardNum)) ?? cardNum
                PasswordFieldRowView(label: "CARD NUMBER", value: display) {
                    copy(display, field: "Card Number")
                }
                Divider().padding(.horizontal, 20)
            }

            if let expiry = currentItem.cardExpiry {
                FieldRowView(label: "EXPIRY DATE", value: expiry, isCopyable: true) {
                    copy(expiry, field: "Expiry Date")
                }
                Divider().padding(.horizontal, 20)
            }

            if let cvv = currentItem.cardCVV {
                let display = (try? EncryptionService.shared.decrypt(cvv)) ?? cvv
                PasswordFieldRowView(label: "SECURITY CODE", value: display) {
                    copy(display, field: "CVV")
                }
            }
        }
    }

    // MARK: - Secure Note Fields

    private var secureNoteFields: some View {
        VStack(spacing: 0) {
            if let notes = currentItem.notes {
                VStack(alignment: .leading, spacing: 8) {
                    Text("NOTE")
                        .font(.system(size: 11, weight: .semibold))
                        .foregroundColor(Color(hex: "7C3AED"))
                        .tracking(0.5)

                    Text(notes)
                        .font(.system(size: 15))
                        .foregroundColor(.primary)
                        .textSelection(.enabled)

                    HStack {
                        Spacer()
                        Button {
                            copy(notes, field: "Note")
                        } label: {
                            Image(systemName: "doc.on.doc")
                                .font(.system(size: 14))
                                .foregroundColor(Color(hex: "7C3AED"))
                        }
                    }
                }
                .padding(20)
            }
        }
    }

    // MARK: - Wi-Fi Fields

    private var wifiFields: some View {
        VStack(spacing: 0) {
            if let network = currentItem.networkName {
                FieldRowView(label: "NETWORK NAME", value: network, isCopyable: true) {
                    copy(network, field: "Network Name")
                }
                Divider().padding(.horizontal, 20)
            }

            if let security = currentItem.securityType {
                FieldRowView(label: "SECURITY", value: security, isCopyable: false) { }
                Divider().padding(.horizontal, 20)
            }

            if let password = currentItem.decryptedPassword {
                PasswordFieldRowView(label: "PASSWORD", value: password) {
                    copy(password, field: "Password")
                }
            }
        }
    }

    // MARK: - Passkey Fields

    private var passkeyFields: some View {
        VStack(spacing: 0) {
            if let username = currentItem.username {
                FieldRowView(label: "USERNAME", value: username, isCopyable: true) {
                    copy(username, field: "Username")
                }
                Divider().padding(.horizontal, 20)
            }

            if let website = currentItem.website {
                FieldRowView(label: "WEBSITE", value: website, isURL: true, isCopyable: true) {
                    copy(website, field: "Website")
                }
            }
        }
    }

    // MARK: - Identity Fields

    private var identityFields: some View {
        VStack(spacing: 0) {
            let name = [currentItem.firstName, currentItem.lastName].compactMap { $0 }.joined(separator: " ")
            if !name.isEmpty {
                FieldRowView(label: "FULL NAME", value: name, isCopyable: true) {
                    copy(name, field: "Name")
                }
                Divider().padding(.horizontal, 20)
            }

            if let email = currentItem.email {
                FieldRowView(label: "EMAIL", value: email, isCopyable: true) {
                    copy(email, field: "Email")
                }
                Divider().padding(.horizontal, 20)
            }

            if let phone = currentItem.phone {
                FieldRowView(label: "PHONE", value: phone, isCopyable: true) {
                    copy(phone, field: "Phone")
                }
                Divider().padding(.horizontal, 20)
            }

            if let address = currentItem.address {
                FieldRowView(label: "ADDRESS", value: address, isCopyable: true) {
                    copy(address, field: "Address")
                }
            }
        }
    }

    // MARK: - Bank Account Fields

    private var bankAccountFields: some View {
        VStack(spacing: 0) {
            if let bank = currentItem.bankName {
                FieldRowView(label: "BANK", value: bank, isCopyable: true) {
                    copy(bank, field: "Bank")
                }
                Divider().padding(.horizontal, 20)
            }

            if let acct = currentItem.accountNumber {
                let display = (try? EncryptionService.shared.decrypt(acct)) ?? acct
                PasswordFieldRowView(label: "ACCOUNT NUMBER", value: display) {
                    copy(display, field: "Account Number")
                }
                Divider().padding(.horizontal, 20)
            }

            if let routing = currentItem.routingNumber {
                FieldRowView(label: "ROUTING NUMBER", value: routing, isCopyable: true) {
                    copy(routing, field: "Routing Number")
                }
            }
        }
    }

    // MARK: - Metadata Section

    private var metadataSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("INFORMATION")
                .font(.system(size: 11, weight: .semibold))
                .foregroundColor(.secondary)
                .tracking(0.5)

            VStack(spacing: 0) {
                metaRow(label: "Created", value: formatted(currentItem.createdAt))
                Divider().padding(.horizontal, 16)
                metaRow(label: "Modified", value: formatted(currentItem.updatedAt))

                if let syncId = currentItem.syncId {
                    Divider().padding(.horizontal, 16)
                    metaRow(label: "Sync ID", value: String(syncId.prefix(8)) + "...")
                }
            }
            .background(Color(UIColor.secondarySystemBackground))
            .cornerRadius(12)
        }
    }

    private func metaRow(label: String, value: String) -> some View {
        HStack {
            Text(label)
                .font(.system(size: 14))
                .foregroundColor(.secondary)
            Spacer()
            Text(value)
                .font(.system(size: 14))
                .foregroundColor(.primary)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 12)
    }

    // MARK: - Danger Zone

    private var dangerZone: some View {
        VStack(alignment: .leading, spacing: 12) {
            Button {
                withAnimation {
                    try? PasswordItemService.shared.softDelete(currentItem)
                    selectedItem = nil
                }
            } label: {
                HStack {
                    Image(systemName: "trash.fill")
                    Text("Move to Trash")
                }
                .font(.system(size: 15, weight: .medium))
                .foregroundColor(.red)
                .frame(maxWidth: .infinity)
                .frame(height: 48)
                .background(Color.red.opacity(0.08))
                .cornerRadius(12)
            }
        }
    }

    // MARK: - Helpers

    private func copy(_ value: String, field: String) {
        UIPasteboard.general.string = value
        withAnimation {
            copiedField = field
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
            withAnimation {
                if copiedField == field { copiedField = nil }
            }
        }
    }

    private func toggleFavorite() {
        try? PasswordItemService.shared.toggleFavorite(currentItem)
        if let updated = try? PasswordItemService.shared.fetchById(currentItem.id!) {
            currentItem = updated
            selectedItem = updated
        }
    }

    private func formatted(_ date: Date) -> String {
        let f = DateFormatter()
        f.dateStyle = .medium
        f.timeStyle = .short
        return f.string(from: date)
    }
}
