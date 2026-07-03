param(
    [Parameter(Mandatory = $false, Position = 0)]
    [string]$Target = "Default",
    
    [Parameter(ValueFromRemainingArguments = $true)]
    $RemainingArgs
)

dotnet run --project ./build/Build.csproj --no-build --no-restore --target=$Target $RemainingArgs