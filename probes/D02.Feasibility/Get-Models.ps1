[CmdletBinding()]
param([switch] $IncludeSmall)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$directory = (New-Item -ItemType Directory -Force '.\.assets\models').FullName
$models = @(
    @{ Name = 'ggml-base.en-q5_1.bin'; Repo = 'ggerganov/whisper.cpp'; Revision = '5359861c739e955e79d9a303bcbc70fb988958b1'; Bytes = 59721011; Sha256 = '4baf70dd0d7c4247ba2b81fafd9c01005ac77c2f9ef064e00dcf195d0e2fdd2f' },
    @{ Name = 'ggml-silero-v6.2.0.bin'; Repo = 'ggml-org/whisper-vad'; Revision = '9ffd54a1e1ee413ddf265af9913beaf518d1639b'; Bytes = 885098; Sha256 = '2aa269b785eeb53a82983a20501ddf7c1d9c48e33ab63a41391ac6c9f7fb6987' }
)
if ($IncludeSmall) {
    $models += @{ Name = 'ggml-small.en-q5_1.bin'; Repo = 'ggerganov/whisper.cpp'; Revision = '5359861c739e955e79d9a303bcbc70fb988958b1'; Bytes = 190098681; Sha256 = 'bfdff4894dcb76bbf647d56263ea2a96645423f1669176f4844a1bf8e478ad30' }
}
$onnx = Join-Path $directory 'silero_vad.onnx'
if (!(Test-Path $onnx)) {
    Invoke-WebRequest 'https://raw.githubusercontent.com/snakers4/silero-vad/v6.2.3/src/silero_vad/data/silero_vad.onnx' -OutFile $onnx
}
if ((Get-Item $onnx).Length -ne 2327524 -or (Get-FileHash $onnx -Algorithm SHA256).Hash -ne '1a153a22f4509e292a94e67d6f9b85e8deb25b4988682b7e174c65279d8788e3') {
    throw 'Official ONNX fallback integrity failed. Native loading is forbidden.'
}
Write-Output 'VERIFIED official silero_vad.onnx v6.2.3'
foreach ($model in $models) {
    $path = Join-Path $directory $model.Name
    if (!(Test-Path $path)) {
        Invoke-WebRequest "https://huggingface.co/$($model.Repo)/resolve/$($model.Revision)/$($model.Name)" -OutFile $path
    }
    if ((Get-Item $path).Length -ne $model.Bytes -or (Get-FileHash $path -Algorithm SHA256).Hash -ne $model.Sha256) {
        throw "Model integrity failed: $($model.Name). Native loading is forbidden."
    }
    Write-Output "VERIFIED $($model.Name) bytes=$($model.Bytes) sha256=$($model.Sha256)"
}
