using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using QRCoder;
using SyncSyntax.Models; // <-- where AppUser is (adjust if needed)

public class QrController : Controller
{
    private readonly UserManager<AppUser> _userManager;

    public QrController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("/qr/u/{username}")]
    public async Task<IActionResult> ProfileQr(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return NotFound();

        // QR will open your existing profile page
        var profileUrl = Url.Action(
            action: "Profile",
            controller: "Following",
            values: new { area = "ContentCreator", userId = user.Id },
            protocol: Request.Scheme
        );

        if (string.IsNullOrWhiteSpace(profileUrl))
            return Problem("Could not generate profile URL. Check routing.");

        var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(profileUrl, QRCodeGenerator.ECCLevel.Q);

        var pngQr = new PngByteQRCode(data);
        byte[] bytes = pngQr.GetGraphic(20);

        return File(bytes, "image/png");
    }
}
