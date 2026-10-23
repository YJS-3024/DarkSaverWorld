param(
    [string]$GameRoot = (Join-Path $PSScriptRoot '..\extracted\Saver_Files\Saver'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\unpacked\mxf'),
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\catalogs\mxf_images.csv')
)

$width = 288; $height = 288; $expectedSize = $width * $height
$rows = [Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

foreach ($file in Get-ChildItem -LiteralPath $GameRoot -Filter '*.mxf' -File -Recurse) {
    $pixels = [IO.File]::ReadAllBytes($file.FullName)
    if ($pixels.Length -ne $expectedSize) { throw "Unexpected MXF size: $($file.FullName)" }
    $name = '{0}_{1}.bmp' -f $file.Directory.Name, $file.BaseName
    $path = Join-Path $OutputDirectory $name
    $stream = [IO.File]::Create($path); $writer = [IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([byte]0x42); $writer.Write([byte]0x4D)
        $writer.Write([int](1078 + $pixels.Length)); $writer.Write([int]0); $writer.Write([int]1078)
        $writer.Write([int]40); $writer.Write([int]$width); $writer.Write([int](-$height))
        $writer.Write([int16]1); $writer.Write([int16]8); $writer.Write([int]0)
        $writer.Write([int]$pixels.Length); $writer.Write([int]0); $writer.Write([int]0)
        $writer.Write([int]256); $writer.Write([int]256)
        for ($i = 0; $i -lt 256; $i++) {
            $writer.Write([byte]$i); $writer.Write([byte]$i); $writer.Write([byte]$i); $writer.Write([byte]0)
        }
        $writer.Write($pixels)
    } finally { $writer.Dispose(); $stream.Dispose() }
    $rows.Add([pscustomobject]@{
        source = "$($file.Directory.Name)/$($file.Name)"; width = $width; height = $height
        format = '8-bit grayscale/index plane'; output = $name
    })
}
$rows | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath $ManifestPath
Write-Output ("images={0}" -f $rows.Count)
