import SwiftUI

struct TypePickerView: View {
    @Binding var selectedType: ItemType
    var onSelect: () -> Void

    let columns = [
        GridItem(.flexible()),
        GridItem(.flexible()),
        GridItem(.flexible()),
    ]

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 24) {
                Text("What would you like to save?")
                    .font(.system(size: 17, weight: .semibold))
                    .foregroundColor(.primary)
                    .padding(.horizontal, 24)
                    .padding(.top, 8)

                LazyVGrid(columns: columns, spacing: 16) {
                    ForEach(ItemType.allCases, id: \.self) { type in
                        TypePickerCell(type: type, isSelected: selectedType == type) {
                            selectedType = type
                            onSelect()
                        }
                    }
                }
                .padding(.horizontal, 16)

                Text("Your data is encrypted end-to-end with AES-256-GCM.")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
                    .multilineTextAlignment(.center)
                    .frame(maxWidth: .infinity)
                    .padding(.horizontal, 24)
                    .padding(.bottom, 24)
            }
        }
    }
}

// MARK: - Type Picker Cell

private struct TypePickerCell: View {
    let type: ItemType
    let isSelected: Bool
    let onTap: () -> Void

    var body: some View {
        Button(action: onTap) {
            VStack(spacing: 10) {
                ZStack {
                    RoundedRectangle(cornerRadius: 16)
                        .fill(type.color.opacity(isSelected ? 0.25 : 0.12))
                        .frame(width: 64, height: 64)
                        .overlay(
                            RoundedRectangle(cornerRadius: 16)
                                .stroke(isSelected ? type.color : Color.clear, lineWidth: 2)
                        )
                    Image(systemName: type.systemImage)
                        .font(.system(size: 26))
                        .foregroundColor(type.color)
                }

                Text(type.displayName)
                    .font(.system(size: 12, weight: .medium))
                    .foregroundColor(.primary)
                    .multilineTextAlignment(.center)
                    .lineLimit(2)
                    .fixedSize(horizontal: false, vertical: true)
            }
            .frame(maxWidth: .infinity)
            .padding(.vertical, 12)
            .background(
                RoundedRectangle(cornerRadius: 16)
                    .fill(Color(UIColor.secondarySystemBackground))
            )
        }
        .buttonStyle(.plain)
        .scaleEffect(isSelected ? 0.96 : 1.0)
        .animation(.spring(response: 0.2), value: isSelected)
    }
}
