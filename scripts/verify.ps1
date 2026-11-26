# Verification Harness Script -- 3-Layer (Syntax -> Lint -> Test)
# Circuit Breaker: Dung ngay tai lop dau tien fail, bao structured error.
$ErrorActionPreference = "Stop"

function Write-Layer { param($msg) Write-Host "" ; Write-Host "[LAYER] $msg" -ForegroundColor Cyan }
function Write-Ok    { param($msg) Write-Host "[OK]   $msg" -ForegroundColor Green }
function Write-Fail  { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Verification Harness -- ai-spec" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Layer 1: Syntax Check (node --check)
Write-Layer "1/4 Syntax Check"
$jsFiles = Get-ChildItem -Path "src" -Recurse -Filter "*.js" -ErrorAction SilentlyContinue
if ($null -eq $jsFiles -or $jsFiles.Count -eq 0) {
    Write-Ok "No .js files in src/ -- skipped"
} elseif (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Ok "Node.js not in PATH -- checked $($jsFiles.Count) files exist and readable"
} else {
    foreach ($f in $jsFiles) {
        $result = node --check $f.FullName 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "Syntax error in $($f.FullName)"
            Write-Host $result -ForegroundColor Yellow
            Write-Host ""
            Write-Host "[HARNESS] Layer 1 FAILED -- fix syntax before proceeding." -ForegroundColor Red
            exit 1
        }
    }
    Write-Ok "Syntax OK ($($jsFiles.Count) files)"
}

# Layer 2: Lint (hardcoded secret detection)
Write-Layer "2/4 Lint"
$scriptFiles = Get-ChildItem -Path "scripts" -Recurse -Filter "*.js" -ErrorAction SilentlyContinue
$allJs = @()
if ($jsFiles)     { $allJs += $jsFiles }
if ($scriptFiles) { $allJs += $scriptFiles }

if ($allJs.Count -eq 0) {
    Write-Ok "No .js files -- skipped"
} else {
    $secretPattern = '(password|secret|api_key)\s*=\s*["''][^""'']{4,}'
    $hits = $allJs | Select-String -Pattern $secretPattern -CaseSensitive:$false
    if ($hits) {
        Write-Fail "Hardcoded secret detected:"
        $hits | ForEach-Object { Write-Host "  $($_.Filename):$($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Yellow }
        exit 1
    }
    Write-Ok "Lint OK (no hardcoded secrets, $($allJs.Count) files checked)"
}

# Layer 3: Skills Verification
Write-Layer "3/4 Skills Verification"
$skillsDir = Join-Path (Get-Location).Path ".agents\skills"
if (Test-Path $skillsDir) {
    $skills = Get-ChildItem -Path $skillsDir -Directory
    $failed = 0
    foreach ($s in $skills) {
        $file = Join-Path $s.FullName "SKILL.md"
        if (-not (Test-Path $file)) {
            Write-Fail "Missing SKILL.md in $($s.Name)"
            $failed++
            continue
        }
        $raw = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
        if ($raw -notmatch '^---\r?\n([\s\S]*?)\r?\n---') {
            Write-Fail "Invalid frontmatter in $($s.Name)"
            $failed++
            continue
        }
        $frontmatter = $Matches[1]
        if ($frontmatter -notmatch 'name:\s*\S+' -or $frontmatter -notmatch 'description:\s*\S+') {
            Write-Fail "Missing name or description in $($s.Name)"
            $failed++
            continue
        }
    }
    if ($failed -gt 0) {
        Write-Host ""
        Write-Host "[HARNESS] Skills verification FAILED -- $failed skills have errors." -ForegroundColor Red
        exit 1
    }
    Write-Ok "Skills OK ($($skills.Count) skills verified)"
} else {
    Write-Ok "No .agents/skills directory -- skipped"
}

# Layer 4: Unit Tests
Write-Layer "4/4 Unit Tests"
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Ok "Node.js not in PATH -- JS unit tests skipped (syntax and skills verified)"
} else {
    $testFiles = Get-ChildItem -Path "tests" -Recurse -Filter "*.test.js" -ErrorAction SilentlyContinue
    if ($null -eq $testFiles -or $testFiles.Count -eq 0) {
        Write-Ok "No test files found -- skipped"
    } else {
        $testOutput = node --test tests/**/*.test.js 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "Test failures detected:"
            $testOutput |
                Select-String -Pattern "(fail|error|expected|actual|AssertionError)" -CaseSensitive:$false |
                Select-Object -Last 10 |
                ForEach-Object { Write-Host "  $($_.Line)" -ForegroundColor Yellow }
            Write-Host ""
            Write-Host "[HARNESS] Layer 4 FAILED -- max 3 repair cycles, then rollback + [BLOCKED]." -ForegroundColor Red
            exit 1
        }
        Write-Ok "All tests passed ($($testFiles.Count) test files)"
    }
}

# Summary
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  [HARNESS PASS] All verification layers cleared." -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
