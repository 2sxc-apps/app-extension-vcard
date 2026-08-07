#if NETCOREAPP // Oqtane
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#else // DNN
// 2sxclint:disable:no-web-namespace
using System.Web.Http;
#endif
using AppCode.Extensions.VCard;
using System.Threading.Tasks;

/// <summary>
/// Self-contained test endpoint for the vCard extension.
/// URL: /api/TestVCard/Download
/// </summary>
public class TestVCardController : Custom.Hybrid.ApiTyped
{
  [HttpGet]
  [AllowAnonymous]
  public async Task<object> Download()
  {
    // Deliberately uses hard-coded data: this proves that the extension is
    // independent of a specific 2sxc content type, query, or app.
    var card = new VCard
    {
      FirstName = "2sic",
      LastName = "2scilast",
      Organization = "2sic",
      JobTitle = "2sic Job Title",
      StreetAddress = "2sic street 123",
      Zip = "2222",
      City = "Buchs",
      CountryName = "Switzerland",
      Phone = "22131",
      Mobile = "1231232",
      Email = "info@2sic.net",
      Url = "2sic.net",
      FileName = "2sic test",
      PhotoUrl = AppIconUrl(),
      PhotoType = "PNG",
    };

    var result = await GetService<VCardService>().CreateAsync(card);
    return File(
      download: true,
      contents: result.Contents,
      contentType: result.ContentType,
      fileDownloadName: result.FileName
    );
  }

  private string AppIconUrl()
  {
    var relativeUrl = $"{App.Folder.Url}/app-icon.png";

#if NETCOREAPP // Oqtane
    return $"{Request.Scheme}://{Request.Host}{relativeUrl}";
#else // DNN
    var siteRoot = Request.RequestUri.GetLeftPart(System.UriPartial.Authority);
    return $"{siteRoot}{relativeUrl}";
#endif
  }
}
