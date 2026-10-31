// Small progressive enhancements. Every page still works without this file.
(function () {
  "use strict";

  // Confirm before destructive actions: <form data-confirm="Delete this event?">
  document.addEventListener("submit", function (e) {
    var form = e.target;
    var message = form.getAttribute && form.getAttribute("data-confirm");
    if (message && !window.confirm(message)) e.preventDefault();
  });

  // Stop double submits: the button greys out once the form is valid and on its way.
  document.querySelectorAll("form[data-busy]").forEach(function (form) {
    form.addEventListener("submit", function () {
      if (window.jQuery && jQuery.validator && !jQuery(form).valid()) return;
      form.querySelectorAll("button[type=submit]").forEach(function (b) {
        b.disabled = true;
        b.dataset.label = b.innerHTML;
        b.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>' + (form.getAttribute("data-busy") || "Saving…");
      });
    });
  });

  // Image pickers: <input type=file data-max-mb="5" data-preview="#previewId">
  document.querySelectorAll("input[type=file][data-max-mb]").forEach(function (input) {
    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      var error = document.querySelector(input.getAttribute("data-error") || "");
      if (error) error.textContent = "";
      if (!file) return;

      var maxMb = parseFloat(input.getAttribute("data-max-mb"));
      if (file.size > maxMb * 1024 * 1024) {
        if (error) error.textContent = "That image is larger than " + maxMb + " MB. Please choose a smaller one.";
        input.value = "";
        return;
      }
      var preview = document.querySelector(input.getAttribute("data-preview") || "");
      if (preview && file.type.indexOf("image/") === 0) {
        var reader = new FileReader();
        reader.onload = function (ev) { preview.src = ev.target.result; preview.classList.remove("d-none"); };
        reader.readAsDataURL(file);
      }
    });
  });

  // Register page: only Organisers need the invite code.
  var roleInputs = document.querySelectorAll("input[name=Role]");
  var invite = document.getElementById("invite-block");
  if (roleInputs.length && invite) {
    var sync = function () {
      var checked = document.querySelector("input[name=Role]:checked");
      invite.classList.toggle("d-none", !checked || checked.value !== "Organiser");
    };
    roleInputs.forEach(function (r) { r.addEventListener("change", sync); });
    sync();
  }

  // Live table filter: <input data-filter="#tableId"> hides rows that don't contain the text.
  document.querySelectorAll("input[data-filter]").forEach(function (input) {
    var rows = document.querySelectorAll(input.getAttribute("data-filter") + " tbody tr");
    input.addEventListener("input", function () {
      var term = input.value.trim().toLowerCase();
      rows.forEach(function (row) { row.hidden = term && row.textContent.toLowerCase().indexOf(term) === -1; });
    });
  });
})();
