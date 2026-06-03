(function () {
    if (typeof toastr === 'undefined') return;

    var params = new URLSearchParams(window.location.search);
    var type = params.get('toast');
    var message = params.get('toastMsg');

    if (!message) {
        var legacyError = params.get('error');
        if (legacyError) {
            type = 'error';
            message = legacyError;
            params.delete('error');
        }
    }

    if (!type || !message) return;

    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: 5000,
        extendedTimeOut: 2000
    };

    if (typeof toastr[type] === 'function') {
        toastr[type](message);
    } else {
        toastr.info(message);
    }

    params.delete('toast');
    params.delete('toastMsg');
    var qs = params.toString();
    var cleanUrl = window.location.pathname + (qs ? '?' + qs : '') + window.location.hash;
    history.replaceState({}, document.title, cleanUrl);
})();
