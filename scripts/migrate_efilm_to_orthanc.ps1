<#
.SYNOPSIS
    Di tru du lieu DICOM tu eFilm Workstation cu sang Orthanc PACS (DCMTK storescu).
#>
param(
    [string]$EfilmDicomRoot = "D:\eFilmData\DICOM",
    [string]$OrthancHost = "127.0.0.1",
    [int]$OrthancDicomPort = 4242,
    [string]$CalledAet = "SCM_PACS"
)

if (-not (Test-Path $EfilmDicomRoot)) {
    Write-Error "Khong tim thay thu muc DICOM eFilm: $EfilmDicomRoot"
    exit 1
}

$dcmFiles = Get-ChildItem -Path $EfilmDicomRoot -Recurse -File -Include *.dcm,*.DCM
Write-Host "[SCM-PACS] Tim thay $($dcmFiles.Count) file DICOM. Bat dau day sang Orthanc ($OrthancHost`:$OrthancDicomPort AET=$CalledAet)..."

foreach ($f in $dcmFiles) {
    & storescu -aec $CalledAet $OrthancHost $OrthancDicomPort $f.FullName
}

Write-Host "[SCM-PACS] Hoan tat di tru kho anh eFilm sang Orthanc!"
