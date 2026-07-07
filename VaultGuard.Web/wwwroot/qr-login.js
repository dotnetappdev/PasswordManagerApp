// QR Code rendering - real, spec-correct, scannable QR codes.
// Backed by the vendored qrcode-generator library (wwwroot/qrcode.min.js, exposes window.qrcode).
// No CDN dependency, so it works offline and is not blocked by browser tracking prevention.

// Renders a scannable QR code for `data` into the element with id `elementId`.
window.renderQrCode = function (elementId, data) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error('QR code element not found:', elementId);
        return;
    }

    try {
        if (typeof window.qrcode !== 'function') {
            throw new Error('QR code library (qrcode-generator) is not loaded');
        }

        // typeNumber 0 = auto-pick the smallest version that fits the data.
        // 'M' error correction gives a good balance of density vs. resilience.
        const qr = window.qrcode(0, 'M');
        qr.addData(data);
        qr.make();

        // cellSize (px per module) and margin (px quiet zone, 4 modules recommended).
        element.innerHTML = qr.createImgTag(6, 24, 'Login QR code');

        const img = element.querySelector('img');
        if (img) {
            img.style.maxWidth = '220px';
            img.style.width = '100%';
            img.style.height = 'auto';
            img.style.imageRendering = 'pixelated'; // keep modules crisp when scaled
        }

        console.log('QR code generated successfully (offline, scannable)');
    } catch (error) {
        console.error('Error rendering QR code:', error);

        // Fallback: show the raw payload so the flow can still be completed manually.
        element.innerHTML = `
            <div style="width: 200px; height: 200px; border: 2px dashed #ccc; display: flex; align-items: center; justify-content: center; font-size: 14px; color: #666; text-align: center; margin: 0 auto;">
                <div>
                    <div style="margin-bottom: 8px;">📱</div>
                    <div>QR Code Generation<br>Failed</div>
                    <div style="font-size: 12px; margin-top: 8px; font-family: monospace; word-break: break-all; max-width: 180px;">
                        ${(data || '').substring(0, 50)}...
                    </div>
                </div>
            </div>
        `;
    }
};
