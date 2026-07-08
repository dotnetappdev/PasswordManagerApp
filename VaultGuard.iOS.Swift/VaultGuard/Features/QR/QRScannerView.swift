// QRScannerView.swift — AVFoundation QR scanner used to capture otpauth:// TOTP secrets.
import SwiftUI
import AVFoundation

struct QRScannerView: View {
    var onResult: (String) -> Void
    var onCancel: () -> Void

    var body: some View {
        ZStack(alignment: .bottom) {
            ScannerRepresentable(onResult: onResult)
                .ignoresSafeArea()
            VStack(spacing: 16) {
                Text("Point the camera at a QR code")
                    .padding(10)
                    .background(.ultraThinMaterial, in: Capsule())
                Button("Cancel", action: onCancel)
                    .buttonStyle(.borderedProminent)
                    .padding(.bottom, 32)
            }
        }
    }
}

private struct ScannerRepresentable: UIViewControllerRepresentable {
    var onResult: (String) -> Void

    func makeUIViewController(context: Context) -> ScannerViewController {
        let vc = ScannerViewController()
        vc.onResult = onResult
        return vc
    }
    func updateUIViewController(_ uiViewController: ScannerViewController, context: Context) {}
}

final class ScannerViewController: UIViewController, AVCaptureMetadataOutputObjectsDelegate {
    var onResult: ((String) -> Void)?
    private let session = AVCaptureSession()
    private var handled = false

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .black
        guard let device = AVCaptureDevice.default(for: .video),
              let input = try? AVCaptureDeviceInput(device: device),
              session.canAddInput(input) else { return }
        session.addInput(input)

        let output = AVCaptureMetadataOutput()
        guard session.canAddOutput(output) else { return }
        session.addOutput(output)
        output.setMetadataObjectsDelegate(self, queue: .main)
        output.metadataObjectTypes = [.qr]

        let preview = AVCaptureVideoPreviewLayer(session: session)
        preview.frame = view.layer.bounds
        preview.videoGravity = .resizeAspectFill
        view.layer.addSublayer(preview)

        DispatchQueue.global(qos: .userInitiated).async { [weak self] in self?.session.startRunning() }
    }

    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        if session.isRunning { session.stopRunning() }
    }

    func metadataOutput(_ output: AVCaptureMetadataOutput,
                        didOutput metadataObjects: [AVMetadataObject],
                        from connection: AVCaptureConnection) {
        guard !handled,
              let obj = metadataObjects.first as? AVMetadataMachineReadableCodeObject,
              let value = obj.stringValue else { return }
        handled = true
        session.stopRunning()
        onResult?(value)
    }
}
