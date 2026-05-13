/**
 * gdi-swal2-dialog-shim.js — GDI Aviação | ERP Plataform
 * Módulo startprime: diálogos do ERP via SweetAlert2 (global `Swal`).
 * Expõe `GdiSwal2` com API estável (alert, confirm, prompt, dialog, hideAll) para views e scripts.
 *
 * Stack: AdminLTE 4 + Bootstrap 5 + ASP.NET MVC .NET Framework
 * Pré-requisito: SweetAlert2 11.x (e CSS tema bootstrap-5) carregados antes deste ficheiro no bundle `~/bundles/libui-swal-compat`.
 */

var GdiSwal2 = (function () {

  var swalUi = (typeof Swal !== 'undefined' && typeof Swal.mixin === 'function')
    ? Swal.mixin({ theme: 'bootstrap-5', buttonsStyling: true })
    : Swal;

  function btnLabel(btn, fallback) {
    return (btn && btn.label) ? btn.label : fallback;
  }

  return {

    /**
     * GdiSwal2.alert(message, callback)
     * GdiSwal2.alert({ message, title, callback, buttons, icon })
     * icon: mesmo conjunto do SweetAlert2 ('info'|'warning'|'error'|'success'|'question') ou false para ocultar
     */
    alert: function (options, callback) {
      var config = {};

      if (typeof options === 'string') {
        config.html = options;
        config.callback = callback || null;
      } else {
        config = options;
      }

      var widthOpt = {};
      if (config.size === 'small') {
        widthOpt.width = '22em';
      }

      var iconOpt = Object.prototype.hasOwnProperty.call(config, 'icon')
        ? config.icon
        : 'info';

      var fireOpts = {
        title: config.title || null,
        html: config.message || config.html || '',
        confirmButtonText: btnLabel(config.buttons ? config.buttons.ok : null, 'OK'),
        buttonsStyling: true
      };
      if (iconOpt === false) {
        fireOpts.icon = false;
      } else if (iconOpt !== '' && iconOpt != null && iconOpt !== undefined) {
        fireOpts.icon = iconOpt;
      }

      return swalUi.fire(Object.assign(fireOpts, widthOpt)).then(function () {
        if (typeof config.callback === 'function') config.callback();
      });
    },

    /**
     * GdiSwal2.confirm(message, callback)
     * GdiSwal2.confirm({ message, title, callback, buttons })
     */
    confirm: function (options, callback) {
      var config = {};

      if (typeof options === 'string') {
        config.message = options;
        config.callback = callback || null;
      } else {
        config = options;
      }

      return swalUi.fire({
        title: config.title || 'Confirmação',
        html: config.message || '',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: btnLabel(config.buttons ? config.buttons.confirm : null, 'Sim'),
        cancelButtonText: btnLabel(config.buttons ? config.buttons.cancel : null, 'Cancelar'),
        buttonsStyling: true,
        reverseButtons: true
      }).then(function (result) {
        if (typeof config.callback === 'function') {
          config.callback(result.isConfirmed);
        }
      });
    },

    /**
     * GdiSwal2.prompt({ title, value, inputType, placeholder, required, callback, buttons })
     */
    prompt: function (options) {
      var inputType = options.inputType || 'text';

      return swalUi.fire({
        title: options.title || 'Informe o valor',
        input: inputType,
        inputValue: options.value || '',
        inputPlaceholder: options.placeholder || '',
        showCancelButton: true,
        confirmButtonText: btnLabel(options.buttons ? options.buttons.confirm : null, 'OK'),
        cancelButtonText: btnLabel(options.buttons ? options.buttons.cancel : null, 'Cancelar'),
        buttonsStyling: true,
        inputValidator: options.required
          ? function (value) { if (!value) return 'Este campo é obrigatório.'; }
          : undefined
      }).then(function (result) {
        if (typeof options.callback === 'function') {
          options.callback(result.isConfirmed ? result.value : null);
        }
      });
    },

    /**
     * GdiSwal2.dialog({ title, message, buttons, onEscape, backdrop, closeButton })
     */
    dialog: function (options) {
      var buttons = options.buttons || {};
      var btnKeys = Object.keys(buttons);

      var footerHtml = btnKeys.map(function (key) {
        var btn = buttons[key];
        var cls = btn.className || 'btn-secondary';
        var fullCls = (cls.indexOf('btn') === 0) ? cls : ('btn ' + cls);
        return '<button type="button" class="' + fullCls + ' swal2-dialog-btn" data-key="' + key + '">'
          + (btn.label || key)
          + '</button>';
      }).join(' ');

      var showClose = options.closeButton !== false;

      return swalUi.fire({
        title: options.title || null,
        html: options.message || '',
        showConfirmButton: false,
        showCloseButton: showClose,
        allowOutsideClick: options.backdrop !== false,
        allowEscapeKey: options.onEscape !== false,
        footer: footerHtml || null,
        buttonsStyling: true,
        didOpen: function () {
          document.querySelectorAll('.swal2-dialog-btn').forEach(function (el) {
            el.addEventListener('click', function () {
              var key = el.getAttribute('data-key');
              if (buttons[key] && typeof buttons[key].callback === 'function') {
                buttons[key].callback.call(el);
              }
              Swal.close();
            });
          });

          if (typeof options.onEscape === 'function') {
            document.addEventListener('keyup', function handler(e) {
              if (e.key === 'Escape') {
                options.onEscape();
                document.removeEventListener('keyup', handler);
              }
            });
          }
        }
      });
    },

    /**
     * GdiSwal2.hideAll() — fecha diálogos SweetAlert2 abertos.
     */
    hideAll: function () {
      Swal.close();
    }

  };

})();

window.GdiSwal2 = GdiSwal2;

/**
 * Ponte usada por `start.js` (`LibMessageAlert`, `LibMessageConfirm`, `LibMessageDialog`, …).
 * Sem este objeto, as mensagens caem no `alert()` nativo (HTML de `result.msg` sem formatação).
 */
window.GdiSwalCompat = window.GdiSwalCompat || {
  alert: function (cfg) {
    return GdiSwal2.alert(cfg);
  },
  confirm: function (cfg) {
    return GdiSwal2.confirm(cfg);
  },
  prompt: function (cfg) {
    return GdiSwal2.prompt(cfg);
  },
  dialog: function (options) {
    return GdiSwal2.dialog(options);
  },
  hideAll: function () {
    return GdiSwal2.hideAll();
  }
};
