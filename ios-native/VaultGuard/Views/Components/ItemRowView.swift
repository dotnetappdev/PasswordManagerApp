import SwiftUI

struct ItemRowView: View {
    let item: PasswordItem

    var body: some View {
        HStack(spacing: 12) {
            // Colored icon badge
            ZStack {
                RoundedRectangle(cornerRadius: 10)
                    .fill(item.type.color.opacity(0.15))
                    .frame(width: 44, height: 44)
                Image(systemName: item.type.systemImage)
                    .font(.system(size: 20))
                    .foregroundColor(item.type.color)
            }

            // Title + subtitle
            VStack(alignment: .leading, spacing: 3) {
                Text(item.title)
                    .font(.system(size: 15, weight: .semibold))
                    .foregroundColor(.primary)
                    .lineLimit(1)

                Text(item.subtitle)
                    .font(.system(size: 13))
                    .foregroundColor(.secondary)
                    .lineLimit(1)
            }

            Spacer()

            if item.isFavorite {
                Image(systemName: "star.fill")
                    .font(.system(size: 11))
                    .foregroundColor(.yellow)
            }
        }
        .padding(.vertical, 4)
    }
}
