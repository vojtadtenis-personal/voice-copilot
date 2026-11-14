$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$directory = (New-Item -ItemType Directory -Force '.\.assets\audio').FullName
$fixtures = @(
    @{ Name = 'ordinary'; Text = 'Create a new session. Open the file and stop the task.' },
    @{ Name = 'domain'; Text = 'Use NAudio with Whisper and Copilot. Keep the project path unchanged.' }
)
$voice = New-Object -ComObject SAPI.SpVoice
$english = @($voice.GetVoices() | Where-Object { $_.GetAttribute('Language') -match '(^|;)409(;|$)' })[0]
if (!$english) { throw 'English SAPI voice unavailable; no substitute recording is permitted.' }
$voice.Voice = $english
$voice.Rate = -1
foreach ($fixture in $fixtures) {
    $path = Join-Path $directory "$($fixture.Name).wav"
    $stream = New-Object -ComObject SAPI.SpFileStream
    try {
        $stream.Format.Type = 18 # SAPI SAFT16kHz16BitMono.
        $stream.Open($path, 3)
        $voice.AudioOutputStream = $stream
        [void] $voice.Speak($fixture.Text)
    }
    finally {
        $stream.Close()
        [void] [Runtime.InteropServices.Marshal]::FinalReleaseComObject($stream)
    }
    Write-Output "SYNTHETIC $($fixture.Name) sha256=$((Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant())"
}
Write-Output "SAPI voice=$($english.GetDescription()); rate=-1; file output only; no audible playback or microphone"
[void] [Runtime.InteropServices.Marshal]::FinalReleaseComObject($voice)
