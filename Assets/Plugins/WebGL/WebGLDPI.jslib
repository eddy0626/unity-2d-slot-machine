mergeInto(LibraryManager.library, {
    GetDevicePixelRatio: function() {
        return window.devicePixelRatio || 1;
    },

    SetCanvasSize: function(width, height) {
        var canvas = document.querySelector('#unity-canvas');
        if (canvas) {
            canvas.width = width;
            canvas.height = height;
            canvas.style.width = (width / window.devicePixelRatio) + 'px';
            canvas.style.height = (height / window.devicePixelRatio) + 'px';
        }
    }
});
