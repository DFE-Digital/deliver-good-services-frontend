using Microsoft.AspNetCore.Mvc;

namespace ServiceManual.Controllers;

public class DocumentationController : Controller
{
    [Route("documentation")]
    public IActionResult Index() => View("~/Views/Documentation/Index.cshtml");

    [Route("documentation/styles")]
    public IActionResult StylesIndex() => View("~/Views/Documentation/Styles/Index.cshtml");

    [Route("documentation/styles/typography")]
    public IActionResult StylesTypography() => View("~/Views/Documentation/Typography.cshtml");

    [Route("documentation/styles/headings")]
    public IActionResult StylesHeadings() => View("~/Views/Documentation/Headings.cshtml");

    [Route("documentation/styles/links")]
    public IActionResult StylesLinks() => View("~/Views/Documentation/Links.cshtml");

    [Route("documentation/styles/inset-text")]
    public IActionResult StylesInsetText() => View("~/Views/Documentation/InsetText.cshtml");

    [Route("documentation/styles/tables")]
    public IActionResult StylesTables() => View("~/Views/Documentation/Tables.cshtml");

    [Route("documentation/styles/horizontal-rule")]
    public IActionResult StylesHorizontalRule() => View("~/Views/Documentation/HorizontalRule.cshtml");

    [Route("documentation/styles/colour")]
    public IActionResult StylesColour() => View("~/Views/Documentation/Colour.cshtml");

    [Route("documentation/components")]
    public IActionResult ComponentsIndex() => View("~/Views/Documentation/Components/Index.cshtml");

    [Route("documentation/components/markdown")]
    public IActionResult ComponentsMarkdown() => View("~/Views/Documentation/Markdown.cshtml");

    [Route("documentation/components/action-link")]
    public IActionResult ComponentsActionLink() => View("~/Views/Documentation/ActionLink.cshtml");

    [Route("documentation/components/panel")]
    public IActionResult ComponentsPanel() => View("~/Views/Documentation/PanelComponent.cshtml");

    [Route("documentation/components/pill")]
    public IActionResult ComponentsPill() => View("~/Views/Documentation/PillComponent.cshtml");

    [Route("documentation/components/cards")]
    public IActionResult ComponentsCards() => View("~/Views/Documentation/Cards.cshtml");

    [Route("documentation/components/card-list")]
    public IActionResult ComponentsCardList() => View("~/Views/Documentation/ChevronCards.cshtml");

    [Route("documentation/components/related-content")]
    public IActionResult ComponentsRelatedContent() => View("~/Views/Documentation/RelatedContent.cshtml");

    [Route("documentation/components/phase-components")]
    public IActionResult ComponentsPhaseComponents() => View("~/Views/Documentation/PhaseComponents.cshtml");

    [Route("documentation/patterns")]
    public IActionResult PatternsIndex() => View("~/Views/Documentation/Patterns/Index.cshtml");

    [Route("documentation/templates")]
    public IActionResult TemplatesIndex() => View("~/Views/Documentation/Templates/Index.cshtml");

    [Route("documentation/templates/collection")]
    public IActionResult TemplatesCollection() => View("~/Views/Documentation/Templates/Collection.cshtml");

    [Route("documentation/templates/detailed-guide")]
    public IActionResult TemplatesDetailedGuide() => View("~/Views/Documentation/Templates/DetailedGuide.cshtml");

    [Route("documentation/templates/detailed-guide-page")]
    public IActionResult TemplatesDetailedGuidePage() => View("~/Views/Documentation/Templates/DetailedGuidePage.cshtml");

    [Route("documentation/templates/single-page-guide")]
    public IActionResult TemplatesSinglePageGuide() => View("~/Views/Documentation/Templates/SinglePageGuide.cshtml");

    [Route("documentation/templates/redirector")]
    public IActionResult TemplatesRedirector() => View("~/Views/Documentation/Templates/Redirector.cshtml");

    [Route("documentation/publishing")]
    public IActionResult PublishingIndex() => View("~/Views/Documentation/Publishing/Index.cshtml");

    [Route("documentation/publishing/lifecycle")]
    public IActionResult PublishingLifecycle() => View("~/Views/Documentation/Publishing/Lifecycle.cshtml");

    [Route("documentation/configuration")]
    public IActionResult ConfigurationIndex() => View("~/Views/Documentation/Configuration/Index.cshtml");

    [Route("documentation/configuration/cms")]
    public IActionResult ConfigurationCms() => View("~/Views/Documentation/Configuration/Cms.cshtml");
}
