## Podpięcie serwera

```csharp
IServerSession server = new ServerSession();

server.ClientConnected    += client => { /* klient dołączył */ };
server.ClientDisconnected += client => { /* klient odłączył */ };
server.FrameDispatched    += (client, frameIndex) => { };
server.FrameCompleted     += (client, frameIndex, duration) => { };
server.FrameFailed        += (client, frameIndex) => { };
server.RenderCompleted    += () => { };

await server.StartAsync(new ServerConnectionSettings(
    Address:       "0.0.0.0",
    Port:          3000,
    Protocol:      TransportProtocol.Tcp,
    ClientTimeout: TimeSpan.FromSeconds(30)
));
```

## Podpięcie klienta

```csharp
IClientSession client = new ClientSession();

client.Connected     += () => { };
client.Disconnected  += () => { };
client.FrameStarted  += frameIndex => { };
client.FrameCompleted += (frameIndex, duration, result) => { };
client.FrameFailed   += frameIndex => { };

await client.ConnectAsync("NazwaKlienta", new ClientConnectionSettings(
    Address:           "127.0.0.1",
    Port:              3000,
    Protocol:          TransportProtocol.Tcp,
    HeartbeatInterval: TimeSpan.FromSeconds(5)
));
```

## Uruchamianie renderowania

```csharp
await server.StartRenderAsync(new RenderSettings(
    Keyframes:      keyframes,
    Options:        new MandelbrotOptions(Width: 1920, Height: 1080, MaxIterations: 500),
    Colorizer:      FractalColorizerType.CyclingHsv,
    TotalFrames:    120,
    Fps:            30,
    Interpolation:  ZoomInterpolationType.SmoothStep,
    FramesPerClient: 1,
    ClientSelector: ClientSelectorType.RoundRobin,
    OutputFormat:   VideoFormat.Gif,
    OutputPath:     "output.gif"
));
```

## Preview klatki

Do podglądu pojedynczej klatki lokalnie użyj `FrameRenderer` z projektu `DistributedFractals.Fractal`:

```csharp
FractalResult result = await FrameRenderer.RenderAsync(
    options:       new MandelbrotOptions(Width: 800, Height: 600, MaxIterations: 200),
    bounds:        new FrameBounds(MinRe: -2.5, MaxRe: 1.0, MinIm: -1.2, MaxIm: 1.2),
    colorizerType: FractalColorizerType.BlackAndWhite
);
```

`FrameRenderer` dobiera generator na podstawie `options.GeneratorType` automatycznie.

## Logger w GUI (konsola logów)

`Logger` jest singletonem z domyślnym `NullLogger`. Przed uruchomieniem sesji należy go zainicjalizować implementacją piszącą do konsoli w GUI.

### Implementacja

```csharp
public class ViewModelLogger : ILogger
{
    private readonly ObservableCollection<string> _entries;

    public ViewModelLogger(ObservableCollection<string> entries)
    {
        _entries = entries;
    }

    public void Log(string message)
    {
        string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (Dispatcher.UIThread.CheckAccess())
            _entries.Add(entry);
        else
            Dispatcher.UIThread.Post(() => _entries.Add(entry));
    }
}
```

`Logger.Log` wywołuje się z wątków roboczych (renderowanie, heartbeat, odbiór wiadomości), więc `Dispatcher.UIThread.Post` jest konieczne - bez tego modyfikacja kolekcji z innego wątku rzuci wyjątek.

### Inicjalizacja

Raz, przy starcie aplikacji (np. w konstruktorze `MainView`):

```csharp
ObservableCollection<string> _logEntries = new();
Logger.Initialize(new ViewModelLogger(_logEntries));
```

`Logger` przyjmuje dokładnie jedną implementację `ILogger`. Żeby logować w kilku miejscach jednocześnie (np. GUI + plik + konsola), używa się wzorca dekoratora: `CompositeLogger` opakowuje wiele loggerów i deleguje do każdego z nich. Dzięki temu `Logger.Initialize` wywołuje się nadal raz, a każda warstwa logowania jest osobną klasą:

```csharp
ObservableCollection<string> _logEntries = new();

Logger.Initialize(new CompositeLogger(
    new ConsoleLogger(),
    new ViewModelLogger(_logEntries)
));
```

Chcąc dodać np. logowanie do pliku wystarczy dopisać kolejny `ILogger` do listy - bez zmian w istniejących klasach.

## Dynamiczne kontrolki opcji generatora

Każdy generator ma inne parametry, więc kontrolki w GUI powinny być generowane dynamicznie w zależności od wybranego generatora. Proponowane podejście: **panel opcji jako interfejs**.

### Interfejs

```csharp
public interface IGeneratorOptionsPanel
{
    Control CreateControl();
    IFractalGeneratorOptions GetOptions();
    void SetOptions(IFractalGeneratorOptions options);
}
```

- `CreateControl()` - zwraca widok Avalonia z polami dla danego generatora
- `GetOptions()` - odczytuje wartości z kontrolek i zwraca gotowe opcje
- `SetOptions()` - wypełnia kontrolki z istniejących opcji (np. przy wczytaniu projektu)

### Implementacja dla Mandelbrota

```csharp
public class MandelbrotOptionsPanel : IGeneratorOptionsPanel
{
    private readonly NumericUpDown _width        = new() { Value = 1920 };
    private readonly NumericUpDown _height       = new() { Value = 1080 };
    private readonly NumericUpDown _maxIterations = new() { Value = 500 };

    public Control CreateControl() => new StackPanel
    {
        Children = { _width, _height, _maxIterations }
    };

    public IFractalGeneratorOptions GetOptions() =>
        new MandelbrotOptions(
            Width:         (ulong)(_width.Value ?? 1920),
            Height:        (ulong)(_height.Value ?? 1080),
            MaxIterations: (ulong)(_maxIterations.Value ?? 500)
        );

    public void SetOptions(IFractalGeneratorOptions options)
    {
        if (options is not MandelbrotOptions o) return;
        _width.Value         = o.Width;
        _height.Value        = o.Height;
        _maxIterations.Value = o.MaxIterations;
    }
}
```

### Fabryka paneli

```csharp
public static class GeneratorOptionsPanelFactory
{
    public static IGeneratorOptionsPanel Create(FractalGeneratorType type) => type switch
    {
        FractalGeneratorType.Mandelbrot => new MandelbrotOptionsPanel(),
        _ => throw new NotSupportedException($"Unsupported generator type: {type}")
    };
}
```

### Użycie w GUI

Gdy użytkownik zmienia wybrany generator w ComboBox, GUI podmienia panel:

```csharp
private IGeneratorOptionsPanel _optionsPanel;

private void OnGeneratorChanged(FractalGeneratorType type)
{
    _optionsPanel = GeneratorOptionsPanelFactory.Create(type);
    OptionsContainer.Content = _optionsPanel.CreateControl();
}

private IFractalGeneratorOptions GetCurrentOptions() =>
    _optionsPanel.GetOptions();
```

Dodanie nowego generatora wymaga tylko stworzenia nowej implementacji `IGeneratorOptionsPanel` i zarejestrowania jej w `GeneratorOptionsPanelFactory`.
