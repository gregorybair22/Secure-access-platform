window.saToast = {
    options: {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: 5000,
        extendedTimeOut: 2000,
        newestOnTop: true,
        preventDuplicates: true
    },

    show: function (type, message) {
        if (!message || typeof toastr === 'undefined') return;

        toastr.options = this.options;

        if (typeof toastr[type] === 'function') {
            toastr[type](message);
        } else {
            toastr.info(message);
        }
    }
};
