# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build KryptoDrive.sln

# Build for Android
dotnet build src/KryptoDrive/KryptoDrive.csproj -f net8.0-android -p:AndroidSdkDirectory="C:/Program Files (x86)/Android/android-sdk"

# Deploy to Android emulator (emulator must be running)
dotnet build src/KryptoDrive/KryptoDrive.csproj -f net8.0-android -t:Install -p:AndroidSdkDirectory="C:/Program Files (x86)/Android/android-sdk"

# Launch on emulator
"C:/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" shell am start -n club.codedev.kryptodrive/crc64e2fbc741db1c3677.MainActivity

# Run tests
dotnet test tests/KryptoDrive.Tests/
```

## Architecture

Multi-project .NET 8 MAUI app using **MVVM + Rich Domain** with layered architecture.

```
KryptoDrive.sln
├── src/KryptoDrive.Domain/              ← Rich Domain Models (MediaFile, SecureFolder, MediaCatalog)
├── src/KryptoDrive.DTO/                 ← DTOs with "Info" suffix (MediaFileInfo, FileItemInfo, etc.)
├── src/KryptoDrive.Infra.Interfaces/    ← Interfaces (ICryptoService, IVaultRepository, IVaultAppService)
├── src/KryptoDrive.Infra/               ← Repositories + AppServices + AutoMapper + CryptoService
├── src/KryptoDrive/                     ← MAUI App (Pages, ViewModels, Converters)
└── tests/KryptoDrive.Tests/             ← xUnit tests (Domain, AppServices, Mappers)
```

**Data flow:** `Page (View) → ViewModel → IVaultAppService → IVaultRepository → Encrypted files on disk`

### Project Dependencies

- **Domain**: No dependencies (pure C# models with behavior)
- **DTO**: No dependencies (plain data classes)
- **Infra.Interfaces**: References Domain + DTO
- **Infra**: References Domain + DTO + Infra.Interfaces; uses AutoMapper 12.0.1
- **KryptoDrive (MAUI)**: References all src projects; uses CommunityToolkit.Mvvm, AutoMapper DI
- **Tests**: References Domain + DTO + Infra.Interfaces + Infra; uses xUnit + Moq

### Navigation (Shell)

Routes are defined in `src/KryptoDrive/AppShell.xaml` and `AppShell.xaml.cs`:
- `//login` → `LoginPage` (absolute route, replaces stack)
- `//explorer` → `FileExplorerPage` (absolute route, replaces stack)
- `viewer` → `MediaViewerPage` (registered via `Routing.RegisterRoute`, push route with `[QueryProperty]` params)

### DI Registration (`MauiProgram.cs`)

- **Singletons:** `ICryptoService` (path-injected), `IVaultRepository` (path-injected), `IVaultAppService`, `GoogleDriveService`
- **Transients:** All ViewModels and Pages
- **AutoMapper:** Registered via `AddAutoMapper(typeof(VaultMapperProfile))`
- Pages receive their ViewModel via constructor injection and set `BindingContext`

### Storage — No Database

All data is file-based in `FileSystem.AppDataDirectory/vault/`:
- `.salt` — 16-byte PBKDF2 salt
- `.verify` — encrypted verification token (password check)
- `catalog.enc` — AES-256-GCM encrypted JSON (backward-compatible via `CatalogPersistenceDto` in VaultRepository)
- `files/<guid>.enc` — individually encrypted media blobs

**Encryption:** AES-256-GCM with PBKDF2-SHA256 (100k iterations). Each encrypted blob: `[nonce 12B][tag 16B][ciphertext]`.

### Security Lifecycle

`App.xaml.cs` calls `ClearPassword()` on `OnSleep()` (zeroes master key in memory) and redirects to `//login` on `OnResume()` if no key is loaded.

## Key Conventions

- **Domain models** use private setters + factory methods (`Create`, `Reconstitute`) for encapsulation
- **DTOs** use "Info" suffix (e.g., `MediaFileInfo`, `FileItemInfo`)
- **Interfaces** live in `Infra.Interfaces` project, not alongside implementations
- ViewModels inherit `BaseViewModel : ObservableObject` and use `[ObservableProperty]` and `[RelayCommand]` source generators
- XAML pages use compiled bindings (`x:DataType`)
- Value converters are in `Converters/ValueConverters.cs` and registered as global resources in `App.xaml`
- Google auth tokens stored via MAUI `SecureStorage`; token expiry via `Preferences`
- Android package ID: `club.codedev.kryptodrive` (set in `AndroidManifest.xml`)

## Known Issues

- `GoogleDriveService` is partially implemented but not integrated into the main Shell navigation flow
