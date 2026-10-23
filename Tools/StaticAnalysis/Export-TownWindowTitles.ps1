# Decode original Wlib layouts as a diagnostic sheet, never load the client executable.
param([string]$Output = 'Tools/CombatRulesChecks/town-window-titles.png')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sourceRoot = Join-Path (Get-Location) '분석/Saver_1.2'
$header = [IO.File]::ReadAllBytes((Join-Path $sourceRoot 'unpacked/legacy_libraries/Wlib_waitlib_lce.header.bin'))
$files = @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'extracted/Saver_Files/Saver/Wlib') -Filter '*.lsc' | Where-Object { $_.BaseName -match '^scene\d+$' } | Sort-Object { [int]($_.BaseName -replace '\D','') })
function Read-Int([byte[]]$bytes, [int]$offset) { [BitConverter]::ToInt32($bytes,$offset) }
$atlas = [Drawing.Bitmap]::new((Join-Path $sourceRoot 'unpacked/legacy_libraries/Wlib_waitlib_lce.bmp'))
$sheet = [Drawing.Bitmap]::new(1300, [int]([Math]::Ceiling($files.Count/2.0)*66))
$graphics = [Drawing.Graphics]::FromImage($sheet)
$font = [Drawing.Font]::new('Consolas',10)
$attributes = [Drawing.Imaging.ImageAttributes]::new()
try {
    $graphics.Clear([Drawing.Color]::DimGray)
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $attributes.SetColorKey([Drawing.Color]::Black,[Drawing.Color]::Black)
    for($index=0; $index -lt $files.Count; $index++) {
        $scene = [IO.File]::ReadAllBytes($files[$index].FullName)
        $width=Read-Int $scene 0; $height=Read-Int $scene 4
        $ox=($index%2)*650; $oy=[int][Math]::Floor($index/2.0)*66
        $state=$graphics.Save()
        $graphics.SetClip([Drawing.Rectangle]::new($ox,$oy,$width,[Math]::Min(42,$height)))
        $cursor=28
        for($group=0; $group -lt (Read-Int $scene 24); $group++) {
            $id=Read-Int $scene $cursor; $count=Read-Int $scene ($cursor+4); $cursor+=8
            if($id -lt 1 -or $id -gt 278 -or $count -lt 0 -or $count -gt 1000) { throw 'Invalid layout record' }
            $record=4+($id-1)*20
            $sx=Read-Int $header ($record+4); $sy=Read-Int $header ($record+8)
            $w=Read-Int $header ($record+12); $h=Read-Int $header ($record+16)
            for($p=0;$p -lt $count;$p++) {
                $left=Read-Int $scene $cursor; $top=Read-Int $scene ($cursor+4)
                $nx=Read-Int $scene ($cursor+8); $ny=Read-Int $scene ($cursor+12); $cursor+=16
                if($nx -lt 0 -or $ny -lt 0 -or ([long]$nx*$ny) -gt 10000) { throw 'Invalid repetition' }
                for($ry=0;$ry -lt $ny;$ry++) {
                    $dy=$top+$ry*$h
                    if($dy -ge 42 -or $dy+$h -le 0) { continue }
                    for($rx=0;$rx -lt $nx;$rx++) {
                        $dest=[Drawing.Rectangle]::new(($ox+$left+$rx*$w),($oy+$dy),$w,$h)
                        $graphics.DrawImage($atlas,$dest,$sx,$sy,$w,$h,[Drawing.GraphicsUnit]::Pixel,$attributes)
                    }
                }
            }
        }
        $graphics.Restore($state)
        $graphics.DrawString(('{0} {1}x{2}' -f $files[$index].Name,$width,$height),$font,[Drawing.Brushes]::White,$ox,($oy+44))
    }
    $target=[IO.Path]::GetFullPath((Join-Path (Get-Location) $Output))
    if(-not $target.StartsWith((Join-Path (Get-Location) 'Tools/CombatRulesChecks/'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Output must remain under Tools/CombatRulesChecks' }
    $sheet.Save($target,[Drawing.Imaging.ImageFormat]::Png)
    Write-Output ('Exported {0} window titles: {1}' -f $files.Count,$Output)
} finally { $attributes.Dispose(); $font.Dispose(); $graphics.Dispose(); $sheet.Dispose(); $atlas.Dispose() }
