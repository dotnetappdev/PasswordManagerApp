// Camera-based QR scanner shared by the Blazor Web app and the MAUI WebView.
// Uses getUserMedia for the camera feed and the vendored jsQR decoder (window.jsQR).
// A single scan session is active at a time; state is kept module-scoped.

window.vgQrScanner = (function () {
    'use strict';

    let stream = null;
    let rafId = 0;
    let video = null;
    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let running = false;

    async function start(videoEl, canvasEl, dotnet) {
        stop(); // ensure any previous session is torn down

        video = videoEl;
        canvas = canvasEl;
        dotNetRef = dotnet;

        if (typeof window.jsQR !== 'function') {
            notifyError('QR decoder (jsQR) is not loaded.');
            return false;
        }

        try {
            // Prefer the rear camera on phones; fall back to any camera.
            stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: { ideal: 'environment' } },
                audio: false
            });
        } catch (e) {
            try {
                stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
            } catch (e2) {
                notifyError(describeError(e2));
                return false;
            }
        }

        try {
            video.setAttribute('playsinline', 'true'); // iOS: stay inline, don't fullscreen
            video.srcObject = stream;
            await video.play();
        } catch (e) {
            notifyError(describeError(e));
            stop();
            return false;
        }

        canvas = canvas || document.createElement('canvas');
        ctx = canvas.getContext('2d', { willReadFrequently: true });
        running = true;
        rafId = requestAnimationFrame(tick);
        return true;
    }

    function tick() {
        if (!running || !video) return;

        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            const w = video.videoWidth;
            const h = video.videoHeight;
            if (w && h) {
                canvas.width = w;
                canvas.height = h;
                ctx.drawImage(video, 0, 0, w, h);
                let imageData;
                try {
                    imageData = ctx.getImageData(0, 0, w, h);
                } catch (e) {
                    // Some WebViews taint the canvas until the first frame settles; retry next frame.
                    rafId = requestAnimationFrame(tick);
                    return;
                }
                const code = window.jsQR(imageData.data, w, h, { inversionAttempts: 'dontInvert' });
                if (code && code.data) {
                    const text = code.data;
                    stop();
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('OnCodeDetected', text);
                    }
                    return;
                }
            }
        }
        rafId = requestAnimationFrame(tick);
    }

    function stop() {
        running = false;
        if (rafId) { cancelAnimationFrame(rafId); rafId = 0; }
        if (stream) {
            try { stream.getTracks().forEach(t => t.stop()); } catch (e) { /* ignore */ }
            stream = null;
        }
        if (video) {
            try { video.pause(); video.srcObject = null; } catch (e) { /* ignore */ }
        }
    }

    function notifyError(message) {
        if (dotNetRef) {
            try { dotNetRef.invokeMethodAsync('OnScanError', message); } catch (e) { /* ignore */ }
        }
        console.warn('QR scanner:', message);
    }

    function describeError(e) {
        if (!e) return 'Camera error.';
        switch (e.name) {
            case 'NotAllowedError':
            case 'SecurityError':
                return 'Camera permission was denied. Allow camera access and try again.';
            case 'NotFoundError':
            case 'OverconstrainedError':
                return 'No camera was found on this device.';
            case 'NotReadableError':
                return 'The camera is already in use by another app.';
            default:
                return e.message || 'Unable to start the camera.';
        }
    }

    return { start, stop };
})();
