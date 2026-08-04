using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AppCode.Extensions.VCard
{
  /// <summary>Creates vCard files from data supplied by any 2sxc app.</summary>
  public class VCardService : Custom.Hybrid.CodeTyped
  {
    public VCardFile Create(VCard card)
    {
      if (card == null)
        throw new ArgumentNullException(nameof(card));

      return new VCardFile
      {
        Contents = new UTF8Encoding(false).GetBytes(Serialize(card)),
        ContentType = "text/vcard",
        FileName = BuildFileName(card),
      };
    }

    public string Serialize(VCard card)
    {
      if (card == null)
        throw new ArgumentNullException(nameof(card));

      return new StringBuilder()
        .AppendLine("BEGIN:VCARD")
        .AppendLine("VERSION:3.0")
        .AppendLine($"N:{card.LastName.Escape()};{card.FirstName.Escape()};;;")
        .AppendLine($"FN:{DisplayName(card).Escape()}")
        .AppendLineIfValue("ORG:", card.Organization)
        .AppendLineIfValue("TITLE:", card.JobTitle)
        .AppendLineIfAny(
          $"ADR;TYPE=WORK,PREF:;;{card.StreetAddress.Escape()};{card.City.Escape()};{card.Region.Escape()};{card.Zip.Escape()};{card.CountryName.Escape()}",
          card.StreetAddress,
          card.City,
          card.Region,
          card.Zip,
          card.CountryName
        )
        .AppendLineIfValue("TEL;TYPE=WORK,VOICE:", card.Phone)
        .AppendLineIfValue("X-MS-TEL;TYPE=VOICE,COMPANY:", card.PhoneCompany)
        .AppendLineIfValue("TEL;TYPE=CELL,VOICE:", card.Mobile)
        .AppendLineIfValue("EMAIL;TYPE=PREF,INTERNET:", card.Email)
        .AppendLineIfValue("URL;TYPE=WORK:", card.Url)
        .AppendLineIfRawValue(
          $"PHOTO;ENCODING=b;TYPE={PhotoType(card)}:",
          card.PhotoBase64?.Trim()
        )
        .AppendLine("END:VCARD")
        .ToString();
    }

    public async Task<string> DownloadPhotoAsync(string absoluteUrl)
    {
      if (string.IsNullOrWhiteSpace(absoluteUrl))
        return null;

      try
      {
        using (var client = new HttpClient())
          return Convert.ToBase64String(
            await client.GetByteArrayAsync(absoluteUrl)
          );
      }
      catch (Exception ex)
      {
        Log.Add($"Could not download the vCard photo from: {absoluteUrl}");
        Log.Exception(ex);
        return null;
      }
    }

    private static string DisplayName(VCard card)
    {
      var name = $"{card.FirstName} {card.LastName}".Trim();

      return string.IsNullOrWhiteSpace(name)
        ? card.Organization ?? "Contact"
        : name;
    }

    private static string BuildFileName(VCard card)
    {
      var requested = string.IsNullOrWhiteSpace(card.FileName)
        ? DisplayName(card)
        : card.FileName.Trim();

      var invalidCharacters = Path.GetInvalidFileNameChars();
      var safeName = new string(
          requested
            .Where(character => !invalidCharacters.Contains(character))
            .ToArray()
        )
        .Trim()
        .Trim('.');

      if (string.IsNullOrWhiteSpace(safeName))
        safeName = "contact";

      return safeName.EndsWith(".vcf", StringComparison.OrdinalIgnoreCase)
        ? safeName
        : $"{safeName}.vcf";
    }

    private static string PhotoType(VCard card)
      => string.IsNullOrWhiteSpace(card.PhotoType)
        ? "JPEG"
        : card.PhotoType.ToUpperInvariant();
  }
}

