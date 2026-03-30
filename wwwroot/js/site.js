//
// For guidance on how to add JavaScript see:
// https://prototype-kit.service.gov.uk/docs/adding-css-javascript-and-images
//

// Shared telemetry helpers for GTM and Application Insights
function pushToDataLayer(eventName, properties) {
  window.dataLayer = window.dataLayer || [];
  window.dataLayer.push(Object.assign({ event: eventName }, properties || {}));
}

function trackEvent(eventName, properties) {
  pushToDataLayer(eventName, properties);

  if (window.appInsights) {
    window.appInsights.trackEvent(eventName, properties);
  }
}

function trackPageView(pageName, url) {
  pushToDataLayer('page_view', {
    page_title: pageName,
    page_location: url,
    page_path: window.location.pathname
  });

  if (window.appInsights) {
    window.appInsights.trackPageView(pageName, url);
  }
}

window.trackEvent = trackEvent;
window.trackPageView = trackPageView;

function getElementLabel(element) {
  if (!element) return '';

  var ariaLabel = element.getAttribute('aria-label');
  if (ariaLabel && ariaLabel.trim()) {
    return ariaLabel.trim();
  }

  return (element.textContent || '').replace(/\s+/g, ' ').trim();
}

function getSectionHeadingText(element, selector) {
  var container = element && element.closest ? element.closest(selector) : null;
  if (!container) return '';

  var heading = container.querySelector('h1, h2, h3, .gf-title');
  return heading ? getElementLabel(heading) : '';
}

// Track page load
document.addEventListener('DOMContentLoaded', function () {
  // Track page view
  trackPageView(document.title, window.location.href);

  // Track user interactions
  trackEvent('page_loaded', {
    page: window.location.pathname,
    referrer: document.referrer,
    userAgent: navigator.userAgent,
    timestamp: new Date().toISOString()
  });
});

// Feedback form functionality
document.addEventListener('DOMContentLoaded', function () {
  const feedbackLink = document.getElementById('feedback-link');
  const feedbackPanel = document.getElementById('feedback-panel');
  const thanksMessage = document.getElementById('thanksMessage');
  const feedbackForm = document.getElementById('feedback-form');
  const cancelButton = document.getElementById('cancelButton');

  if (feedbackLink && feedbackPanel && thanksMessage && feedbackForm && cancelButton) {
    // Show feedback panel when link is clicked
    feedbackLink.addEventListener('click', function (e) {
      e.preventDefault();
      feedbackPanel.classList.add('show');
      feedbackPanel.setAttribute('aria-hidden', 'false');
      thanksMessage.classList.remove('show');

      // Clear any validation errors when opening the panel
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');

      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.remove('govuk-form-group--error');
        textarea.classList.remove('govuk-textarea--error');
        errorSummary.style.display = 'none';
        errorMessage.style.display = 'none';

        // Reset aria-describedby
        textarea.setAttribute('aria-describedby', 'feedback_form_input-info');
      }

      // Track feedback panel opened
      trackEvent('feedback_panel_opened', {
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });

      // Focus on the textarea
      if (textarea) {
        textarea.focus();
      }
    });

    // Hide feedback panel when cancel button is clicked
    cancelButton.addEventListener('click', function (e) {
      e.preventDefault();
      feedbackPanel.classList.remove('show');
      feedbackPanel.setAttribute('aria-hidden', 'true');

      // Clear any validation errors
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');

      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.remove('govuk-form-group--error');
        textarea.classList.remove('govuk-textarea--error');
        errorSummary.style.display = 'none';
        errorMessage.style.display = 'none';

        // Reset aria-describedby
        textarea.setAttribute('aria-describedby', 'feedback_form_input-info');
      }

      // Track feedback panel cancelled
      trackEvent('feedback_panel_cancelled', {
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    });

    // Function to show validation errors
    function showFeedbackError() {
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');

      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.add('govuk-form-group--error');
        textarea.classList.add('govuk-textarea--error');
        errorSummary.style.display = 'block';
        errorMessage.style.display = 'block';

        // Update aria-describedby to include error message
        const currentDescribedBy = textarea.getAttribute('aria-describedby') || '';
        if (!currentDescribedBy.includes('feedback_form_input-error')) {
          textarea.setAttribute('aria-describedby', 'feedback_form_input-error ' + currentDescribedBy);
        }

        // Focus on error summary for screen readers
        errorSummary.focus();
      }
    }

    // Function to hide validation errors
    function hideFeedbackError() {
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');

      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.remove('govuk-form-group--error');
        textarea.classList.remove('govuk-textarea--error');
        errorSummary.style.display = 'none';
        errorMessage.style.display = 'none';

        // Update aria-describedby to remove error message
        const currentDescribedBy = textarea.getAttribute('aria-describedby') || '';
        textarea.setAttribute('aria-describedby', currentDescribedBy.replace('feedback_form_input-error', '').trim());
      }
    }

    // Clear errors when user starts typing and is under limit
    const textarea = feedbackForm.querySelector('textarea');
    if (textarea) {
      textarea.addEventListener('input', function () {
        if (this.value.length <= 1000) {
          hideFeedbackError();
        }
      });
    }

    // Handle form submission
    feedbackForm.addEventListener('submit', function (e) {
      e.preventDefault();

      const textarea = feedbackForm.querySelector('textarea');
      const feedbackText = textarea.value.trim();

      // Validate character count
      if (feedbackText.length > 1000) {
        showFeedbackError();

        // Track validation error
        trackEvent('feedback_validation_error', {
          page: window.location.pathname,
          characterCount: feedbackText.length,
          timestamp: new Date().toISOString()
        });

        return;
      }

      // Clear any existing errors
      hideFeedbackError();

      if (feedbackText) {
        // Track feedback submission
        trackEvent('feedback_submitted', {
          page: window.location.pathname,
          feedbackLength: feedbackText.length,
          timestamp: new Date().toISOString()
        });

        // Send feedback to the server
        const submitButton = feedbackForm.querySelector('button[type="submit"]');
        const originalButtonText = submitButton.textContent;
        submitButton.textContent = 'Submitting...';
        submitButton.disabled = true;

        fetch('/Contact/SubmitFeedback', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            feedbackFormInput: feedbackText
          })
        })
          .then(response => response.json())
          .then(data => {
            if (data.success) {
              // Show success message
              feedbackPanel.classList.remove('show');
              feedbackPanel.setAttribute('aria-hidden', 'true');
              thanksMessage.classList.add('show');

              // Clear the form and errors
              textarea.value = '';
              hideFeedbackError();

              // Focus on the thank you message for screen readers
              thanksMessage.focus();

              console.log('Feedback submitted successfully');
            } else {
              // Show error message
              alert('Sorry, there was an error submitting your feedback. Please try again.');
              console.error('Feedback submission failed:', data.message);
            }
          })
          .catch(error => {
            // Show error message
            alert('Sorry, there was an error submitting your feedback. Please try again.');
            console.error('Feedback submission error:', error);
          })
          .finally(() => {
            // Reset button state
            submitButton.textContent = originalButtonText;
            submitButton.disabled = false;
          });
      }
    });
  }
});

// Header search: icon + "Search" toggle, expandable panel, close button (GOV.UK style)
document.addEventListener('DOMContentLoaded', function () {
  const header = document.querySelector('.govuk-js-header-search');
  const toggle = document.querySelector('.govuk-js-header-search-toggle');
  const closeBtn = document.querySelector('.govuk-js-header-search-close');
  const panel = document.getElementById('header-search-panel');
  const input = document.getElementById('header-search-input');

  if (!header || !toggle || !panel) return;

  function openSearch() {
    panel.hidden = false;
    panel.setAttribute('aria-hidden', 'false');
    toggle.setAttribute('aria-expanded', 'true');
    if (input) {
      input.focus();
    }
  }

  function closeSearch() {
    panel.hidden = true;
    panel.setAttribute('aria-hidden', 'true');
    toggle.setAttribute('aria-expanded', 'false');
    toggle.focus();
  }

  function isOpen() {
    return toggle.getAttribute('aria-expanded') === 'true';
  }

  toggle.addEventListener('click', function () {
    if (isOpen()) {
      closeSearch();
    } else {
      openSearch();
    }
  });

  if (closeBtn) {
    closeBtn.addEventListener('click', closeSearch);
  }

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && isOpen()) {
      closeSearch();
    }
  });

  document.addEventListener('click', function (e) {
    if (!isOpen()) return;
    if (!header.contains(e.target)) {
      closeSearch();
    }
  });
});

// Track search interactions
document.addEventListener('DOMContentLoaded', function () {
  const searchForms = document.querySelectorAll('form[action*="search"], form[action*="Search"]');
  searchForms.forEach(function (form) {
    form.addEventListener('submit', function (e) {
      const searchInput = form.querySelector('input[type="search"], input[name*="search"], input[name*="Search"]');
      if (searchInput && searchInput.value.trim()) {
        trackEvent('search_performed', {
          searchTerm: searchInput.value.trim(),
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });
      }
    });
  });
});

// Track product clicks
document.addEventListener('DOMContentLoaded', function () {
  const productLinks = document.querySelectorAll('a[href*="/products/"], a[href*="/product/"]');
  productLinks.forEach(function (link) {
    link.addEventListener('click', function (e) {
      trackEvent('product_clicked', {
        productUrl: link.href,
        productText: link.textContent.trim(),
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    });
  });
});

// Track category clicks
document.addEventListener('DOMContentLoaded', function () {
  const categoryLinks = document.querySelectorAll('a[href*="/categories/"], a[href*="/category/"]');
  categoryLinks.forEach(function (link) {
    link.addEventListener('click', function (e) {
      trackEvent('category_clicked', {
        categoryUrl: link.href,
        categoryText: link.textContent.trim(),
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    });
  });
});

// Track errors
window.addEventListener('error', function (e) {
  trackEvent('javascript_error', {
    errorMessage: e.message,
    errorSource: e.filename,
    errorLine: e.lineno,
    errorColumn: e.colno,
    page: window.location.pathname,
    timestamp: new Date().toISOString()
  });
});

// Track unhandled promise rejections
window.addEventListener('unhandledrejection', function (e) {
  trackEvent('unhandled_promise_rejection', {
    errorMessage: e.reason ? e.reason.toString() : 'Unknown error',
    page: window.location.pathname,
    timestamp: new Date().toISOString()
  });
});



// Header Navigation and Search Functionality
document.addEventListener('DOMContentLoaded', function () {
  const toolsToggle = document.getElementById('super-tools-menu-toggle');
  const navigationToggle = document.getElementById('super-navigation-menu-toggle');
  const searchToggle = document.getElementById('super-search-menu-toggle');
  const toolsMenu = document.getElementById('super-tools-menu');
  const navigationMenu = document.getElementById('super-navigation-menu');
  const searchMenu = document.getElementById('super-search-menu');
  const searchForm = document.getElementById('search');

  // Function to close all menus and reset button states
  function closeAllMenus() {
    // Close tools
    if (toolsToggle && toolsMenu) {
      toolsToggle.setAttribute('aria-expanded', 'false');
      toolsMenu.setAttribute('hidden', 'hidden');
      toolsToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
    }

    // Close navigation
    if (navigationToggle && navigationMenu) {
      navigationToggle.setAttribute('aria-expanded', 'false');
      navigationMenu.setAttribute('hidden', 'hidden');
      navigationToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
    }

    // Close search
    if (searchToggle && searchMenu) {
      searchToggle.setAttribute('aria-expanded', 'false');
      searchMenu.setAttribute('hidden', 'hidden');
      searchToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
    }
  }

  // Toggle tools menu
  if (toolsToggle && toolsMenu) {
    toolsToggle.addEventListener('click', function () {
      const isHidden = toolsMenu.hasAttribute('hidden');

      if (isHidden) {
        toolsToggle.setAttribute('aria-expanded', 'true');
        toolsMenu.removeAttribute('hidden');
        toolsToggle.classList.add('gem-c-layout-super-navigation-header__open-button');

        trackEvent('header_tools_toggled', {
          action: 'open',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });

        if (navigationToggle && navigationMenu) {
          navigationToggle.setAttribute('aria-expanded', 'false');
          navigationMenu.setAttribute('hidden', 'hidden');
          navigationToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
        }

        if (searchToggle && searchMenu) {
          searchToggle.setAttribute('aria-expanded', 'false');
          searchMenu.setAttribute('hidden', 'hidden');
          searchToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
        }
      } else {
        toolsToggle.setAttribute('aria-expanded', 'false');
        toolsMenu.setAttribute('hidden', 'hidden');
        toolsToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');

        trackEvent('header_tools_toggled', {
          action: 'close',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });
      }
    });
  }

  // Toggle navigation menu
  if (navigationToggle && navigationMenu) {
    navigationToggle.addEventListener('click', function () {
      const isHidden = navigationMenu.hasAttribute('hidden');

      if (isHidden) {
        // Open navigation menu
        navigationToggle.setAttribute('aria-expanded', 'true');
        navigationMenu.removeAttribute('hidden');
        navigationToggle.classList.add('gem-c-layout-super-navigation-header__open-button');

        trackEvent('header_navigation_toggled', {
          action: 'open',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });

        // Close tools if open
        if (toolsToggle && toolsMenu) {
          toolsToggle.setAttribute('aria-expanded', 'false');
          toolsMenu.setAttribute('hidden', 'hidden');
          toolsToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
        }

        // Close search if open
        if (searchToggle && searchMenu) {
          searchToggle.setAttribute('aria-expanded', 'false');
          searchMenu.setAttribute('hidden', 'hidden');
          searchToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
        }
      } else {
        // Close navigation menu
        navigationToggle.setAttribute('aria-expanded', 'false');
        navigationMenu.setAttribute('hidden', 'hidden');
        navigationToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');

        trackEvent('header_navigation_toggled', {
          action: 'close',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });
      }
    });
  }

  // Toggle search panel
  if (searchToggle && searchMenu) {
    searchToggle.addEventListener('click', function () {
      const isHidden = searchMenu.hasAttribute('hidden');

      if (isHidden) {
        // Open search menu
        searchToggle.setAttribute('aria-expanded', 'true');
        searchMenu.removeAttribute('hidden');
        searchToggle.classList.add('gem-c-layout-super-navigation-header__open-button');

        trackEvent('header_search_toggled', {
          action: 'open',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });

        // Close tools if open
        if (toolsToggle && toolsMenu) {
          toolsToggle.setAttribute('aria-expanded', 'false');
          toolsMenu.setAttribute('hidden', 'hidden');
          toolsToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
        }

        // Close navigation if open
        if (navigationToggle && navigationMenu) {
          navigationToggle.setAttribute('aria-expanded', 'false');
          navigationMenu.setAttribute('hidden', 'hidden');
          navigationToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');
        }
      } else {
        // Close search menu
        searchToggle.setAttribute('aria-expanded', 'false');
        searchMenu.setAttribute('hidden', 'hidden');
        searchToggle.classList.remove('gem-c-layout-super-navigation-header__open-button');

        trackEvent('header_search_toggled', {
          action: 'close',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });
      }
    });
  }

  // Header search form submits normally to /search/all?keywords=...

  // Close panels on escape key
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      closeAllMenus();
    }
  });
});

// Track navigation clicks across header and service navigation
document.addEventListener('DOMContentLoaded', function () {
  var navLinks = document.querySelectorAll(
    '.gem-c-layout-super-navigation-header a[href], .govuk-service-navigation a[href]'
  );

  navLinks.forEach(function (link) {
    if (link._navigationTrackingWired) return;
    link._navigationTrackingWired = true;

    link.addEventListener('click', function () {
      var navigationType = link.closest('.gem-c-layout-super-navigation-header') ? 'header' : 'service';
      var section = getSectionHeadingText(link, '.govuk-grid-column-two-thirds-from-desktop, .govuk-grid-column-one-third-from-desktop, .govuk-service-navigation');

      trackEvent('navigation_link_clicked', {
        navigationType: navigationType,
        linkText: getElementLabel(link),
        linkUrl: link.href,
        section: section,
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    });
  });

  var serviceNavigationToggle = document.querySelector('.govuk-js-service-navigation-toggle');
  if (serviceNavigationToggle) {
    serviceNavigationToggle.addEventListener('click', function () {
      window.setTimeout(function () {
        trackEvent('service_navigation_toggled', {
          action: serviceNavigationToggle.getAttribute('aria-expanded') === 'true' ? 'open' : 'close',
          page: window.location.pathname,
          timestamp: new Date().toISOString()
        });
      }, 0);
    });
  }
});

// Track filter interactions for guidance, standards, search and library filter patterns
document.addEventListener('DOMContentLoaded', function () {
  document.body.addEventListener('click', function (e) {
    var filterLink = e.target.closest('.gf-link');
    if (filterLink) {
      trackEvent('filter_link_clicked', {
        filterLabel: getElementLabel(filterLink),
        filterUrl: filterLink.href,
        filterSection: getSectionHeadingText(filterLink, '.gf-block'),
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
      return;
    }

    var clearFilters = e.target.closest('.guidance-filters__clear');
    if (clearFilters) {
      trackEvent('filters_cleared', {
        clearUrl: clearFilters.href || '',
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    }
  });

  var filterForms = document.querySelectorAll('.guidance-filters form, #lib-type-filter-form');
  filterForms.forEach(function (form) {
    if (form._filterTrackingWired) return;
    form._filterTrackingWired = true;

    form.addEventListener('submit', function () {
      var checkedFilters = form.querySelectorAll('input[type="checkbox"]:checked').length;
      var searchInput = form.querySelector('input[type="search"], input[name="search"], input[name="keywords"]');

      trackEvent('filters_applied', {
        formAction: form.getAttribute('action') || window.location.pathname,
        filterSection: getSectionHeadingText(form, '.gf-block') || getElementLabel(form.closest('.guidance-filters') || form),
        checkedFilterCount: checkedFilters,
        searchTerm: searchInput ? searchInput.value.trim() : '',
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    });
  });
});

// Guide/Collection meta: expand/collapse audience "+N more"
document.addEventListener('DOMContentLoaded', function () {
  document.body.addEventListener('click', function (e) {
    var btn = e.target && e.target.closest && e.target.closest('.guide-meta__audience-toggle');
    if (!btn) return;
    e.preventDefault();
    var wrapper = btn.closest('.guide-meta__audience');
    if (!wrapper) return;
    var extra = wrapper.querySelector('.guide-meta__audience-extra');
    var extraCount = btn.getAttribute('data-extra-count') || '';
    var isExpanded = wrapper.classList.contains('guide-meta__audience--expanded');
    if (isExpanded) {
      wrapper.classList.remove('guide-meta__audience--expanded');
      btn.textContent = '+' + extraCount + ' more';
      btn.setAttribute('aria-expanded', 'false');
      if (extra) extra.setAttribute('aria-hidden', 'true');
    } else {
      wrapper.classList.add('guide-meta__audience--expanded');
      btn.textContent = 'Show less';
      btn.setAttribute('aria-expanded', 'true');
      if (extra) extra.setAttribute('aria-hidden', 'false');
    }
  });
}); 