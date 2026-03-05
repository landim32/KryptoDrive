---
name: maui-architecture
description: Guides the implementation of a new entity following the MVVM + layered architecture for .NET MAUI apps. Covers all layers from SQLite Model to Page, including Repository pattern, Rich Domain entities, Domain Services, AppServices, DTOs with AutoMapper, ViewModel with CommunityToolkit.Mvvm, XAML Page, AppDatabase registration, and DI setup. Use when creating or modifying entities, adding new tables, or scaffolding CRUD features in any MAUI project.
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
user-invocable: true
---

# .NET MAUI Layered Architecture — Entity Implementation Guide

You are an expert assistant that helps developers create or modify entities following the MVVM + layered architecture pattern for .NET MAUI mobile apps, applying **Rich Domain** principles.

## Input

The user will describe the entity to create or modify: `$ARGUMENTS`

## Before You Start

1. Run `dotnet sln list` and `ls` to discover project names and folder layout.
2. The placeholder `{App}` stands for the actual root namespace. Replace it everywhere.
3. **Read at least one existing entity end-to-end** (Model → DTO → Interface → Repository → Mapper → ViewModel → Page) to match style.
4. Check existing UI strings to detect the app language and match it.

---

## Project Structure & Dependencies

```
{App}.sln
├── {App}.Domain/              ← Models (Rich Domain) + Services + Helpers (refs Infra.Interfaces)
├── {App}.DTO/                 ← DTOs with "Info" suffix (net8.0, no deps)
├── {App}.Infra.Interfaces/    ← Interfaces for Repos, Services & AppServices (refs Domain + DTO)
├── {App}.Infra/               ← AppServices + Repos + Context + Mappers (refs Domain + DTO + Infra.Interfaces)
├── {App}/                     ← MAUI: Pages, ViewModels, Converters (refs all projects)
└── {App}.Tests/               ← Unit tests (refs Domain + DTO + Infra.Interfaces + Infra)
```

```
DTO (no deps)    Infra.Interfaces (refs Domain + DTO)
      ↑                ↑
      ├── Domain ──────┘ (refs Infra.Interfaces)
      ├── Infra ─────────  (refs Domain + DTO + Infra.Interfaces)
      └── MAUI ──────────  (refs all) → Tests
```

All projects use `<RootNamespace>{App}</RootNamespace>` to share namespaces.

---

## Architecture Principles

### Rich Domain Model

Entities contain **business rules, validation, and behavior** — NOT anemic data bags:
- Entities encapsulate invariants (validation, state transitions, computed properties)
- Mutable with controlled state changes via methods
- SQLite attributes coexist with domain logic

### DTOs (Data Transfer Objects)

- Live in `{App}.DTO` project — pure data classes, **no logic, no attributes**
- Named with **"Info" suffix**: `{Entity}Info`
- Used by ViewModels and service interfaces as input/output contracts
- **Never** expose domain Models to the UI layer — always map to/from DTOs

### Mapping Flow

```
DB (SQLite) → Repository → Model (Domain) → AutoMapper → {Entity}Info (DTO) → ViewModel → Page
Page → ViewModel → {Entity}Info (DTO) → AutoMapper → Model (Domain) → Repository → DB
```

### Layer Responsibilities

| Layer | Contains | Responsibility |
|-------|----------|----------------|
| **Domain** | Rich Entities + Services | Business rules. Entities own invariants. Services depend ONLY on Infra.Interfaces. |
| **DTO** | Info classes | Pure data transfer. No logic, no deps. Suffix "Info". |
| **Infra.Interfaces** | Interfaces | Contracts for Repos, Services, AppServices. |
| **Infra** | AppServices + Repos + Context + Mappers | Infrastructure + AutoMapper profiles. |
| **MAUI** | Pages + ViewModels + Converters | UI. ViewModels work with DTOs (Info), not Models. |

**Packages:** `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`, `CommunityToolkit.Mvvm`, `AutoMapper.Extensions.Microsoft.DependencyInjection`

---

## Step-by-Step Implementation

### Step 1: Rich Domain Entity — `{App}.Domain/Models/{Entity}.cs`

```csharp
using SQLite;
namespace {App}.Models;

public class {Entity}
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // --- Domain Logic ---
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) return "Name is required.";
        if (Name.Length > 200) return "Name cannot exceed 200 characters.";
        return null;
    }

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public static {Entity} Create(string name)
    {
        var entity = new {Entity} { Name = name };
        var error = entity.Validate();
        if (error != null) throw new InvalidOperationException(error);
        return entity;
    }
}
```

**Conventions:** `Validate()` returns error or null · `Update()` controls state + sets UpdatedAt · `Create()` factory enforces validation · `[Indexed]` on FKs · No navigation properties

### Step 2: DTO — `{App}.DTO/DTOs/{Entity}Info.cs`

```csharp
namespace {App}.DTOs;

public class {Entity}Info
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**DTO conventions:**
- Suffix **"Info"** always: `NoteInfo`, `CategoryInfo`, `CommentInfo`
- No SQLite attributes, no validation, no methods — pure data
- Mirror the properties the UI/consumers need (can omit internal-only fields)
- No dependency on any other project

### Step 3: Repository Interface — `{App}.Infra.Interfaces/Services/I{Entity}Repository.cs`

```csharp
using {App}.Models;
namespace {App}.Services;

public interface I{Entity}Repository
{
    Task<List<{Entity}>> GetAllAsync();
    Task<{Entity}?> GetByIdAsync(int id);
    Task<int> SaveAsync({Entity} entity);   // upsert
    Task<int> DeleteAsync(int id);
}
```

Repositories work with **Models** (Domain). The mapping to DTOs happens at the ViewModel/Service level.

### Step 4: Domain Service (when needed) — `{App}.Domain/Services/{Entity}Service.cs`

For business rules spanning multiple entities. Depends ONLY on interfaces — never concrete classes. Does NOT call external APIs.

```csharp
using {App}.Models;
namespace {App}.Services;

public class {Entity}Service
{
    private readonly I{Entity}Repository _{entity}Repository;
    public {Entity}Service(I{Entity}Repository repo) => _{entity}Repository = repo;

    public async Task<string?> ValidateUniqueNameAsync(string name, int? excludeId = null)
    {
        var all = await _{entity}Repository.GetAllAsync();
        var dup = all.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && e.Id != (excludeId ?? 0));
        return dup ? "Name already exists." : null;
    }
}
```

### Step 5: AppService (when needed) — Interface + Implementation

**Interface** in `{App}.Infra.Interfaces/Services/I{Entity}AppService.cs`:
```csharp
public interface I{Entity}AppService { Task<string> ProcessAsync({Entity} entity); }
```

**Implementation** in `{App}.Infra/AppServices/{Entity}AppService.cs` — infrastructure only (external APIs, HTTP). NO business rules.

### Step 6: AutoMapper Profile — `{App}.Infra/Mappers/{Entity}Profile.cs`

```csharp
using AutoMapper;
using {App}.DTOs;
using {App}.Models;

namespace {App}.Mappers;

public class {Entity}Profile : Profile
{
    public {Entity}Profile()
    {
        CreateMap<{Entity}, {Entity}Info>();          // Model → DTO (read)
        CreateMap<{Entity}Info, {Entity}>()           // DTO → Model (write)
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
```

**Mapper conventions:**
- One Profile per entity in `{App}.Infra/Mappers/`
- Model → Info: direct map (all readable props)
- Info → Model: **ignore** managed fields (`CreatedAt`, `UpdatedAt`) — the entity's `Update()`/`Create()` methods control those
- AutoMapper auto-discovered via `AddAutoMapper(assembly)` in DI

### Step 7: Repository Implementation — `{App}.Infra/Services/{Entity}Repository.cs`

```csharp
using {App}.Data;
using {App}.Models;
namespace {App}.Services;

public class {Entity}Repository : I{Entity}Repository
{
    private readonly AppDatabase _database;
    public {Entity}Repository(AppDatabase database) => _database = database;

    public async Task<List<{Entity}>> GetAllAsync() =>
        await _database.Connection.Table<{Entity}>().OrderBy(e => e.Name).ToListAsync();

    public async Task<{Entity}?> GetByIdAsync(int id) =>
        await _database.Connection.Table<{Entity}>().Where(e => e.Id == id).FirstOrDefaultAsync();

    public async Task<int> SaveAsync({Entity} entity) =>
        entity.Id != 0
            ? await _database.Connection.UpdateAsync(entity)
            : await _database.Connection.InsertAsync(entity);

    public async Task<int> DeleteAsync(int id) =>
        await _database.Connection.DeleteAsync<{Entity}>(id);
}
```

**Conventions:** Upsert via `Id != 0` · Repo does NOT set UpdatedAt (entity handles it) · No try/catch · No business logic

### Step 8: Register Table — `{App}.Infra/Context/AppDatabase.cs`

Add to `InitializeAsync()`: `await _database.CreateTableAsync<{Entity}>();`

### Step 9: List ViewModel — `{App}/ViewModels/{Entity}ListViewModel.cs`

```csharp
public partial class {Entity}ListViewModel : ObservableObject
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;
    public {Entity}ListViewModel(I{Entity}Repository repo, IMapper mapper)
    { _{entity}Repository = repo; _mapper = mapper; }

    [ObservableProperty] private ObservableCollection<{Entity}Info> _items = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        IsLoading = true;
        try
        {
            var models = await _{entity}Repository.GetAllAsync();
            Items = new(_mapper.Map<List<{Entity}Info>>(models));
            IsEmpty = Items.Count == 0;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync({Entity}Info item)
    {
        if (!await Shell.Current.DisplayAlert("Delete", $"Delete \"{item.Name}\"?", "Yes", "No")) return;
        await _{entity}Repository.DeleteAsync(item.Id); Items.Remove(item); IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task GoToDetailAsync({Entity}Info item) =>
        await Shell.Current.GoToAsync("{Entity}DetailPage", new Dictionary<string, object> { { "{Entity}Info", item } });
}
```

### Step 10: Detail ViewModel — `{App}/ViewModels/{Entity}DetailViewModel.cs`

```csharp
public partial class {Entity}DetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;
    public {Entity}DetailViewModel(I{Entity}Repository repo, IMapper mapper)
    { _{entity}Repository = repo; _mapper = mapper; }

    [ObservableProperty] private int _{entity}Id;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isNewItem = true;
    [ObservableProperty] private string _pageTitle = "New {Entity}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("{Entity}Info", out var obj) && obj is {Entity}Info info)
        { {Entity}Id = info.Id; Name = info.Name; IsNewItem = false; PageTitle = "Edit {Entity}"; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var entity = IsNewItem ? {Entity}.Create(Name) : (await _{entity}Repository.GetByIdAsync({Entity}Id))!;
            if (!IsNewItem) entity.Update(Name);
            var error = entity.Validate();
            if (error != null) { await Shell.Current.DisplayAlert("Error", error, "OK"); return; }
            await _{entity}Repository.SaveAsync(entity);
            await Shell.Current.GoToAsync("..");
        }
        catch (InvalidOperationException ex) { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); }
        finally { IsSaving = false; }
    }

    [RelayCommand] private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
```

**ViewModel conventions:** ViewModels bind to **`{Entity}Info`** (DTO), not Models · `IMapper` injected for conversions · Navigation passes DTOs · Save/Update still goes through domain entity methods (Create/Update/Validate) for business rules · `[ObservableProperty]` on `_camelCase` · `[RelayCommand]` on `{Method}Async`

### Step 11: List Page XAML — `{App}/Pages/{Entity}ListPage.xaml`

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:{App}.ViewModels"
             xmlns:dto="clr-namespace:{App}.DTOs;assembly={App}.DTO"
             x:Class="{App}.Pages.{Entity}ListPage"
             x:DataType="vm:{Entity}ListViewModel"
             Title="{Entities}">
    <Grid RowDefinitions="*,Auto" Padding="16">
        <ActivityIndicator Grid.Row="0" IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}"
                           HorizontalOptions="Center" VerticalOptions="Center" />
        <Label Grid.Row="0" Text="No items yet" IsVisible="{Binding IsEmpty}"
               FontSize="18" HorizontalOptions="Center" VerticalOptions="Center" />
        <CollectionView Grid.Row="0" ItemsSource="{Binding Items}" SelectionMode="None">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="dto:{Entity}Info">
                    <SwipeView>
                        <SwipeView.RightItems><SwipeItems>
                            <SwipeItem Text="Delete" BackgroundColor="#E53935"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:{Entity}ListViewModel}}, Path=DeleteCommand}"
                                CommandParameter="{Binding}" />
                        </SwipeItems></SwipeView.RightItems>
                        <Frame Margin="0,4" Padding="16" CornerRadius="12" BorderColor="Transparent">
                            <Frame.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:{Entity}ListViewModel}}, Path=GoToDetailCommand}"
                                    CommandParameter="{Binding}" />
                            </Frame.GestureRecognizers>
                            <Label Text="{Binding Name}" FontSize="16" FontAttributes="Bold" />
                        </Frame>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
        <Button Grid.Row="1" Text="+ New" Command="{Binding GoToDetailCommand}"
                FontSize="16" HeightRequest="56" CornerRadius="28" Margin="0,12,0,0" />
    </Grid>
</ContentPage>
```

**XAML assembly refs:** DTOs need `assembly={App}.DTO` · ViewModels in MAUI do NOT need assembly qualifier.

### Step 12: Page Code-Behind — `{App}/Pages/{Entity}ListPage.xaml.cs`

```csharp
public partial class {Entity}ListPage : ContentPage
{
    private readonly {Entity}ListViewModel _viewModel;
    public {Entity}ListPage({Entity}ListViewModel viewModel)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; }

    protected override async void OnAppearing()
    { base.OnAppearing(); await _viewModel.LoadItemsCommand.ExecuteAsync(null); }
}
```

### Step 13: DI Registration — `{App}/MauiProgram.cs`

```csharp
// AutoMapper — scans Infra assembly for all Profiles
builder.Services.AddAutoMapper(typeof(AppDatabase).Assembly);

builder.Services.AddSingleton<{Entity}Service>();                              // Domain Service (if needed)
builder.Services.AddSingleton<I{Entity}AppService, {Entity}AppService>();      // AppService (if needed)
builder.Services.AddSingleton<I{Entity}Repository, {Entity}Repository>();      // Repository
builder.Services.AddTransient<{Entity}ListViewModel>();                        // ViewModels
builder.Services.AddTransient<{Entity}DetailViewModel>();
builder.Services.AddTransient<{Entity}ListPage>();                             // Pages
builder.Services.AddTransient<{Entity}DetailPage>();
```

**Singleton** for repos/services/appservices · **Transient** for ViewModels/Pages · AutoMapper registered **once**, scans assembly for all Profiles.

### Step 14: Shell Navigation — `AppShell`

In `.xaml`: `<ShellContent Title="{Entities}" ContentTemplate="{DataTemplate pages:{Entity}ListPage}" Route="{Entity}ListPage" />`

In `.xaml.cs`: `Routing.RegisterRoute("{Entity}DetailPage", typeof({Entity}DetailPage));`

### Step 15: Unit Tests — `{App}.Tests/Services/{Entity}RepositoryTests.cs`

```csharp
public class {Entity}RepositoryTests : IAsyncLifetime
{
    private AppDatabase _database = null!;
    private {Entity}Repository _repository = null!;
    private string _dbPath = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db3");
        _database = new AppDatabase(_dbPath); await _database.InitializeAsync();
        _repository = new {Entity}Repository(_database);
    }
    public Task DisposeAsync() { if (File.Exists(_dbPath)) File.Delete(_dbPath); return Task.CompletedTask; }

    [Fact] public async Task SaveAsync_Insert() { Assert.Equal(1, await _repository.SaveAsync({Entity}.Create("Test"))); }
    [Fact] public async Task GetAllAsync() { /* save 2, assert count == 2 */ }
    [Fact] public async Task SaveAsync_Update() { /* save, Update(), save again, assert new value */ }
    [Fact] public async Task DeleteAsync() { /* save, delete, get returns null */ }

    // Domain logic tests (no DB needed)
    [Fact] public void Validate_EmptyName_ReturnsError() => Assert.NotNull(new {Entity} { Name = "" }.Validate());
    [Fact] public void Create_Valid() => Assert.Equal("Ok", {Entity}.Create("Ok").Name);
    [Fact] public void Create_Invalid_Throws() => Assert.Throws<InvalidOperationException>(() => {Entity}.Create(""));

    // Mapper tests
    [Fact] public void MapModelToInfo()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<{Entity}Profile>());
        var mapper = config.CreateMapper();
        var model = {Entity}.Create("Test");
        var info = mapper.Map<{Entity}Info>(model);
        Assert.Equal(model.Name, info.Name);
    }
}
```

---

## Checklist

| # | Layer | File |
|---|-------|------|
| 1 | Domain | `{App}.Domain/Models/{Entity}.cs` (Rich Entity: Validate, Update, Create) |
| 2 | DTO | `{App}.DTO/DTOs/{Entity}Info.cs` (pure data, "Info" suffix) |
| 3 | Infra.Interfaces | `{App}.Infra.Interfaces/Services/I{Entity}Repository.cs` |
| 4 | Domain (if needed) | `{App}.Domain/Services/{Entity}Service.cs` (cross-entity rules) |
| 5 | Infra.Interfaces (if needed) | `{App}.Infra.Interfaces/Services/I{Entity}AppService.cs` |
| 6 | Infra (if needed) | `{App}.Infra/AppServices/{Entity}AppService.cs` (external APIs) |
| 7 | Infra | `{App}.Infra/Mappers/{Entity}Profile.cs` (Model ↔ Info) |
| 8 | Infra | `{App}.Infra/Services/{Entity}Repository.cs` |
| 9 | Infra | Modify `AppDatabase.cs` → `CreateTableAsync<{Entity}>()` |
| 10-11 | MAUI | `ViewModels/{Entity}ListViewModel.cs` + `{Entity}DetailViewModel.cs` |
| 12-15 | MAUI | `Pages/{Entity}ListPage.xaml(.cs)` + `{Entity}DetailPage.xaml(.cs)` |
| 16 | MAUI | Modify `MauiProgram.cs` (DI + AutoMapper) |
| 17 | MAUI | Modify `AppShell.xaml` + `.xaml.cs` (navigation) |
| 18 | Tests | `{Entity}RepositoryTests.cs` + domain logic + mapper tests |

## Response Guidelines

1. **Discover first** — `dotnet sln list`, read existing entities to match patterns
2. **Order** — Domain → DTO → Infra.Interfaces → Domain Services → Infra (Mappers + AppServices + Repos) → MAUI → Tests
3. **Build after each layer** to catch errors early
4. **Match the app language** in UI strings
5. **Rich Domain** — validation in entities, state via methods, creation via factories. NO business rules in repos/ViewModels
6. **DTOs** — suffix "Info", pure data, no logic. ViewModels/Pages bind to DTOs, never to Models directly
7. **Mapper** — one Profile per entity in `Infra/Mappers/`. Model → Info (read), Info → Model (write, ignore managed fields)
8. **Domain Services** — cross-entity rules only, depend on interfaces only
9. **AppServices** — infrastructure only (external APIs), live in `{App}.Infra/AppServices/`
10. **Singleton** for repos/services/appservices · **Transient** for ViewModels/Pages
