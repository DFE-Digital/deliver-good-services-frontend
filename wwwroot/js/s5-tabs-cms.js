function s5tab(btn, id) {
  var ctx = (btn && btn.closest && btn.closest('.guide-modern')) || document;

  var tabs = ctx.querySelectorAll('.s5-tab');
  var panels = ctx.querySelectorAll('.s5-panel');

  tabs.forEach(function (t) {
    t.setAttribute('aria-selected', 'false');
    t.setAttribute('tabindex', '-1');
  });

  panels.forEach(function (p) {
    p.classList.remove('on');
  });

  if (btn) {
    btn.setAttribute('aria-selected', 'true');
    btn.setAttribute('tabindex', '0');
  }

  var panel = document.getElementById(id);
  if (!panel && window.CSS && CSS.escape) {
    panel = ctx.querySelector('#' + CSS.escape(id));
  }
  if (!panel) {
    panel = ctx.querySelector('#' + id);
  }

  if (panel) {
    panel.classList.add('on');
  }
}

(function () {
  function activateFromHashOrDefault() {
    var hashId = (window.location.hash || '').replace(/^#/, '');
    if (hashId) {
      try { hashId = decodeURIComponent(hashId); } catch (e) {}
    }

    var targetTab = null;

    if (hashId) {
      targetTab = document.querySelector('.s5-tab[aria-controls="' + hashId + '"]');
    }

    if (!targetTab) {
      targetTab = document.querySelector('.s5-tab');
    }

    if (targetTab) {
      var panelId = targetTab.getAttribute('aria-controls');
      if (panelId) s5tab(targetTab, panelId);
    } else {
      document.querySelectorAll('.s5-panel').forEach(function (p) {
        p.classList.remove('on');
      });
    }
  }

  function wireClicks() {
    document.querySelectorAll('.s5-tab').forEach(function (tab) {
      if (tab.dataset.s5Wired === 'true') return;
      tab.dataset.s5Wired = 'true';

      tab.addEventListener('click', function (e) {
        e.preventDefault();
        var panelId = tab.getAttribute('aria-controls');
        if (panelId) {
          s5tab(tab, panelId);
          if (history && history.replaceState) {
            history.replaceState(null, '', '#' + encodeURIComponent(panelId));
          } else {
            window.location.hash = panelId;
          }
        }
      });
    });
  }

  function initS5Tabs() {
    wireClicks();
    activateFromHashOrDefault();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initS5Tabs);
  } else {
    initS5Tabs();
  }

  window.addEventListener('hashchange', activateFromHashOrDefault);
})();
