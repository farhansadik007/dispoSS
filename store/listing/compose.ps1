param(
    [string]$RawDirectory = $PSScriptRoot,
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fontDirectory = Join-Path $repoRoot 'src\DisposableScreenshots\Assets\Fonts'
$pixelFontPath = Join-Path $fontDirectory 'PixelOperator8-Bold.ttf'
$displayFontPath = Join-Path $fontDirectory '04B_30__.TTF'

$fonts = New-Object System.Drawing.Text.PrivateFontCollection
if (Test-Path $pixelFontPath) { $fonts.AddFontFile($pixelFontPath) }
if (Test-Path $displayFontPath) { $fonts.AddFontFile($displayFontPath) }

$pixelFamily = $fonts.Families | Where-Object { $_.Name -match 'Pixel Operator' } | Select-Object -First 1
$displayFamily = $fonts.Families | Where-Object { $_.Name -match '04b' } | Select-Object -First 1
if (-not $pixelFamily) { $pixelFamily = New-Object System.Drawing.FontFamily('Segoe UI') }
if (-not $displayFamily) { $displayFamily = New-Object System.Drawing.FontFamily('Segoe UI') }

$canvasWidth = 1920
$canvasHeight = 1080
$cream = [System.Drawing.Color]::FromArgb(255, 249, 236, 214)
$coral = [System.Drawing.Color]::FromArgb(255, 255, 112, 92)
$gold = [System.Drawing.Color]::FromArgb(255, 245, 184, 78)
$navy = [System.Drawing.Color]::FromArgb(255, 20, 24, 48)

function New-Background {
    $bitmap = New-Object System.Drawing.Bitmap($canvasWidth, $canvasHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Rectangle(0, 0, $canvasWidth, $canvasHeight)),
        [System.Drawing.Color]::FromArgb(255, 15, 18, 38),
        [System.Drawing.Color]::FromArgb(255, 47, 24, 58),
        90.0)
    $graphics.FillRectangle($brush, 0, 0, $canvasWidth, $canvasHeight)
    $brush.Dispose()

    # Subtle scanline texture.
    $scan = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(10, 255, 255, 255), 1)
    for ($y = 0; $y -lt $canvasHeight; $y += 8) {
        $graphics.DrawLine($scan, 0, $y, $canvasWidth, $y)
    }
    $scan.Dispose()

    # The window screenshots carry the product name, so the canvas stays uncluttered.

    return @($bitmap, $graphics)
}

function Add-RoundedClip {
    param($Graphics, $Rectangle, [int]$Radius)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($Rectangle.X, $Rectangle.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rectangle.X, $Rectangle.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Add-WindowImage {
    param(
        $Graphics,
        [string]$Path,
        [int]$X,
        [int]$Y,
        [int]$Scale = 2,
        [int]$Radius = 38,
        [int]$InsetLeft = 10,
        [int]$InsetTop = 10,
        [int]$InsetRight = 10,
        [int]$InsetBottom = 22,
        [int]$SourceRadius = 0
    )

    $source = [System.Drawing.Image]::FromFile($Path)
    $sourceRect = New-Object System.Drawing.Rectangle($InsetLeft, $InsetTop, ($source.Width - $InsetLeft - $InsetRight), ($source.Height - $InsetTop - $InsetBottom))
    $width = [int]($sourceRect.Width * $Scale)
    $height = [int]($sourceRect.Height * $Scale)

    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
    $shadowPath = Add-RoundedClip $Graphics (New-Object System.Drawing.Rectangle(($X + 14), ($Y + 18), $width, $height)) $Radius
    $Graphics.FillPath($shadowBrush, $shadowPath)
    $shadowPath.Dispose()
    $shadowBrush.Dispose()

    $windowBitmap = $null
    if ($SourceRadius -gt 0) {
        $windowBitmap = New-Object System.Drawing.Bitmap($sourceRect.Width, $sourceRect.Height)
        $windowGraphics = [System.Drawing.Graphics]::FromImage($windowBitmap)
        $windowGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $windowGraphics.Clear([System.Drawing.Color]::Transparent)
        $sourceClip = Add-RoundedClip $windowGraphics (New-Object System.Drawing.Rectangle(0, 0, $sourceRect.Width, $sourceRect.Height)) $SourceRadius
        $windowGraphics.SetClip($sourceClip)
        $windowGraphics.DrawImage($source, (New-Object System.Drawing.Rectangle(0, 0, $sourceRect.Width, $sourceRect.Height)), $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
        $windowGraphics.ResetClip()
        $sourceClip.Dispose()
        $windowGraphics.Dispose()
    }

    $clip = Add-RoundedClip $Graphics (New-Object System.Drawing.Rectangle($X, $Y, $width, $height)) $Radius
    $Graphics.SetClip($clip)
    $Graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    if ($windowBitmap) {
        $Graphics.DrawImage($windowBitmap, (New-Object System.Drawing.Rectangle($X, $Y, $width, $height)))
        $windowBitmap.Dispose()
    }
    else {
        $Graphics.DrawImage($source, (New-Object System.Drawing.Rectangle($X, $Y, $width, $height)), $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
    }
    $Graphics.ResetClip()
    $clip.Dispose()
    $source.Dispose()
}

function Write-Caption {
    param(
        $Graphics,
        [string]$Heading,
        [string]$Subheading,
        [int]$X = 100,
        [int]$Y = 40,
        [int]$HeadingSize = 46,
        [int]$SubheadingSize = 20
    )

    $headingFont = New-Object System.Drawing.Font($displayFamily, $HeadingSize, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
    $subFont = New-Object System.Drawing.Font($pixelFamily, $SubheadingSize, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
    $headingBrush = New-Object System.Drawing.SolidBrush($cream)
    $subBrush = New-Object System.Drawing.SolidBrush($coral)

    $Graphics.DrawString($Heading, $headingFont, $headingBrush, $X, $Y)
    $Graphics.DrawString($Subheading, $subFont, $subBrush, ($X + 2), ($Y + $HeadingSize + 14))

    $headingFont.Dispose()
    $subFont.Dispose()
    $headingBrush.Dispose()
    $subBrush.Dispose()
}

function Save-Canvas {
    param($Bitmap, [string]$Name)
    $path = Join-Path $OutputDirectory $Name
    $Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Bitmap.Dispose()
    Write-Host "Wrote $path"
}

$mainRaw = Join-Path $RawDirectory '_raw-main.png'
$settingsRaw = Join-Path $RawDirectory '_raw-settings.png'
$previewRaw = Join-Path $RawDirectory '_raw-preview.png'
$aboutRaw = Join-Path $RawDirectory '_raw-about.png'

# Screenshot 1: main window.
$canvas = New-Background
$bitmap = $canvas[0]; $graphics = $canvas[1]
Write-Caption $graphics 'CLIPBOARD-FIRST SCREENSHOTS.' 'NOTHING IS SAVED UNLESS YOU SAY SO.' 100 24 44 19
Add-WindowImage $graphics $mainRaw 340 110 2 40 -InsetLeft 0 -InsetTop 0 -InsetRight 14 -InsetBottom 14 -SourceRadius 26
Save-Canvas $bitmap 'Screenshot-1-Main.png'
$graphics.Dispose()

# Screenshot 2: settings.
$canvas = New-Background
$bitmap = $canvas[0]; $graphics = $canvas[1]
Write-Caption $graphics 'MAKE IT YOURS.' 'HOTKEY, COUNTDOWN, PREVIEW, STARTUP.' 100 24 44 19
Add-WindowImage $graphics $settingsRaw 340 110 2 40 -InsetLeft 0 -InsetTop 0 -InsetRight 14 -InsetBottom 14 -SourceRadius 26
Save-Canvas $bitmap 'Screenshot-2-Settings.png'
$graphics.Dispose()

# Screenshot 3: preview.
$canvas = New-Background
$bitmap = $canvas[0]; $graphics = $canvas[1]
Write-Caption $graphics 'COPY, SAVE, OR DISCARD.' 'THE PREVIEW CLOSES ITSELF. HOVER TO PAUSE.' 100 180 42 18
$bulletFont = New-Object System.Drawing.Font($pixelFamily, 20, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$bulletBrush = New-Object System.Drawing.SolidBrush($cream)
$coralBrush = New-Object System.Drawing.SolidBrush($coral)
$bullets = @('CLIPBOARD FIRST', 'NO HISTORY', 'NO NETWORK', 'NO ACCOUNT')
$bulletY = 330
foreach ($bullet in $bullets) {
    $graphics.FillEllipse($coralBrush, 104, ($bulletY + 8), 8, 8)
    $graphics.DrawString($bullet, $bulletFont, $bulletBrush, 130, $bulletY)
    $bulletY += 44
}
$bulletFont.Dispose()
$bulletBrush.Dispose()
$coralBrush.Dispose()
Add-WindowImage $graphics $previewRaw 1000 140 2 38 -InsetLeft 16 -InsetTop 16 -InsetRight 16 -InsetBottom 16
Save-Canvas $bitmap 'Screenshot-3-Preview.png'
$graphics.Dispose()

# Screenshot 4: about.
$canvas = New-Background
$bitmap = $canvas[0]; $graphics = $canvas[1]
Write-Caption $graphics 'THE PERSON BEHIND THE PIXELS.' 'BUILT IN PUBLIC. SHIPPED WITH CARE.' 100 24 44 19
Add-WindowImage $graphics $aboutRaw 340 110 2 40 -InsetLeft 0 -InsetTop 0 -InsetRight 14 -InsetBottom 14 -SourceRadius 26
Save-Canvas $bitmap 'Screenshot-4-About.png'
$graphics.Dispose()

$fonts.Dispose()
Write-Host 'Done.'
