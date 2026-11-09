// Small progressive enhancements. The site works without any of this.
(function () {
  // Confirm before destructive actions: <form data-confirm="Delete this?">
  document.addEventListener('submit', function (e) {
    var msg = e.target && e.target.getAttribute && e.target.getAttribute('data-confirm');
    if (msg && !window.confirm(msg)) e.preventDefault();
  });

  // Auto-submit filter dropdowns: <select data-autosubmit>
  document.querySelectorAll('select[data-autosubmit]').forEach(function (s) {
    s.addEventListener('change', function () { s.form && s.form.submit(); });
  });

  // Live image preview: <input type=file data-preview="#img" data-preview-hide="#placeholder">
  document.querySelectorAll('input[type=file][data-preview]').forEach(function (input) {
    input.addEventListener('change', function () {
      var file = input.files && input.files[0];
      var img = document.querySelector(input.getAttribute('data-preview'));
      if (!file || !img || !file.type.startsWith('image/')) return;
      img.src = URL.createObjectURL(file);
      img.hidden = false;
      var hide = input.getAttribute('data-preview-hide');
      if (hide) { var el = document.querySelector(hide); if (el) el.hidden = true; }
    });
  });
})();
