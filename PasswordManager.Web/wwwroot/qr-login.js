// QR Code rendering using qrcode.js library
window.renderQrCode = function(elementId, data) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error('Element not found:', elementId);
        return;
    }

    // Clear any existing content
    element.innerHTML = '';

    try {
        // Check if QRCode library is available
        if (typeof QRCode !== 'undefined') {
            // Use QRCode.js library if available
            new QRCode(element, {
                text: data,
                width: 200,
                height: 200,
                colorDark: "#000000",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.M
            });
        } else {
            // Fallback: create a simple placeholder
            element.innerHTML = `
                <div style="width: 200px; height: 200px; border: 2px dashed #ccc; display: flex; align-items: center; justify-content: center; font-size: 14px; color: #666; text-align: center; margin: 0 auto;">
                    <div>
                        <div style="margin-bottom: 8px;">📱</div>
                        <div>QR Code Library<br>Not Available</div>
                        <div style="font-size: 12px; margin-top: 8px; font-family: monospace; word-break: break-all; max-width: 180px;">
                            ${data.substring(0, 50)}...
                        </div>
                    </div>
                </div>
            `;
        }
    } catch (error) {
        console.error('Error rendering QR code:', error);
        element.innerHTML = `
            <div style="width: 200px; height: 200px; border: 2px solid #f44336; display: flex; align-items: center; justify-content: center; font-size: 14px; color: #f44336; text-align: center; margin: 0 auto;">
                <div>
                    <div>❌</div>
                    <div>Error rendering<br>QR Code</div>
                </div>
            </div>
        `;
    }
};

// Initialize QR code library from CDN if not already loaded
window.initQrCodeLibrary = function() {
    if (typeof QRCode === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/qrcode@1.5.3/build/qrcode.min.js';
        script.onload = function() {
            console.log('QR Code library loaded');
        };
        script.onerror = function() {
            console.warn('Failed to load QR Code library from CDN');
        };
        document.head.appendChild(script);
    }
};

// Auto-initialize when the script loads
document.addEventListener('DOMContentLoaded', function() {
    window.initQrCodeLibrary();
});