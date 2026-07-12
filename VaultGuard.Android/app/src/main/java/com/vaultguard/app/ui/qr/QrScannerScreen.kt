package com.vaultguard.app.ui.qr

import android.Manifest
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.ExperimentalGetImage
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PhotoLibrary
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.google.accompanist.permissions.ExperimentalPermissionsApi
import com.google.accompanist.permissions.isGranted
import com.google.accompanist.permissions.rememberPermissionState
import com.google.mlkit.vision.barcode.BarcodeScanner
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.common.InputImage
import com.vaultguard.app.ui.theme.VgAccent
import java.util.concurrent.Executors

@OptIn(ExperimentalPermissionsApi::class)
@Composable
fun QrScannerScreen(
    onResult: (String) -> Unit,
    onCancel: () -> Unit,
) {
    val context = LocalContext.current
    val cameraPermission = rememberPermissionState(Manifest.permission.CAMERA)
    val scanner = remember { BarcodeScanning.getClient() }
    var pending by remember { mutableStateOf<QrContent?>(null) }

    DisposableEffect(Unit) { onDispose { scanner.close() } }

    fun onScanned(value: String?) {
        if (value != null && pending == null) pending = QrContent.parse(value)
    }

    fun decodeImage(image: InputImage) {
        scanner.process(image)
            .addOnSuccessListener { barcodes ->
                val value = barcodes.firstOrNull { it.rawValue != null }?.rawValue
                if (value != null) onScanned(value)
                else Toast.makeText(context, "No QR code found in that image.", Toast.LENGTH_SHORT).show()
            }
            .addOnFailureListener { Toast.makeText(context, "Couldn't read that image.", Toast.LENGTH_SHORT).show() }
    }

    val pickImage = rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri ->
        if (uri != null) runCatching { decodeImage(InputImage.fromFilePath(context, uri)) }
    }
    val takePhoto = rememberLauncherForActivityResult(ActivityResultContracts.TakePicturePreview()) { bitmap ->
        if (bitmap != null) decodeImage(InputImage.fromBitmap(bitmap, 0))
    }

    Box(Modifier.fillMaxSize().background(Color.Black)) {
        if (cameraPermission.status.isGranted) {
            CameraScanner(scanner = scanner) { onScanned(it) }
        } else {
            Column(
                Modifier.fillMaxSize().padding(24.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp, Alignment.CenterVertically),
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                Text("Camera access is needed to scan live. You can also pick a photo below.", color = Color.White)
                Button(onClick = { cameraPermission.launchPermissionRequest() }) { Text("Grant camera access") }
            }
        }

        // Square viewfinder reticle over the camera.
        if (pending == null) ScannerReticle()

        // Bottom controls (hidden while showing a result).
        if (pending == null) {
            Column(
                Modifier.fillMaxSize().padding(24.dp),
                verticalArrangement = Arrangement.Bottom,
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                Surface(color = Color.Black.copy(alpha = 0.55f), shape = RoundedCornerShape(24.dp)) {
                    Row(Modifier.padding(horizontal = 16.dp, vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.QrCodeScanner, null, tint = Color.White)
                        Text("  Point the frame at a QR code", color = Color.White)
                    }
                }
                Row(
                    Modifier.fillMaxWidth().padding(top = 16.dp),
                    horizontalArrangement = Arrangement.spacedBy(12.dp, Alignment.CenterHorizontally),
                ) {
                    FilledTonalButton(onClick = {
                        pickImage.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))
                    }) { Icon(Icons.Filled.PhotoLibrary, null); Text("  Library") }
                    FilledTonalButton(onClick = { takePhoto.launch(null) }) {
                        Icon(Icons.Filled.PhotoCamera, null); Text("  Photo")
                    }
                }
                Button(
                    onClick = onCancel,
                    modifier = Modifier.padding(top = 12.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color.White.copy(alpha = 0.15f)),
                ) { Text("Cancel", color = Color.White) }
            }
        }

        // Parsed result card.
        pending?.let { content ->
            ResultCard(
                content = content,
                onUse = { onResult(content.raw) },
                onScanAgain = { pending = null },
                onCancel = onCancel,
                modifier = Modifier.align(Alignment.BottomCenter),
            )
        }
    }
}

@Composable
private fun ScannerReticle() {
    Canvas(Modifier.fillMaxSize()) {
        val side = size.minDimension * 0.68f
        val left = (size.width - side) / 2f
        val top = (size.height - side) / 2f
        val right = left + side
        val bottom = top + side
        val dim = Color.Black.copy(alpha = 0.55f)

        // Dim everything outside the square.
        drawRect(dim, topLeft = Offset(0f, 0f), size = Size(size.width, top))
        drawRect(dim, topLeft = Offset(0f, bottom), size = Size(size.width, size.height - bottom))
        drawRect(dim, topLeft = Offset(0f, top), size = Size(left, side))
        drawRect(dim, topLeft = Offset(right, top), size = Size(size.width - right, side))

        // Corner brackets.
        val len = 28.dp.toPx()
        val stroke = 4.dp.toPx()
        fun corner(x: Float, y: Float, dx: Int, dy: Int) {
            drawLine(VgAccent, Offset(x, y), Offset(x + len * dx, y), stroke, StrokeCap.Round)
            drawLine(VgAccent, Offset(x, y), Offset(x, y + len * dy), stroke, StrokeCap.Round)
        }
        corner(left, top, 1, 1)
        corner(right, top, -1, 1)
        corner(left, bottom, 1, -1)
        corner(right, bottom, -1, -1)
    }
}

@Composable
private fun ResultCard(
    content: QrContent,
    onUse: () -> Unit,
    onScanAgain: () -> Unit,
    onCancel: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        color = MaterialTheme.colorScheme.surface,
        shape = RoundedCornerShape(topStart = 24.dp, topEnd = 24.dp),
        tonalElevation = 6.dp,
    ) {
        Column(Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Surface(color = VgAccent.copy(alpha = 0.15f), shape = RoundedCornerShape(12.dp)) {
                    Icon(content.icon, null, tint = VgAccent, modifier = Modifier.padding(10.dp))
                }
                Text("  ${content.title}", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            }
            content.fields.forEach { (label, value) ->
                Column {
                    Text(label, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.primary)
                    Text(value, style = MaterialTheme.typography.bodyMedium, fontFamily = FontFamily.Monospace)
                }
            }
            Row(Modifier.fillMaxWidth().padding(top = 4.dp), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedButton(onClick = onScanAgain, modifier = Modifier.weight(1f)) { Text("Scan again") }
                Button(onClick = onUse, modifier = Modifier.weight(1f)) { Text(useLabel(content)) }
            }
            OutlinedButton(onClick = onCancel, modifier = Modifier.fillMaxWidth()) { Text("Cancel") }
        }
    }
}

private fun useLabel(content: QrContent): String = when (content) {
    is QrContent.Totp -> "Use secret"
    is QrContent.Login -> "Sign in"
    is QrContent.Wifi -> "Use"
    is QrContent.Url -> "Use link"
    is QrContent.Text -> "Use"
}

@OptIn(ExperimentalGetImage::class)
@Composable
private fun CameraScanner(scanner: BarcodeScanner, onResult: (String) -> Unit) {
    val lifecycleOwner = LocalLifecycleOwner.current
    val analysisExecutor = remember { Executors.newSingleThreadExecutor() }
    DisposableEffect(Unit) { onDispose { analysisExecutor.shutdown() } }

    AndroidView(
        modifier = Modifier.fillMaxSize(),
        factory = { ctx ->
            val previewView = PreviewView(ctx)
            val providerFuture = ProcessCameraProvider.getInstance(ctx)
            providerFuture.addListener({
                val provider = providerFuture.get()
                val preview = Preview.Builder().build().also { it.setSurfaceProvider(previewView.surfaceProvider) }
                val analysis = ImageAnalysis.Builder()
                    .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                    .build()
                analysis.setAnalyzer(analysisExecutor) { imageProxy: ImageProxy ->
                    val mediaImage = imageProxy.image
                    if (mediaImage == null) { imageProxy.close(); return@setAnalyzer }
                    val input = InputImage.fromMediaImage(mediaImage, imageProxy.imageInfo.rotationDegrees)
                    scanner.process(input)
                        .addOnSuccessListener { barcodes ->
                            barcodes.firstOrNull { it.valueType == Barcode.TYPE_TEXT || it.rawValue != null }?.rawValue?.let(onResult)
                        }
                        .addOnCompleteListener { imageProxy.close() }
                }
                provider.unbindAll()
                provider.bindToLifecycle(lifecycleOwner, CameraSelector.DEFAULT_BACK_CAMERA, preview, analysis)
            }, ContextCompat.getMainExecutor(ctx))
            previewView
        },
    )
}
