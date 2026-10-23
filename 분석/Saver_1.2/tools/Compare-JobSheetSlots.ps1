param(
    [Parameter(Mandatory=$true)][string]$SheetRoot,
    [Parameter(Mandatory=$true)][string]$SourceRoot,
    [Parameter(Mandatory=$true)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Security.Cryptography;
public static class JobSlotPixels {
    public static string Hash(Bitmap bitmap, int left, int top) {
        var rgb = new byte[96 * 128 * 3];
        int index = 0;
        for (int y = 0; y < 128; y++)
        for (int x = 0; x < 96; x++) {
            var pixel = bitmap.GetPixel(left + x, top + y);
            rgb[index++] = pixel.A == 0 ? (byte)0 : pixel.R;
            rgb[index++] = pixel.A == 0 ? (byte)0 : pixel.G;
            rgb[index++] = pixel.A == 0 ? (byte)0 : pixel.B;
        }
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(rgb)).Replace("-", "");
    }
}
'@
$sourceDirectory = (Resolve-Path -LiteralPath $SourceRoot).Path
$byHash = @{}
$candidateCount = 0
foreach ($file in Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.bmp' -File -Recurse) {
    $bitmap = [System.Drawing.Bitmap]::new($file.FullName)
    try {
        if ($bitmap.Width -ne 96 -or $bitmap.Height -lt 128) { continue }
        $candidateCount++
        $hash = [JobSlotPixels]::Hash($bitmap, 0, 0)
        if (!$byHash.ContainsKey($hash)) { $byHash[$hash] = @() }
        $relative = $file.FullName.Substring($sourceDirectory.Length).TrimStart('\').Replace('\', '/')
        $byHash[$hash] += $relative
    } finally { $bitmap.Dispose() }
}
$rows = foreach ($sheet in Get-ChildItem -LiteralPath $SheetRoot -Filter '*.png' -File | Sort-Object Name) {
    $bitmap = [System.Drawing.Bitmap]::new($sheet.FullName)
    try {
        if ($bitmap.Width -ne 384 -or $bitmap.Height -ne 256) {
            throw "Unexpected sheet dimensions: $($sheet.Name)"
        }
        for ($slot = 0; $slot -lt 8; $slot++) {
            $x = ($slot % 4) * 96
            $y = [int][Math]::Floor($slot / 4) * 128
            $hash = [JobSlotPixels]::Hash($bitmap, $x, $y)
            $matches = @($byHash[$hash] | Where-Object { $_ })
            [pscustomobject]@{
                sheet = $sheet.Name; slot = $slot; x = $x; y = $y
                width = 96; height = 128; normalizedRgbSha256 = $hash
                sourceMatchCount = $matches.Count; sourceMatches = ($matches -join ';')
            }
        }
    } finally { $bitmap.Dispose() }
}
$rows | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding UTF8
Write-Output "Compared $candidateCount source BMPs and $($rows.Count) slots; matched $(@($rows | Where-Object { $_.sourceMatchCount -gt 0 }).Count)."
$rows | Where-Object { $_.slot -eq 7 } | Select-Object sheet, sourceMatches | Format-Table -AutoSize
