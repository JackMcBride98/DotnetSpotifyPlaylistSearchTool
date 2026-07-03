param(
    [string]$Target = ""
)

if ([string]::IsNullOrWhiteSpace($Target)) {
    # No argument passed: Run the default task list directly
    dotnet run --project ./build/Build.csproj --no-restore
} else {
    # Argument passed: Forward it explicitly
    dotnet run --project ./build/Build.csproj --no-restore -- --target=$Target
}