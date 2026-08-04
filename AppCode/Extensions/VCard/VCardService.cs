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

      return new VCardFile {
        Contents = new UTF8Encoding(false).GetBytes(Serialize(card)),
        ContentType = "text/vcard",
        FileName = BuildFileName(card),
      };
    }

    public string Serialize(VCard card)
    {
      if (card == null) 
        throw new ArgumentNullException(nameof(card));

      var lines = new StringBuilder();

      Add(lines, "BEGIN:VCARD");
      Add(lines, "VERSION:3.0");
      Add(lines, "N:" + Escape(card.LastName) + ";" + Escape(card.FirstName) + ";;;");
      Add(lines, "FN:" + Escape(DisplayName(card)));
      AddValue(lines, "ORG:", card.Organization);
      AddValue(lines, "TITLE:", card.JobTitle);

      if (HasAny(card.StreetAddress, card.City, card.Region, card.Zip, card.CountryName))
        Add(lines, "ADR;TYPE=WORK,PREF:;;" + Escape(card.StreetAddress) + ";" + Escape(card.City) + ";" + Escape(card.Region) + ";" + Escape(card.Zip) + ";" + Escape(card.CountryName));
      
      AddValue(lines, "TEL;TYPE=WORK,VOICE:", card.Phone);
      AddValue(lines, "X-MS-TEL;TYPE=VOICE,COMPANY:", card.PhoneCompany);
      AddValue(lines, "TEL;TYPE=CELL,VOICE:", card.Mobile);
      AddValue(lines, "EMAIL;TYPE=PREF,INTERNET:", card.Email);
      AddValue(lines, "URL;TYPE=WORK:", card.Url);
      
      if (!string.IsNullOrWhiteSpace(card.PhotoBase64))
        Add(lines, "PHOTO;ENCODING=b;TYPE=" + SafePhotoType(card.PhotoType) + ":" + card.PhotoBase64.Trim());
      
      Add(lines, "END:VCARD");
      return lines.ToString();
    }

    public async Task<string> DownloadPhotoAsync(string absoluteUrl)
    {
      if (string.IsNullOrWhiteSpace(absoluteUrl)) 
        return null;

      try {
        using (var client = new HttpClient())
          return Convert.ToBase64String(await client.GetByteArrayAsync(absoluteUrl));
      }
      catch (Exception ex) {
        Log.Add("Could not download the vCard photo from: " + absoluteUrl);
        Log.Exception(ex);
        return null;
      }
    }

    private static string DisplayName(VCard card)
    {
      var name = (card.FirstName + " " + card.LastName).Trim();
      return string.IsNullOrWhiteSpace(name) ? card.Organization ?? "Contact" : name;
    }

    private static string BuildFileName(VCard card)
    {
      var requested = string.IsNullOrWhiteSpace(card.FileName) ? DisplayName(card) : card.FileName.Trim();
      var invalid = Path.GetInvalidFileNameChars();
      var safe = new string(requested.Where(c => !invalid.Contains(c)).ToArray()).Trim().Trim('.');

      if (string.IsNullOrWhiteSpace(safe)) 
        safe = "contact";

      return safe.EndsWith(".vcf", StringComparison.OrdinalIgnoreCase) ? safe : safe + ".vcf";
    }

    private static string Escape(string value) => (value ?? string.Empty)
      .Replace("\\", "\\\\").Replace("\r\n", "\\n").Replace("\n", "\\n")
      .Replace("\r", "\\n").Replace(";", "\\;").Replace(",", "\\,");

    private static bool HasAny(params string[] values) => values.Any(v => !string.IsNullOrWhiteSpace(v));
    private static string SafePhotoType(string value)
    {
      var safe = new string((value ?? "JPEG").Where(char.IsLetterOrDigit).ToArray());
      return string.IsNullOrWhiteSpace(safe) ? "JPEG" : safe.ToUpperInvariant();
    }
    private static void AddValue(StringBuilder builder, string prefix, string value)
    { if (!string.IsNullOrWhiteSpace(value)) Add(builder, prefix + Escape(value)); }
    private static void Add(StringBuilder builder, string line) => builder.Append(line).Append("\r\n");
  }

  public class VCardFile
  {
    public byte[] Contents { get; set; }
    public string ContentType { get; set; }
    public string FileName { get; set; }
  }
}

