using Microsoft.AspNetCore.Mvc;

namespace ServiceManual.Controllers;

public class DocumentationController : Controller
{
    [Route("documentation")]
    public IActionResult Index() => View("~/Views/Documentation/Index.cshtml");

    [Route("documentation/typography")]
    public IActionResult Typography() => View("~/Views/Documentation/Typography.cshtml");

    [Route("documentation/markdown")]
    public IActionResult Markdown() => View("~/Views/Documentation/Markdown.cshtml");

    [Route("documentation/panel-component")]
    public IActionResult PanelComponent() => View("~/Views/Documentation/PanelComponent.cshtml");

    [Route("documentation/pill-component")]
    public IActionResult PillComponent() => View("~/Views/Documentation/PillComponent.cshtml");

    [Route("documentation/links")]
    public IActionResult Links() => View("~/Views/Documentation/Links.cshtml");

    [Route("documentation/action-link")]
    public IActionResult ActionLink() => View("~/Views/Documentation/ActionLink.cshtml");

    [Route("documentation/headings")]
    public IActionResult Headings() => View("~/Views/Documentation/Headings.cshtml");

    [Route("documentation/inset-text")]
    public IActionResult InsetText() => View("~/Views/Documentation/InsetText.cshtml");

    [Route("documentation/tables")]
    public IActionResult Tables() => View("~/Views/Documentation/Tables.cshtml");

    [Route("documentation/horizontal-rule")]
    public IActionResult HorizontalRule() => View("~/Views/Documentation/HorizontalRule.cshtml");

    [Route("documentation/colour")]
    public IActionResult Colour() => View("~/Views/Documentation/Colour.cshtml");

    [Route("documentation/cards")]
    public IActionResult Cards() => View("~/Views/Documentation/Cards.cshtml");

    [Route("documentation/chevron-cards")]
    public IActionResult ChevronCards() => View("~/Views/Documentation/ChevronCards.cshtml");

    [Route("documentation/related-content")]
    public IActionResult RelatedContent() => View("~/Views/Documentation/RelatedContent.cshtml");

    [Route("documentation/phase-components")]
    public IActionResult PhaseComponents() => View("~/Views/Documentation/PhaseComponents.cshtml");
}
