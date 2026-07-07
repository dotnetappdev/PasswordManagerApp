// Draggable column splitter for master-detail layouts (mirrors WPF's GridSplitter).
window.vgSplitter = {
    init: function (splitterId, colId, minWidth, maxWidth) {
        const splitter = document.getElementById(splitterId);
        const col = document.getElementById(colId);
        if (!splitter || !col) return;

        // Re-running init (e.g. after a re-render) would stack duplicate listeners.
        if (splitter.dataset.vgSplitterBound === "1") return;
        splitter.dataset.vgSplitterBound = "1";

        let dragging = false;
        let startX = 0;
        let startWidth = 0;

        splitter.addEventListener("pointerdown", function (e) {
            dragging = true;
            startX = e.clientX;
            startWidth = col.getBoundingClientRect().width;
            splitter.setPointerCapture(e.pointerId);
            document.body.style.cursor = "col-resize";
            document.body.style.userSelect = "none";
        });

        splitter.addEventListener("pointermove", function (e) {
            if (!dragging) return;
            const delta = e.clientX - startX;
            const newWidth = Math.max(minWidth, Math.min(maxWidth, startWidth + delta));
            col.style.width = newWidth + "px";
        });

        function endDrag(e) {
            if (!dragging) return;
            dragging = false;
            try { splitter.releasePointerCapture(e.pointerId); } catch { /* already released */ }
            document.body.style.cursor = "";
            document.body.style.userSelect = "";
        }

        splitter.addEventListener("pointerup", endDrag);
        splitter.addEventListener("pointercancel", endDrag);
    }
};
