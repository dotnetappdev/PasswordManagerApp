import SwiftUI

struct AddItemView: View {
    @Environment(\.dismiss) var dismiss

    var editingItem: PasswordItem?
    var defaultFilter: SidebarSelection?
    var onSave: ((PasswordItem) -> Void)?

    @State private var selectedType: ItemType = .login
    @State private var showTypePicker: Bool = true

    // Common fields
    @State private var title: String = ""
    @State private var notes: String = ""
    @State private var isFavorite: Bool = false
    @State private var selectedVaultId: Int64? = nil
    @State private var selectedCategoryId: Int64? = nil

    // Login
    @State private var username: String = ""
    @State private var password: String = ""
    @State private var website: String = ""

    // Credit card
    @State private var cardholderName: String = ""
    @State private var cardNumber: String = ""
    @State private var cardExpiry: String = ""
    @State private var cardCVV: String = ""

    // Wi-Fi
    @State private var networkName: String = ""
    @State private var securityType: String = "WPA2"

    // Identity
    @State private var firstName: String = ""
    @State private var lastName: String = ""
    @State private var email: String = ""
    @State private var phone: String = ""
    @State private var address: String = ""

    // Bank
    @State private var bankName: String = ""
    @State private var accountNumber: String = ""
    @State private var routingNumber: String = ""

    // Custom fields
    @State private var customFields: [CustomField] = []

    // UI State
    @State private var showPasswordGenerator: Bool = false
    @State private var isLoading: Bool = false
    @State private var errorMessage: String?
    @State private var vaults: [Vault] = []
    @State private var categories: [Category] = []

    private var isEditing: Bool { editingItem != nil }

    var body: some View {
        NavigationView {
            VStack(spacing: 0) {
                if !isEditing && showTypePicker {
                    TypePickerView(selectedType: $selectedType) {
                        withAnimation(.spring(response: 0.3)) {
                            showTypePicker = false
                        }
                    }
                } else {
                    formView
                }
            }
            .navigationTitle(isEditing ? "Edit Item" : (showTypePicker ? "Choose Type" : selectedType.displayName))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }

                if !showTypePicker {
                    ToolbarItem(placement: .confirmationAction) {
                        Button(isEditing ? "Update" : "Save") {
                            saveItem()
                        }
                        .fontWeight(.semibold)
                        .foregroundColor(Color(hex: "7C3AED"))
                        .disabled(title.isEmpty || isLoading)
                    }
                }
            }
        }
        .onAppear {
            loadData()
            if let item = editingItem {
                populateFromItem(item)
            }
        }
    }

    // MARK: - Form View

    private var formView: some View {
        Form {
            // Type badge (when not in type picker mode)
            if !isEditing {
                Section {
                    Button {
                        withAnimation(.spring(response: 0.3)) {
                            showTypePicker = true
                        }
                    } label: {
                        HStack(spacing: 12) {
                            ZStack {
                                RoundedRectangle(cornerRadius: 10)
                                    .fill(selectedType.color.opacity(0.15))
                                    .frame(width: 40, height: 40)
                                Image(systemName: selectedType.systemImage)
                                    .font(.system(size: 18))
                                    .foregroundColor(selectedType.color)
                            }
                            VStack(alignment: .leading, spacing: 2) {
                                Text(selectedType.displayName)
                                    .font(.system(size: 15, weight: .medium))
                                    .foregroundColor(.primary)
                                Text("Tap to change type")
                                    .font(.system(size: 12))
                                    .foregroundColor(.secondary)
                            }
                            Spacer()
                            Image(systemName: "chevron.right")
                                .font(.system(size: 12))
                                .foregroundColor(.secondary)
                        }
                    }
                }
            }

            // Core fields
            Section("Details") {
                HStack {
                    Image(systemName: "textformat")
                        .foregroundColor(Color(hex: "7C3AED"))
                        .frame(width: 24)
                    TextField("Title", text: $title)
                        .font(.system(size: 16, weight: .medium))
                }

                Toggle(isOn: $isFavorite) {
                    HStack {
                        Image(systemName: "star.fill")
                            .foregroundColor(.yellow)
                            .frame(width: 24)
                        Text("Favorite")
                    }
                }
            }

            // Type-specific fields
            typeSpecificSection

            // Notes
            Section {
                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: "note.text")
                        .foregroundColor(Color(hex: "7C3AED"))
                        .frame(width: 24)
                        .padding(.top, 2)
                    TextField("Notes (optional)", text: $notes, axis: .vertical)
                        .lineLimit(3...8)
                }
            } header: {
                Text("Notes")
            }

            // Custom fields
            Section {
                ForEach($customFields) { $field in
                    HStack {
                        VStack(alignment: .leading, spacing: 4) {
                            TextField("Label", text: $field.label)
                                .font(.system(size: 12))
                                .foregroundColor(.secondary)
                            TextField("Value", text: $field.value)
                                .font(.system(size: 15))
                        }

                        Toggle("", isOn: $field.isSecure)
                            .labelsHidden()
                            .tint(Color(hex: "7C3AED"))
                    }
                }
                .onDelete { customFields.remove(atOffsets: $0) }

                Button {
                    customFields.append(CustomField(label: "", value: ""))
                } label: {
                    Label("Add Field", systemImage: "plus.circle.fill")
                        .foregroundColor(Color(hex: "7C3AED"))
                }
            } header: {
                Text("Custom Fields")
            }

            // Organization
            Section("Organization") {
                if !vaults.isEmpty {
                    Picker("Vault", selection: $selectedVaultId) {
                        Text("None").tag(Int64?.none)
                        ForEach(vaults) { vault in
                            Text(vault.name).tag(Optional(vault.id!))
                        }
                    }
                }

                if !categories.isEmpty {
                    Picker("Category", selection: $selectedCategoryId) {
                        Text("None").tag(Int64?.none)
                        ForEach(categories) { category in
                            Text(category.name).tag(Optional(category.id!))
                        }
                    }
                }
            }

            // Error message
            if let error = errorMessage {
                Section {
                    Text(error)
                        .font(.system(size: 13))
                        .foregroundColor(.red)
                }
            }
        }
    }

    // MARK: - Type-Specific Fields

    @ViewBuilder
    private var typeSpecificSection: some View {
        switch selectedType {
        case .login:
            loginSection
        case .creditCard:
            creditCardSection
        case .secureNote:
            EmptyView()
        case .wifi:
            wifiSection
        case .passkey:
            passkeySection
        case .identity:
            identitySection
        case .bankAccount:
            bankAccountSection
        }
    }

    private var loginSection: some View {
        Section("Login") {
            HStack {
                Image(systemName: "person.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Username or email", text: $username)
                    .textContentType(.username)
                    .autocapitalization(.none)
                    .autocorrectionDisabled()
            }

            HStack {
                Image(systemName: "key.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                SecureField("Password", text: $password)
                    .textContentType(.newPassword)

                Button {
                    showPasswordGenerator = true
                } label: {
                    Image(systemName: "wand.and.sparkles")
                        .foregroundColor(Color(hex: "7C3AED"))
                }
            }
            .sheet(isPresented: $showPasswordGenerator) {
                PasswordGeneratorView { generated in
                    password = generated
                }
            }

            HStack {
                Image(systemName: "globe")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Website URL", text: $website)
                    .textContentType(.URL)
                    .keyboardType(.URL)
                    .autocapitalization(.none)
                    .autocorrectionDisabled()
            }
        }
    }

    private var creditCardSection: some View {
        Section("Card Details") {
            HStack {
                Image(systemName: "person.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Cardholder Name", text: $cardholderName)
                    .textContentType(.name)
            }

            HStack {
                Image(systemName: "creditcard.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Card Number", text: $cardNumber)
                    .keyboardType(.numberPad)
                    .textContentType(.creditCardNumber)
            }

            HStack {
                Image(systemName: "calendar")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("MM/YY", text: $cardExpiry)
                    .keyboardType(.numberPad)
            }

            HStack {
                Image(systemName: "lock.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                SecureField("CVV", text: $cardCVV)
                    .keyboardType(.numberPad)
            }
        }
    }

    private var wifiSection: some View {
        Section("Wi-Fi") {
            HStack {
                Image(systemName: "wifi")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Network Name (SSID)", text: $networkName)
                    .autocapitalization(.none)
                    .autocorrectionDisabled()
            }

            HStack {
                Image(systemName: "lock.shield.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                Picker("Security", selection: $securityType) {
                    ForEach(["WPA2", "WPA3", "WEP", "Open"], id: \.self) { Text($0) }
                }
            }

            HStack {
                Image(systemName: "key.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                SecureField("Password", text: $password)
                    .textContentType(.password)
            }
        }
    }

    private var passkeySection: some View {
        Section("Passkey") {
            HStack {
                Image(systemName: "person.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Username", text: $username)
                    .textContentType(.username)
                    .autocapitalization(.none)
                    .autocorrectionDisabled()
            }

            HStack {
                Image(systemName: "globe")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Website", text: $website)
                    .textContentType(.URL)
                    .keyboardType(.URL)
                    .autocapitalization(.none)
                    .autocorrectionDisabled()
            }
        }
    }

    private var identitySection: some View {
        Section("Identity") {
            HStack {
                Image(systemName: "person.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("First Name", text: $firstName)
                    .textContentType(.givenName)
            }

            HStack {
                Image(systemName: "person.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Last Name", text: $lastName)
                    .textContentType(.familyName)
            }

            HStack {
                Image(systemName: "envelope.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Email", text: $email)
                    .textContentType(.emailAddress)
                    .keyboardType(.emailAddress)
                    .autocapitalization(.none)
                    .autocorrectionDisabled()
            }

            HStack {
                Image(systemName: "phone.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Phone", text: $phone)
                    .textContentType(.telephoneNumber)
                    .keyboardType(.phonePad)
            }

            HStack(alignment: .top) {
                Image(systemName: "location.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                    .padding(.top, 2)
                TextField("Address", text: $address, axis: .vertical)
                    .textContentType(.fullStreetAddress)
                    .lineLimit(2...4)
            }
        }
    }

    private var bankAccountSection: some View {
        Section("Bank Account") {
            HStack {
                Image(systemName: "building.columns.fill")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Bank Name", text: $bankName)
            }

            HStack {
                Image(systemName: "number")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                SecureField("Account Number", text: $accountNumber)
                    .keyboardType(.numberPad)
            }

            HStack {
                Image(systemName: "number")
                    .foregroundColor(Color(hex: "7C3AED"))
                    .frame(width: 24)
                TextField("Routing Number", text: $routingNumber)
                    .keyboardType(.numberPad)
            }
        }
    }

    // MARK: - Data Loading

    private func loadData() {
        vaults = (try? VaultService.shared.fetchAll()) ?? []
        categories = (try? CategoryService.shared.fetchAll()) ?? []

        // Set default vault
        if selectedVaultId == nil, let defaultVault = vaults.first(where: { $0.isDefault }) {
            selectedVaultId = defaultVault.id
        }

        // Set filter-based defaults
        if let filter = defaultFilter {
            switch filter {
            case .vault(let id): selectedVaultId = id
            case .category(let id): selectedCategoryId = id
            case .type(let t): selectedType = t
            default: break
            }
        }
    }

    private func populateFromItem(_ item: PasswordItem) {
        selectedType = item.type
        title = item.title
        notes = item.notes ?? ""
        isFavorite = item.isFavorite
        selectedVaultId = item.vaultId
        selectedCategoryId = item.categoryId
        username = item.username ?? ""
        website = item.website ?? ""
        password = item.decryptedPassword ?? ""
        cardholderName = item.cardholderName ?? ""
        cardNumber = item.cardNumber.flatMap { try? EncryptionService.shared.decrypt($0) } ?? ""
        cardExpiry = item.cardExpiry ?? ""
        cardCVV = item.cardCVV.flatMap { try? EncryptionService.shared.decrypt($0) } ?? ""
        networkName = item.networkName ?? ""
        securityType = item.securityType ?? "WPA2"
        firstName = item.firstName ?? ""
        lastName = item.lastName ?? ""
        email = item.email ?? ""
        phone = item.phone ?? ""
        address = item.address ?? ""
        bankName = item.bankName ?? ""
        accountNumber = item.accountNumber.flatMap { try? EncryptionService.shared.decrypt($0) } ?? ""
        routingNumber = item.routingNumber ?? ""
        customFields = item.parsedCustomFields
        showTypePicker = false
    }

    // MARK: - Save

    private func saveItem() {
        guard !title.isEmpty else { return }
        isLoading = true
        errorMessage = nil

        Task {
            do {
                var item = editingItem ?? PasswordItem(
                    title: title,
                    type: selectedType
                )

                // Update fields
                item.title = title
                item.type = selectedType
                item.notes = notes.isEmpty ? nil : notes
                item.isFavorite = isFavorite
                item.vaultId = selectedVaultId
                item.categoryId = selectedCategoryId

                // Type-specific
                switch selectedType {
                case .login:
                    item.username = username.isEmpty ? nil : username
                    item.encryptedPassword = password.isEmpty ? nil : password
                    item.website = website.isEmpty ? nil : website

                case .creditCard:
                    item.cardholderName = cardholderName.isEmpty ? nil : cardholderName
                    item.cardNumber = cardNumber.isEmpty ? nil : cardNumber
                    item.cardExpiry = cardExpiry.isEmpty ? nil : cardExpiry
                    item.cardCVV = cardCVV.isEmpty ? nil : cardCVV

                case .secureNote:
                    break

                case .wifi:
                    item.networkName = networkName.isEmpty ? nil : networkName
                    item.securityType = securityType
                    item.encryptedPassword = password.isEmpty ? nil : password

                case .passkey:
                    item.username = username.isEmpty ? nil : username
                    item.website = website.isEmpty ? nil : website

                case .identity:
                    item.firstName = firstName.isEmpty ? nil : firstName
                    item.lastName = lastName.isEmpty ? nil : lastName
                    item.email = email.isEmpty ? nil : email
                    item.phone = phone.isEmpty ? nil : phone
                    item.address = address.isEmpty ? nil : address

                case .bankAccount:
                    item.bankName = bankName.isEmpty ? nil : bankName
                    item.accountNumber = accountNumber.isEmpty ? nil : accountNumber
                    item.routingNumber = routingNumber.isEmpty ? nil : routingNumber
                }

                // Custom fields
                item.parsedCustomFields = customFields.filter { !$0.label.isEmpty }

                if isEditing {
                    try PasswordItemService.shared.update(&item)
                } else {
                    try PasswordItemService.shared.create(&item)
                }

                await MainActor.run {
                    isLoading = false
                    onSave?(item)
                    dismiss()
                }
            } catch {
                await MainActor.run {
                    isLoading = false
                    errorMessage = error.localizedDescription
                }
            }
        }
    }
}

// MARK: - Password Generator View

struct PasswordGeneratorView: View {
    @Environment(\.dismiss) var dismiss
    var onSelect: (String) -> Void

    @State private var length: Double = 20
    @State private var includeSymbols: Bool = true
    @State private var includeNumbers: Bool = true
    @State private var generatedPassword: String = ""

    var body: some View {
        NavigationView {
            VStack(spacing: 24) {
                // Generated password display
                VStack(spacing: 12) {
                    Text(generatedPassword)
                        .font(.system(size: 18, weight: .medium, design: .monospaced))
                        .foregroundColor(.primary)
                        .multilineTextAlignment(.center)
                        .padding(20)
                        .frame(maxWidth: .infinity)
                        .background(Color(UIColor.secondarySystemBackground))
                        .cornerRadius(16)
                        .onTapGesture {
                            UIPasteboard.general.string = generatedPassword
                        }

                    let strength = EncryptionService.shared.passwordStrength(generatedPassword)
                    HStack(spacing: 8) {
                        ForEach(0..<4) { i in
                            RoundedRectangle(cornerRadius: 2)
                                .fill(i <= strength.rawValue ? strength.color : Color(UIColor.systemGray5))
                                .frame(height: 4)
                        }
                    }

                    Text("Strength: \(strength.label)")
                        .font(.system(size: 13))
                        .foregroundColor(strength.color)
                }
                .padding(.horizontal, 24)

                // Options
                Form {
                    Section {
                        VStack(alignment: .leading, spacing: 8) {
                            HStack {
                                Text("Length")
                                Spacer()
                                Text("\(Int(length))")
                                    .foregroundColor(Color(hex: "7C3AED"))
                                    .fontWeight(.semibold)
                            }
                            Slider(value: $length, in: 8...50, step: 1)
                                .tint(Color(hex: "7C3AED"))
                                .onChange(of: length) { _, _ in regenerate() }
                        }

                        Toggle("Include Symbols (!@#$...)", isOn: $includeSymbols)
                            .tint(Color(hex: "7C3AED"))
                            .onChange(of: includeSymbols) { _, _ in regenerate() }

                        Toggle("Include Numbers (0-9)", isOn: $includeNumbers)
                            .tint(Color(hex: "7C3AED"))
                            .onChange(of: includeNumbers) { _, _ in regenerate() }
                    }
                }

                Spacer()

                // Buttons
                VStack(spacing: 12) {
                    Button {
                        regenerate()
                    } label: {
                        HStack {
                            Image(systemName: "arrow.clockwise")
                            Text("Generate New")
                        }
                        .font(.system(size: 16, weight: .medium))
                        .foregroundColor(Color(hex: "7C3AED"))
                        .frame(maxWidth: .infinity)
                        .frame(height: 50)
                        .background(Color(hex: "7C3AED").opacity(0.1))
                        .cornerRadius(14)
                    }

                    Button {
                        onSelect(generatedPassword)
                        dismiss()
                    } label: {
                        Text("Use This Password")
                            .font(.system(size: 16, weight: .semibold))
                            .foregroundColor(.white)
                            .frame(maxWidth: .infinity)
                            .frame(height: 50)
                            .background(Color(hex: "7C3AED"))
                            .cornerRadius(14)
                    }
                }
                .padding(.horizontal, 24)
                .padding(.bottom, 24)
            }
            .navigationTitle("Password Generator")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
            .onAppear { regenerate() }
        }
    }

    private func regenerate() {
        generatedPassword = EncryptionService.shared.generatePassword(
            length: Int(length),
            includeSymbols: includeSymbols,
            includeNumbers: includeNumbers
        )
    }
}
