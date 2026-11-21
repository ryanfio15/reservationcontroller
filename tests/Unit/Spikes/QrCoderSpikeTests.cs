using System.Reflection;
using System.Runtime.Versioning;
using QRCoder;
using Unit.Architecture;

namespace Unit.Spikes;

/// <summary>
/// Phase 0 spike: "QRCoder renders under WebAssembly" (A5 phase 0).
/// </summary>
/// <remarks>
/// The risk is not the encoder but the renderer: QRCoder's bitmap renderers need
/// <c>System.Drawing.Common</c>, which does not exist on the browser runtime. These tests pin
/// the two facts the frontend depends on — that <see cref="PngByteQRCode"/> produces a PNG from
/// managed code alone, and that the package carries a target the WebAssembly client can use.
/// A full browser render is proved by the Playwright journey in phase 7.
/// </remarks>
[Trait("Category", "Spike")]
public sealed class QrCoderSpikeTests
{
    // The shape of a real ticket reference (B: Reservation ticket security), so the payload
    // length and character set are representative rather than a toy string.
    private const string TicketReference =
        "R1.k3.0193f0c2-6d1f-7a80-9c21-5b2e8a41d7c6.9f8a1c0d4b6e2f35";

    [Fact]
    public void PngByteQRCode_renders_a_png_without_System_Drawing()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(TicketReference, QRCodeGenerator.ECCLevel.Q);

        var png = new PngByteQRCode(data).GetGraphic(pixelsPerModule: 8);

        Assert.NotEmpty(png);

        // PNG signature: 89 50 4E 47 0D 0A 1A 0A.
        Assert.Equal<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], png[..8]);
    }

    [Fact]
    public void The_shared_renderer_uses_only_the_managed_PNG_path()
    {
        // The finding behind this test: QRCoder's net6.0 assembly does reference
        // System.Drawing.Common, which has no implementation on the browser runtime. A reference
        // is harmless as long as no reachable code path needs it, which is why
        // App.UI.QrCodeRenderer is the only way this solution renders a code, and why it uses
        // PngByteQRCode alone. That the trimmer agrees was checked by publishing the client,
        // which emits QRCoder into _framework and no System.Drawing assembly at all
        // (docs/DECISIONS.md D8).
        Assert.Contains(
            "System.Drawing.Common",
            typeof(PngByteQRCode).Assembly.GetReferencedAssemblies()
                .Select(name => name.Name ?? string.Empty));

        var renderer = AssemblyDependencies.CallersOf(
            typeof(App.UI.QrCodeRenderer).Assembly,
            "System.Drawing.Bitmap::.ctor",
            "QRCoder.QRCode::GetGraphic",
            "QRCoder.ArtQRCode::GetGraphic",
            "QRCoder.BitmapByteQRCode::GetGraphic");

        Assert.True(
            renderer.Count == 0,
            "App.UI reaches a QRCoder renderer that needs System.Drawing:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, renderer));
    }

    [Fact]
    public void The_shared_renderer_produces_a_png_data_url()
    {
        var url = App.UI.QrCodeRenderer.ToPngDataUrl(TicketReference);

        Assert.StartsWith("data:image/png;base64,", url, StringComparison.Ordinal);
        Assert.True(url.Length > 100, "The data URL is too short to be a rendered code.");
    }

    [Fact]
    public void The_png_renderer_is_deterministic()
    {
        // The ticket screen re-renders on every visit and the host stand caches it, so the same
        // reference must give the same image.
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(TicketReference, QRCodeGenerator.ECCLevel.Q);

        var first = new PngByteQRCode(data).GetGraphic(pixelsPerModule: 8);
        var second = new PngByteQRCode(data).GetGraphic(pixelsPerModule: 8);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_resolved_assembly_targets_a_framework_the_browser_runtime_can_load()
    {
        var framework = typeof(PngByteQRCode).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            ?.FrameworkName;

        Assert.NotNull(framework);

        // .NET or .NET Standard load on the WebAssembly runtime; a .NET Framework-only
        // assembly would not.
        Assert.DoesNotContain(".NETFramework", framework, StringComparison.Ordinal);
    }
}
