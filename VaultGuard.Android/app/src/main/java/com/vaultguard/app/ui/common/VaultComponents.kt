package com.vaultguard.app.ui.common

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Login
import androidx.compose.material.icons.filled.AccountCircle
import androidx.compose.material.icons.filled.CreditCard
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Note
import androidx.compose.material.icons.filled.Security
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.StarBorder
import androidx.compose.material.icons.filled.Wifi
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.model.VaultItem

/** Rounded, tinted icon tile like the item glyphs in the 1Password mobile apps. */
@Composable
fun ItemIconTile(type: ItemType, modifier: Modifier = Modifier) {
    Box(
        modifier = modifier
            .size(42.dp)
            .background(MaterialTheme.colorScheme.primaryContainer, RoundedCornerShape(12.dp)),
        contentAlignment = Alignment.Center,
    ) {
        Icon(
            imageVector = iconFor(type),
            contentDescription = null,
            tint = MaterialTheme.colorScheme.onPrimaryContainer,
            modifier = Modifier.size(22.dp),
        )
    }
}

// A palette of deep, saturated tile colours reminiscent of 1Password's letter tiles.
private val monogramColors = listOf(
    Color(0xFF2D6CDF), Color(0xFF7A5CFF), Color(0xFFEF5DA8), Color(0xFF00A3A3),
    Color(0xFFE8833A), Color(0xFF4CAF50), Color(0xFFD64545), Color(0xFF5C6BC0),
)

fun monogramColor(seed: String): Color {
    if (seed.isEmpty()) return monogramColors[0]
    val idx = (seed.sumOf { it.code } % monogramColors.size)
    return monogramColors[idx]
}

/** A coloured monogram tile (letter/logo style) used for login items, like 1Password's list icons. */
@Composable
fun MonogramTile(item: VaultItem, size: Dp = 42.dp) {
    val useMonogram = item.type == ItemType.Login || item.type == ItemType.Password
    if (!useMonogram) { ItemIconTile(item.type, Modifier.size(size)); return }
    val color = monogramColor(item.website ?: item.title)
    val letter = (item.website ?: item.title).firstOrNull { it.isLetterOrDigit() }?.uppercaseChar()?.toString() ?: "?"
    Box(
        modifier = Modifier.size(size).background(color, RoundedCornerShape(size * 0.28f)),
        contentAlignment = Alignment.Center,
    ) {
        Text(letter, color = Color.White, fontWeight = FontWeight.Bold, fontSize = (size.value * 0.42f).sp)
    }
}

/** A clean, roomy list row: tinted icon tile, bold title, muted subtitle, favourite toggle. */
@Composable
fun VaultItemRow(
    item: VaultItem,
    onClick: () -> Unit,
    onToggleFavorite: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(horizontal = 16.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        MonogramTile(item)
        Column(
            Modifier
                .weight(1f)
                .padding(horizontal = 14.dp),
        ) {
            Text(
                item.title,
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.SemiBold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            val subtitle = item.username ?: item.email ?: item.website ?: item.type.label
            Text(
                subtitle,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
        }
        IconButton(onClick = onToggleFavorite) {
            Icon(
                if (item.isFavorite) Icons.Filled.Star else Icons.Filled.StarBorder,
                contentDescription = "Favourite",
                tint = if (item.isFavorite) MaterialTheme.colorScheme.primary
                else MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

fun iconFor(type: ItemType): ImageVector = when (type) {
    ItemType.CreditCard -> Icons.Filled.CreditCard
    ItemType.SecureNote -> Icons.Filled.Note
    ItemType.WiFi -> Icons.Filled.Wifi
    ItemType.Passkey -> Icons.Filled.Security
    ItemType.Identity -> Icons.Filled.AccountCircle
    ItemType.Login -> Icons.AutoMirrored.Filled.Login
    ItemType.Password -> Icons.Filled.Lock
}
