using Microsoft.AspNetCore.Mvc;
using ServiceManual.Services;

namespace ServiceManual.Controllers
{
    public class JobSpecificationController : Controller
    {
        private readonly ICmsApiService _cmsApiService;

        public JobSpecificationController(ICmsApiService cmsApiService)
        {
            _cmsApiService = cmsApiService;
        }

        [Route("guidance/job-specifications/{slug}")]
        public async Task<IActionResult> Show(string slug, [FromQuery] string? collection)
        {
            var jobSpec = await _cmsApiService.GetJobSpecificationBySlugAsync(slug);

            if (jobSpec is null)
                return NotFound();

            ViewBag.JobSpecification = jobSpec;

            if (!string.IsNullOrWhiteSpace(collection))
            {
                var collectionData = await _cmsApiService.GetCollectionBySlugAsync(collection.Trim());
                if (collectionData != null)
                {
                    ViewBag.CollectionSlug = collectionData.Slug;
                    ViewBag.CollectionTitle = collectionData.Title;
                }
            }

            return View("~/Views/Templates/JobSpecification.cshtml");
        }

        [Route("guidance/job-specifications/{slug}/download")]
        public async Task<IActionResult> DownloadWord(string slug)
        {
            var jobSpec = await _cmsApiService.GetJobSpecificationBySlugAsync(slug);

            if (jobSpec is null || !jobSpec.EnableWordDocDownload)
                return NotFound();

            var docBytes = WordDocumentService.BuildJobSpecificationDocx(jobSpec);
            var fileName = $"{jobSpec.Title?.Replace("/", "-") ?? slug}.docx";

            return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
    }
}
