// Components.swift — reusable list row + icon tile styled like the 1Password mobile apps.
import SwiftUI

struct ItemIconTile: View {
    let type: ItemType
    var size: CGFloat = 40

    var body: some View {
        RoundedRectangle(cornerRadius: size * 0.28, style: .continuous)
            .fill(Theme.accent.opacity(0.15))
            .frame(width: size, height: size)
            .overlay(
                Image(systemName: type.systemImage)
                    .font(.system(size: size * 0.5))
                    .foregroundStyle(Theme.accent)
            )
    }
}

struct VaultItemRow: View {
    let item: VaultItem
    var onToggleFavorite: () -> Void

    var body: some View {
        HStack(spacing: 14) {
            ItemIconTile(type: item.type)
            VStack(alignment: .leading, spacing: 2) {
                Text(item.title).font(.body.weight(.semibold)).lineLimit(1)
                Text(subtitle).font(.subheadline).foregroundStyle(.secondary).lineLimit(1)
            }
            Spacer()
            Button(action: onToggleFavorite) {
                Image(systemName: item.isFavorite ? "star.fill" : "star")
                    .foregroundStyle(item.isFavorite ? Theme.accent : Color.secondary)
            }
            .buttonStyle(.plain)
        }
        .padding(.vertical, 4)
    }

    private var subtitle: String {
        item.username ?? item.email ?? item.website ?? item.type.label
    }
}

/// Simple strength meter used in the editor.
struct StrengthMeter: View {
    let strength: Int // 0..4

    private var color: Color {
        switch strength {
        case 0, 1: return .red
        case 2: return .orange
        case 3: return .yellow
        default: return .green
        }
    }
    private var label: String {
        switch strength {
        case 0, 1: return "Weak"
        case 2: return "Fair"
        case 3: return "Good"
        default: return "Strong"
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    Capsule().fill(Color.secondary.opacity(0.2))
                    Capsule().fill(color).frame(width: geo.size.width * CGFloat(strength) / 4)
                }
            }
            .frame(height: 6)
            Text(label).font(.caption2).foregroundStyle(color)
        }
    }
}
