/**
 * Documentation site scripts
 * - Copy-to-clipboard buttons (copies from the active tab panel)
 * - Razor / Markdown / HTML tab switching
 */
(function () {
  'use strict';

  // ── Copy buttons ────────────────────────────────────────────────────────────
  function initCopyButtons() {
    document.querySelectorAll('.doc-copy-btn').forEach(function (btn) {
      if (btn.dataset.docCopyInit) return;
      btn.dataset.docCopyInit = 'true';

      btn.addEventListener('click', function () {
        // The button lives inside a .doc-tab-panel or directly in .doc-example__code.
        // Always copy from the nearest code element in the same panel.
        var panel = btn.closest('.doc-tab-panel') || btn.closest('.doc-example__code');
        var codeEl = panel ? panel.querySelector('code') : null;
        if (!codeEl) return;

        var text = codeEl.textContent;

        navigator.clipboard.writeText(text).then(function () {
          flashCopied(btn);
        }).catch(function () {
          // Fallback for older browsers
          var ta = document.createElement('textarea');
          ta.value = text;
          ta.style.position = 'fixed';
          ta.style.opacity = '0';
          document.body.appendChild(ta);
          ta.select();
          document.execCommand('copy');
          document.body.removeChild(ta);
          flashCopied(btn);
        });
      });
    });
  }

  function flashCopied(btn) {
    var original = btn.textContent;
    btn.textContent = 'Copied!';
    setTimeout(function () { btn.textContent = original; }, 2000);
  }

  // ── Code tabs (Razor / Markdown / HTML) ─────────────────────────────────────
  function initDocTabs() {
    document.querySelectorAll('.doc-tabs').forEach(function (tabList) {
      if (tabList.dataset.docTabsInit) return;
      tabList.dataset.docTabsInit = 'true';

      tabList.querySelectorAll('.doc-tab').forEach(function (tab) {
        tab.addEventListener('click', function () {
          var targetId = tab.dataset.target;
          // Tabs wrapper contains the tablist + all panels
          var wrapper = tab.closest('.doc-example__tabs-wrapper') || tab.closest('.doc-example');

          // Deactivate all tabs in this tablist
          tabList.querySelectorAll('.doc-tab').forEach(function (t) {
            t.classList.remove('doc-tab--active');
            t.setAttribute('aria-selected', 'false');
          });

          // Deactivate all panels in this wrapper
          wrapper.querySelectorAll('.doc-tab-panel').forEach(function (p) {
            p.classList.remove('doc-tab-panel--active');
          });

          // Activate the clicked tab and its panel
          tab.classList.add('doc-tab--active');
          tab.setAttribute('aria-selected', 'true');
          var panel = document.getElementById(targetId);
          if (panel) panel.classList.add('doc-tab-panel--active');
        });

        // Keyboard navigation: left/right arrows move between tabs
        tab.addEventListener('keydown', function (e) {
          var tabs = Array.from(tabList.querySelectorAll('.doc-tab'));
          var idx = tabs.indexOf(tab);
          if (e.key === 'ArrowRight' && idx < tabs.length - 1) {
            tabs[idx + 1].focus();
            tabs[idx + 1].click();
          } else if (e.key === 'ArrowLeft' && idx > 0) {
            tabs[idx - 1].focus();
            tabs[idx - 1].click();
          }
        });
      });
    });
  }

  function init() {
    initCopyButtons();
    initDocTabs();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
}());
