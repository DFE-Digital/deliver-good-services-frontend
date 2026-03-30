(function () {
    function parseJsonScript(id, fallback) {
        var el = document.getElementById(id);
        if (!el) return fallback;

        try {
            return JSON.parse(el.textContent || "");
        } catch (err) {
            console.error("how_many_people_parse_error", { id: id, err: err });
            return fallback;
        }
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function toNumber(value) {
        if (typeof value === "number") return Number.isFinite(value) ? value : 0;
        if (typeof value !== "string") return 0;

        var parsed = Number(value.replace(/[^0-9.-]/g, ""));
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function formatNumber(value) {
        return new Intl.NumberFormat("en-GB").format(Math.round(value));
    }

    function formatPercent(value) {
        if (!Number.isFinite(value)) return "0%";
        return value % 1 === 0 ? value + "%" : value.toFixed(1) + "%";
    }

    function prettifyLargeNumber(value) {
        if (value === 69850000) return "UK population (69.85 million)";
        if (value >= 1000000 && value % 1000000 === 0) {
            var millions = value / 1000000;
            return millions === 1 ? "1 million" : millions + " million";
        }

        return formatNumber(value);
    }

    function parseQuickPicks(raw) {
        var ukPopulation = 69850000;
        var defaults = [1000, 10000, 100000, 1000000];

        var csv = typeof raw === "string" ? raw.trim() : "";
        var baseValues = csv
            ? csv
                .split(",")
                .map(function (item) {
                    return Math.round(toNumber(item.trim()));
                })
                .filter(function (value) {
                    return value > 0;
                })
            : defaults.slice();

        if (!baseValues.length) {
            baseValues = defaults.slice();
        }

        var seen = {};
        var unique = [];

        baseValues.forEach(function (value) {
            if (!seen[value]) {
                seen[value] = true;
                unique.push(value);
            }
        });

        // Always put UK population at the end of quick picks.
        unique = unique.filter(function (value) {
            return value !== ukPopulation;
        });
        unique.push(ukPopulation);

        return unique.map(function (value) {
            return {
                value: value,
                label: prettifyLargeNumber(value)
            };
        });
    }

    function firstObjectArray(source, keys) {
        for (var i = 0; i < keys.length; i += 1) {
            var list = source ? source[keys[i]] : null;
            if (Array.isArray(list)) return list;
        }

        return [];
    }

    function firstString(source, keys) {
        for (var i = 0; i < keys.length; i += 1) {
            var value = source ? source[keys[i]] : null;
            if (typeof value === "string" && value.trim()) return value.trim();
        }

        return "";
    }

    function firstNumber(source, keys) {
        for (var i = 0; i < keys.length; i += 1) {
            var value = source ? source[keys[i]] : null;
            var numeric = toNumber(value);
            if (numeric > 0 || value === 0 || value === "0") return numeric;
        }

        return 0;
    }

    function normaliseData(raw) {
        var categories = firstObjectArray(raw, [
            "categories",
            "disabilityCategories",
            "groups",
            "sections"
        ]).map(function (category, index) {
            var conditions = firstObjectArray(category, [
                "conditions",
                "items",
                "rows",
                "entries"
            ]).map(function (condition) {
                var links = firstObjectArray(condition, ["links", "relatedLinks", "guidanceLinks"]).map(function (link) {
                    var url = firstString(link, ["url", "href"]);
                    if (!url) return null;

                    return {
                        type: firstString(link, ["type", "tag", "kind"]) || "Guidance",
                        text: firstString(link, ["text", "label", "title"]) || url,
                        url: url
                    };
                }).filter(Boolean);

                var sourceObject = condition && typeof condition.source === "object" ? condition.source : null;

                return {
                    name: firstString(condition, ["name", "title", "condition"]),
                    detail: firstString(condition, ["detail", "description", "summary"]),
                    note: firstString(condition, ["designNote", "note", "recommendation", "action"]),
                    percentage: firstNumber(condition, ["percentage", "percent", "pct", "rate"]),
                    sourceText: firstString(sourceObject || condition, ["label", "sourceText", "sourceName", "source", "sourceLabel"]),
                    sourceUrl: firstString(sourceObject || condition, ["url", "sourceUrl", "sourceHref", "sourceLink"]),
                    links: links
                };
            }).filter(function (condition) {
                return !!condition.name && condition.percentage >= 0;
            });

            return {
                name: firstString(category, ["label", "name", "title", "category"]) || "Category " + (index + 1),
                color: firstString(category, ["color", "colour", "colorVar", "cssColor"]) || "var(--govuk-colour-blue)",
                conditions: conditions
            };
        }).filter(function (category) {
            return category.conditions.length > 0;
        });

        return {
            categories: categories,
            footnote: firstString(raw, ["footnote", "footerNote", "resultsFootnote"])
        };
    }

    function safeInputValue(input) {
        var numeric = toNumber(input.value);

        if (!numeric || numeric < 1) {
            numeric = 1;
        }

        if (numeric > 999999999) {
            numeric = 999999999;
        }

        input.value = String(Math.round(numeric));
        return Math.round(numeric);
    }

    function updateResultUrl(value) {
        if (!window.history || typeof window.history.replaceState !== "function") return;

        var currentPath = window.location.pathname || "";
        var basePath = currentPath.replace(/\/\d+\/?$/, "").replace(/\/$/, "");

        if (!basePath) {
            basePath = "/tools/how-many-people";
        }

        var nextPath = basePath + "/" + Math.max(1, Math.round(toNumber(value)));
        var nextUrl = nextPath + (window.location.search || "") + (window.location.hash || "");

        if (nextUrl !== window.location.pathname + window.location.search + window.location.hash) {
            window.history.replaceState(null, "", nextUrl);
        }
    }

    function renderPresets(container, picks, onPick) {
        if (!container) return;

        var html = '<span class="calc-preset-label">Quick picks:</span>';
        picks.forEach(function (pick) {
            html +=
                '<button class="calc-preset" type="button" data-preset="' +
                escapeHtml(String(pick.value)) +
                '">' +
                escapeHtml(pick.label) +
                "</button>";
        });

        container.innerHTML = html;

        container.querySelectorAll("[data-preset]").forEach(function (button) {
            button.addEventListener("click", function () {
                var value = toNumber(button.getAttribute("data-preset"));
                onPick(value);
            });
        });
    }

    function renderResults(outputEl, modelledUsers, data) {
        if (!outputEl) return;

        if (!data.categories.length) {
            outputEl.innerHTML =
                '<div class="govuk-warning-text govuk-!-margin-top-5">' +
                '<span class="govuk-warning-text__icon" aria-hidden="true">!</span>' +
                '<strong class="govuk-warning-text__text">' +
                '<span class="govuk-warning-text__assistive">Warning</span>' +
                'No disability data has been configured in CMS yet.' +
                "</strong></div>";
            return;
        }

        var allConditions = data.categories.flatMap(function (category) {
            return category.conditions;
        });

        var summaryHtml =
            '<div class="calc-summary-bar">' +
            '<div class="calc-summary-cell"><span class="calc-summary-n">' + escapeHtml(formatNumber(modelledUsers)) + '</span><span class="calc-summary-l">Users</span></div>' +
            '<div class="calc-summary-cell"><span class="calc-summary-n">' + escapeHtml(String(data.categories.length)) + '</span><span class="calc-summary-l">Categories</span></div>' +
            '<div class="calc-summary-cell"><span class="calc-summary-n">' + escapeHtml(String(allConditions.length)) + '</span><span class="calc-summary-l">Conditions</span></div>' +
            "</div>";

        var categoriesHtml = data.categories.map(function (category) {
            var rowsHtml = category.conditions.map(function (condition) {
                var conditionCount = Math.round((condition.percentage / 100) * modelledUsers);
                var width = Math.max(0, Math.min(100, condition.percentage));

                var linksHtml = condition.links.length
                    ? '<div class="calc-row__links">' +
                    condition.links.map(function (link) {
                        var typeClass = "calc-link--" + String(link.type).toLowerCase().replace(/[^a-z0-9]+/g, "-");
                        return (
                            '<a class="calc-link ' + typeClass + '" href="' +
                            escapeHtml(link.url) +
                            '"' +
                            (link.url.startsWith("http") ? ' target="_blank" rel="noopener"' : "") +
                            '><span class="calc-link__tag">' +
                            escapeHtml(link.type) +
                            "</span> " +
                            escapeHtml(link.text) +
                            "</a>"
                        );
                    }).join("") +
                    "</div>"
                    : "";

                var sourceHtml = condition.sourceText && condition.sourceUrl
                    ? '<div class="calc-row__source"><span class="calc-row__src-label">Source</span><a class="calc-row__src-link" href="' + escapeHtml(condition.sourceUrl) + '" target="_blank" rel="noopener">' + escapeHtml(condition.sourceText) + ' -></a></div>'
                    : "";

                return (
                    '<div class="calc-row">' +
                    '<div class="calc-row__left">' +
                    '<p class="calc-row__name">' + escapeHtml(condition.name) + "</p>" +
                    (condition.detail ? '<p class="calc-row__detail">' + escapeHtml(condition.detail) + "</p>" : "") +
                    (condition.note ? '<div class="calc-row__note">' + escapeHtml(condition.note) + "</div>" : "") +
                    linksHtml +
                    sourceHtml +
                    "</div>" +
                    '<div class="calc-row__right">' +
                    '<span class="calc-row__count">' + escapeHtml(formatNumber(conditionCount)) + "</span>" +
                    '<span class="calc-row__pct">' + escapeHtml(formatPercent(condition.percentage)) + "</span>" +
                    '<div class="calc-row__bar-wrap"><div class="calc-row__bar-fill" style="width:' + escapeHtml(width.toFixed(2)) + "%;background:" + escapeHtml(category.color) + '"></div></div>' +
                    "</div>" +
                    "</div>"
                );
            }).join("");

            return (
                '<div class="calc-category" style="--calc-c:' + escapeHtml(category.color) + '">' +
                '<div class="calc-cat-hd"><span class="calc-cat-pill">' + escapeHtml(category.name) + '</span><span class="calc-cat-count">' + escapeHtml(String(category.conditions.length)) + ' condition' + (category.conditions.length === 1 ? "" : "s") + "</span></div>" +
                rowsHtml +
                "</div>"
            );
        }).join("");

        var footnoteHtml = data.footnote ? '<p class="calc-footnote">' + escapeHtml(data.footnote) + "</p>" : "";

        outputEl.innerHTML = summaryHtml + categoriesHtml + footnoteHtml;
    }

    function initHowManyPeople() {
        var input = document.getElementById("calc-n");
        var output = document.getElementById("calc-output");
        var calculateButton = document.getElementById("calc-go");
        var presetsContainer = document.getElementById("calc-presets");

        if (!input || !output || !calculateButton || !presetsContainer) return;

        var rawData = parseJsonScript("how-many-people-data", {});
        var rawQuickPicks = parseJsonScript("how-many-people-quick-picks", "");
        var normalisedData = normaliseData(rawData || {});
        var quickPicks = parseQuickPicks(rawQuickPicks);

        function runCalc() {
            var value = safeInputValue(input);
            renderResults(output, value, normalisedData);
            updateResultUrl(value);
        }

        function setCalc(value) {
            var safeValue = Math.max(1, Math.round(toNumber(value)));
            input.value = String(safeValue);
            runCalc();
        }

        renderPresets(presetsContainer, quickPicks, setCalc);

        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                runCalc();
            }
        });

        calculateButton.addEventListener("click", runCalc);

        if (String(input.value || "").trim() !== "") {
            runCalc();
        }

        window.calcRun = runCalc;
        window.calcSet = setCalc;
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initHowManyPeople);
    } else {
        initHowManyPeople();
    }
})();
