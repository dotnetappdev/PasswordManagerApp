// QR Code rendering for Vault Guard (MAUI) - real, spec-correct, scannable QR codes.
// Backed by the vendored qrcode-generator library (wwwroot/js/qrcode-lib.js, exposes window.qrcode).
// No CDN dependency, so it works offline and is not blocked by the WebView's tracking prevention.

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
        console.warn('QR code generation failed, showing fallback:', error);

        // Fallback: display the payload so the flow can still be completed manually.
        element.innerHTML = `
            <div style="text-align: center; padding: 20px; border: 2px dashed #ccc; border-radius: 8px; background: #f9f9f9;">
                <div style="font-size: 48px; margin-bottom: 10px;">📱</div>
                <p style="margin: 8px 0; font-weight: bold; color: #333;">QR Code Generation Failed</p>
                <p style="margin: 8px 0; font-size: 12px; color: #666;">
                    Contains: Authentication Token + API Endpoint
                </p>
                <details style="margin-top: 12px;">
                    <summary style="cursor: pointer; color: #666; font-size: 12px;">Show QR Data</summary>
                    <textarea readonly style="width: 100%; height: 60px; margin-top: 8px; font-family: monospace; font-size: 10px; border: 1px solid #ddd; padding: 4px;">${data || ''}</textarea>
                </details>
            </div>
        `;
    }
};
