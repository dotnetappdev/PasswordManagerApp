// QR Code functionality for Password Manager
// This provides a fallback QR code implementation using a simple canvas-based approach

window.renderQrCode = function(elementId, data) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error('QR code element not found:', elementId);
        return;
    }

    // Try to use QRCode library if available (from CDN)
    if (typeof QRCode !== 'undefined') {
        try {
            // Clear any existing QR code
            element.innerHTML = '';
            
            // Create QR code using QRCode.js library
            new QRCode(element, {
                text: data,
                width: 200,
                height: 200,
                colorDark: "#000000",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.M
            });
            
            return;
        } catch (error) {
            console.warn('QRCode library failed, falling back to text display:', error);
        }
    }

    // Fallback: Display QR data as text with instructions
    element.innerHTML = `
        <div style="text-align: center; padding: 20px; border: 2px dashed #ccc; border-radius: 8px; background: #f9f9f9;">
            <div style="font-size: 48px; margin-bottom: 10px;">📱</div>
            <p style="margin: 8px 0; font-weight: bold; color: #333;">QR Code Would Appear Here</p>
            <p style="margin: 8px 0; font-size: 12px; color: #666;">
                Contains: Authentication Token + API Endpoint
            </p>
            <details style="margin-top: 12px;">
                <summary style="cursor: pointer; color: #666; font-size: 12px;">Show QR Data</summary>
                <textarea readonly style="width: 100%; height: 60px; margin-top: 8px; font-family: monospace; font-size: 10px; border: 1px solid #ddd; padding: 4px;">${data}</textarea>
            </details>
        </div>
    `;
};

// Add QRCode library from CDN if not already present
function loadQRCodeLibrary() {
    if (typeof QRCode === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/qrcode@1.5.3/build/qrcode.min.js';
        script.onload = function() {
            console.log('QRCode library loaded successfully');
        };
        script.onerror = function() {
            console.warn('Failed to load QRCode library from CDN');
        };
        document.head.appendChild(script);
    }
}

// Initialize QR code library when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', loadQRCodeLibrary);
} else {
    loadQRCodeLibrary();
}