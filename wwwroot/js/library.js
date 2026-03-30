/**
 * DfE Service Manual — My Library
 * Saves page references to browser localStorage so users can build a
 * personal reading list. Purely client-side; no data leaves the device.
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'dfe-sm-library';
    var activeTypeFilters = [];

    function trackGaEvent(eventName, properties) {
        if (typeof window.trackEvent === 'function') {
            window.trackEvent(eventName, properties || {});
            return;
        }

        window.dataLayer = window.dataLayer || [];
        window.dataLayer.push(Object.assign({ event: eventName }, properties || {}));
    }

    // -------------------------------------------------------------------------
    // Core storage API
    // -------------------------------------------------------------------------
    var DfeLibrary = {
        getAll: function () {
            try {
                return JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
            } catch (e) {
                return [];
            }
        },

        has: function (id) {
            return this.getAll().some(function (item) { return item.id === id; });
        },

        add: function (item) {
            // Deduplicate by id, then prepend so newest is first
            var items = this.getAll().filter(function (i) { return i.id !== item.id; });
            items.unshift(item);
            try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items)); } catch (e) { /* storage full */ }
            this._emit();
        },

        remove: function (id) {
            var items = this.getAll().filter(function (item) { return item.id !== id; });
            try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items)); } catch (e) { }
            this._emit();
        },

        toggle: function (item) {
            if (this.has(item.id)) { this.remove(item.id); } else { this.add(item); }
        },

        clear: function () {
            try { localStorage.removeItem(STORAGE_KEY); } catch (e) { }
            this._emit();
        },

        _emit: function () {
            document.dispatchEvent(new CustomEvent('dfe:library:changed'));
            updateHeaderLibraryCount();
        }
    };

    // -------------------------------------------------------------------------
    // Helpers — output encoding to prevent XSS when inserting into innerHTML
    // -------------------------------------------------------------------------
    function escHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function escAttr(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function getLiveRegion() {
        var existing = document.getElementById('library-live-region');
        if (existing) { return existing; }

        var region = document.createElement('div');
        region.id = 'library-live-region';
        region.className = 'govuk-visually-hidden';
        region.setAttribute('role', 'status');
        region.setAttribute('aria-live', 'polite');
        region.setAttribute('aria-atomic', 'true');
        document.body.appendChild(region);
        return region;
    }

    function announceLibraryChange(message) {
        var region = getLiveRegion();
        // Clear then set on next tick so repeated messages are re-announced.
        region.textContent = '';
        window.setTimeout(function () {
            region.textContent = message;
        }, 20);
    }

    function getRemoveModal() {
        var existing = document.getElementById('lib-remove-modal');
        if (existing) { return existing; }

        var wrapper = document.createElement('div');
        wrapper.id = 'lib-remove-modal';
        wrapper.className = 'lib-modal';
        wrapper.hidden = true;
        wrapper.innerHTML =
            '<div class="lib-modal__backdrop" data-lib-modal-cancel></div>' +
            '<div class="lib-modal__dialog" role="dialog" tabindex="-1" aria-modal="true" aria-labelledby="lib-remove-modal-title" aria-describedby="lib-remove-modal-body">' +
            '<h2 class="govuk-heading-m lib-modal__title" id="lib-remove-modal-title">Remove saved item</h2>' +
            '<p class="govuk-body" id="lib-remove-modal-body"></p>' +
            '<div class="lib-modal__actions">' +
            '<button type="button" class="govuk-button govuk-button--warning" data-lib-modal-confirm>Remove</button>' +
            '<button type="button" class="govuk-button govuk-button--secondary" data-lib-modal-cancel>Cancel</button>' +
            '</div>' +
            '</div>';

        document.body.appendChild(wrapper);
        return wrapper;
    }

    function showLibraryConfirmDialog(options, onConfirm) {
        var modal = getRemoveModal();
        var dialog = modal.querySelector('.lib-modal__dialog');
        var title = modal.querySelector('#lib-remove-modal-title');
        var body = modal.querySelector('#lib-remove-modal-body');
        var confirmBtn = modal.querySelector('[data-lib-modal-confirm]');
        var cancelBtns = modal.querySelectorAll('[data-lib-modal-cancel]');
        var previousActiveElement = document.activeElement;

        if (title) {
            title.textContent = options.title || 'Confirm action';
        }
        if (body) {
            body.textContent = options.body || '';
        }
        if (confirmBtn) {
            confirmBtn.textContent = options.confirmText || 'Confirm';
        }

        function closeModal() {
            modal.hidden = true;
            document.body.classList.remove('lib-modal-open');
            confirmBtn.removeEventListener('click', confirmHandler);
            cancelBtns.forEach(function (btn) { btn.removeEventListener('click', cancelHandler); });
            document.removeEventListener('keydown', escHandler);
            if (previousActiveElement && typeof previousActiveElement.focus === 'function') {
                previousActiveElement.focus();
            }
        }

        function confirmHandler() {
            closeModal();
            onConfirm();
        }

        function cancelHandler() {
            closeModal();
        }

        function escHandler(e) {
            if (e.key === 'Escape') {
                e.preventDefault();
                closeModal();
            }
        }

        confirmBtn.addEventListener('click', confirmHandler);
        cancelBtns.forEach(function (btn) { btn.addEventListener('click', cancelHandler); });
        document.addEventListener('keydown', escHandler);

        modal.hidden = false;
        document.body.classList.add('lib-modal-open');
        if (dialog && typeof dialog.focus === 'function') {
            dialog.focus();
        }
    }

    function showRemoveConfirmDialog(itemTitle, onConfirm) {
        showLibraryConfirmDialog({
            title: 'Remove saved item',
            body: 'Are you sure you want to remove "' + itemTitle + '" from your library?',
            confirmText: 'Remove'
        }, onConfirm);
    }

    // -------------------------------------------------------------------------
    // Toggle button state
    // -------------------------------------------------------------------------
    function updateButton(btn) {
        var id = btn.getAttribute('data-lib-id');
        var saved = DfeLibrary.has(id);
        var label = btn.querySelector('.library-btn__label');
        if (label) {
            label.textContent = saved ? 'Saved' : 'Save';
        }
        btn.setAttribute('aria-pressed', saved ? 'true' : 'false');
        btn.classList.toggle('library-btn--saved', saved);
    }

    // Update header library count
    function updateHeaderLibraryCount() {
        var count = DfeLibrary.getAll().length;
        var badge = document.getElementById('library-count-badge');
        var text = document.getElementById('library-count-text');
        var link = document.querySelector('.gem-c-layout-super-navigation-header__library-item-link');
        if (badge) {
            badge.textContent = count;
        }
        if (text) {
            text.textContent = count;
        }
        if (link) {
            link.setAttribute('aria-label', 'My library (' + count + ' items)');
        }
    }

    function initToggles() {
        document.querySelectorAll('[data-library-toggle]').forEach(function (btn) {
            updateButton(btn);
            // Guard against double-wiring on re-calls
            if (btn._libWired) { return; }
            btn._libWired = true;
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                var item = {
                    id: btn.getAttribute('data-lib-id'),
                    title: btn.getAttribute('data-lib-title'),
                    url: btn.getAttribute('data-lib-url'),
                    type: btn.getAttribute('data-lib-type'),
                    description: btn.getAttribute('data-lib-description') || ''
                };
                var wasSaved = DfeLibrary.has(item.id);

                DfeLibrary.toggle(item);

                announceLibraryChange(
                    wasSaved
                        ? item.title + ' removed from your library.'
                        : item.title + ' saved to your library.'
                );

                trackGaEvent('save_to_library', {
                    action: wasSaved ? 'remove' : 'add',
                    item_id: item.id,
                    item_name: item.title,
                    item_url: item.url,
                    content_type: item.type,
                    page_path: window.location.pathname
                });

                updateButton(btn);
            });
        });
    }

    function initShareTracking() {
        document.querySelectorAll('.share-panel__link[data-share-provider]').forEach(function (link) {
            if (link._shareWired) { return; }
            link._shareWired = true;

            link.addEventListener('click', function () {
                var sharePanel = link.closest('.share-panel');
                var provider = link.getAttribute('data-share-provider');
                if (!provider) { return; }
                var shareTitle = (sharePanel && sharePanel.getAttribute('data-share-title')) || document.title;
                var shareUrl = (sharePanel && sharePanel.getAttribute('data-share-url')) || window.location.href;

                trackGaEvent('share_page', {
                    method: provider,
                    item_name: shareTitle,
                    item_url: shareUrl,
                    page_path: window.location.pathname
                });
            });
        });
    }

    // -------------------------------------------------------------------------
    // Navigation badge (shows saved-item count beside "My library" nav link)
    // -------------------------------------------------------------------------
    function updateNavBadge() {
        var count = DfeLibrary.getAll().length;
        document.querySelectorAll('.lib-count').forEach(function (badge) {
            badge.textContent = count > 0 ? String(count) : '';
            if (count > 0) { badge.removeAttribute('hidden'); }
            else { badge.setAttribute('hidden', ''); }
        });
        // Update accessible label on the nav link
        var link = document.querySelector('.lib-nav-link');
        if (link) {
            var ariaLabel = count > 0
                ? 'My library, ' + count + ' saved item' + (count !== 1 ? 's' : '')
                : 'My library';
            link.setAttribute('aria-label', ariaLabel);
        }
    }

    // -------------------------------------------------------------------------
    // Library page rendering (used on /my-library)
    // -------------------------------------------------------------------------
    function renderLibraryPage() {
        var container = document.getElementById('library-content');
        var filterContainer = document.getElementById('lib-type-filters');
        if (!container) { return; }

        var items = DfeLibrary.getAll();
        var groups = {};
        items.forEach(function (item) {
            var t = item.type || 'Other';
            if (!groups[t]) { groups[t] = []; }
            groups[t].push(item);
        });

        renderTypeFilters(groups, filterContainer);

        container.setAttribute('aria-busy', 'false');

        if (items.length === 0) {
            container.innerHTML =
                '<div class="lib-empty">' +
                '<p class="govuk-body govuk-!-margin-bottom-2">You have not saved any pages yet.</p>' +
                '<p class="govuk-body"><a href="/guidance" class="govuk-link">Browse guidance</a> to ' +
                'find content to save to your library.</p>' +
                '</div>';
            return;
        }

        var selectedSet = activeTypeFilters.length > 0 ? new Set(activeTypeFilters) : null;
        var visibleGroupKeys = Object.keys(groups).sort().filter(function (type) {
            return !selectedSet || selectedSet.has(type);
        });

        var visibleCount = 0;
        visibleGroupKeys.forEach(function (type) {
            visibleCount += groups[type].length;
        });

        if (visibleCount === 0) {
            container.innerHTML =
                '<div class="lib-empty">' +
                '<p class="govuk-body govuk-!-margin-bottom-2">No saved items match your current filter.</p>' +
                '<p class="govuk-body"><button type="button" id="lib-reset-filters" class="govuk-link lib-text-btn">Show all types</button></p>' +
                '</div>';

            var resetBtn = document.getElementById('lib-reset-filters');
            if (resetBtn) {
                resetBtn.addEventListener('click', function () {
                    activeTypeFilters = [];
                    renderLibraryPage();
                });
            }
            return;
        }

        var html =
            '<p class="govuk-body-s govuk-!-margin-bottom-6 lib-meta">' +
            visibleCount + '\u00a0saved\u00a0item' + (visibleCount !== 1 ? 's' : '') +
            (selectedSet ? ' (filtered)' : '') +
            '\u2002\u2014\u2002<button type="button" id="lib-clear-all" class="govuk-link lib-text-btn">Clear all</button>' +
            '</p>';

        visibleGroupKeys.forEach(function (type) {
            html += '<h2 class="govuk-heading-m">' + escHtml(type) + '</h2>';
            html += '<ul class="lib-items">';
            groups[type].forEach(function (item) {
                html +=
                    '<li class="lib-card">' +
                    '<div class="lib-card__main">' +
                    '<a href="' + encodeURI(item.url) + '" class="govuk-link lib-card__title">' +
                    escHtml(item.title) + '</a>';
                if (item.description) {
                    html += '<p class="govuk-body-s lib-card__desc">' + escHtml(item.description) + '</p>';
                }
                html +=
                    '</div>' +
                    '<button type="button" class="lib-card__remove" ' +
                    'data-lib-remove="' + escAttr(item.id) + '" ' +
                    'data-lib-remove-title="' + escAttr(item.title) + '" ' +
                    'aria-label="Remove ' + escAttr(item.title) + ' from library"><i class="fa-regular fa-trash-can" aria-hidden="true"></i>Remove</button>' +
                    '</li>';
            });
            html += '</ul>';
        });

        container.innerHTML = html;

        // Wire per-item remove buttons
        container.querySelectorAll('[data-lib-remove]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var itemId = btn.getAttribute('data-lib-remove');
                var itemTitle = btn.getAttribute('data-lib-remove-title') || 'this item';
                var item = DfeLibrary.getAll().find(function (savedItem) {
                    return savedItem.id === itemId;
                }) || null;
                showRemoveConfirmDialog(itemTitle, function () {
                    DfeLibrary.remove(itemId);

                    trackGaEvent('library_item_removed', {
                        item_id: itemId,
                        item_name: itemTitle,
                        item_url: item && item.url ? item.url : '',
                        content_type: item && item.type ? item.type : '',
                        page_path: window.location.pathname
                    });

                    announceLibraryChange(itemTitle + ' removed from your library.');
                    renderLibraryPage();
                    updateNavBadge();
                });
            });
        });

        // Wire "Clear all" button
        var clearAllBtn = document.getElementById('lib-clear-all');
        if (clearAllBtn) {
            clearAllBtn.addEventListener('click', function () {
                var savedItems = DfeLibrary.getAll();
                var count = savedItems.length;
                showLibraryConfirmDialog({
                    title: 'Clear your library',
                    body: 'Are you sure you want to remove all ' + count + ' saved item' + (count !== 1 ? 's' : '') + ' from your library?',
                    confirmText: 'Clear all'
                }, function () {
                    DfeLibrary.clear();

                    trackGaEvent('library_cleared', {
                        item_count: count,
                        content_types: Array.from(new Set(savedItems.map(function (item) {
                            return item.type || 'Other';
                        }))).join(','),
                        page_path: window.location.pathname
                    });

                    announceLibraryChange('All saved items removed from your library.');
                    renderLibraryPage();
                });
            });
        }
    }

    function renderTypeFilters(groups, filterContainer) {
        if (!filterContainer) { return; }

        var types = Object.keys(groups).sort();

        if (types.length === 0) {
            activeTypeFilters = [];
            filterContainer.innerHTML = '<p class="govuk-body-s govuk-!-margin-bottom-0">No saved types yet.</p>';
            return;
        }

        // Remove filters for types that no longer exist in saved data
        activeTypeFilters = activeTypeFilters.filter(function (type) {
            return types.indexOf(type) >= 0;
        });

        var html = '<form id="lib-type-filter-form" class="lib-type-filter-form">';
        html += '<ul class="gf-list">';
        types.forEach(function (type, index) {
            var id = 'lib-filter-type-' + index;
            var checked = activeTypeFilters.indexOf(type) >= 0 ? ' checked' : '';
            html +=
                '<li class="gf-item">' +
                '<label class="gf-check" for="' + id + '">' +
                '<input id="' + id + '" name="libraryType" type="checkbox" value="' + escAttr(type) + '"' + checked + '>' +
                '<span class="gf-check__lbl">' + escHtml(type) + '</span>' +
                '<span class="gf-count">' + groups[type].length + '</span>' +
                '</label>' +
                '</li>';
        });
        html += '</ul>';
        html += '<div class="guidance-filters__actions">';
        html += '<button type="submit" class="govuk-button govuk-button--secondary guidance-filters__button">Apply filters</button>';
        html += '<a href="#" id="lib-clear-type-filters" class="govuk-link guidance-filters__clear">Clear all filters</a>';
        html += '</div>';
        html += '</form>';

        filterContainer.innerHTML = html;

        var typeFilterForm = document.getElementById('lib-type-filter-form');
        if (typeFilterForm) {
            typeFilterForm.addEventListener('submit', function (e) {
                e.preventDefault();
                activeTypeFilters = Array.from(typeFilterForm.querySelectorAll('input[name="libraryType"]:checked')).map(function (cb) {
                    return cb.value;
                });
                renderLibraryPage();
            });
        }

        var clearFiltersBtn = document.getElementById('lib-clear-type-filters');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', function (e) {
                e.preventDefault();
                activeTypeFilters = [];
                renderLibraryPage();
            });
        }
    }

    // -------------------------------------------------------------------------
    // Init
    // -------------------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', function () {
        initToggles();
        initShareTracking();
        updateNavBadge();
        updateHeaderLibraryCount();
        renderLibraryPage();

        document.addEventListener('dfe:library:changed', function () {
            updateHeaderLibraryCount();
            updateNavBadge();
            document.querySelectorAll('[data-library-toggle]').forEach(updateButton);
            renderLibraryPage();
        });
    });

    // Expose globally so other scripts or the console can interact with it
    window.DfeLibrary = DfeLibrary;
}());
