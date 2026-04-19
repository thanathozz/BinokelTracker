# BinokelTracker

## Build Verification
- Always run a full build (`dotnet build`) after multi-file changes before declaring task complete
- For MAUI projects, verify both CLI build AND mention if IDE reload/cache invalidation may be needed
- After XAML/Razor changes in MAUI Blazor Hybrid, suggest a rebuild (not just hot reload) since CSS classes sometimes don't apply
